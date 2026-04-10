using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 平台配置工厂
    /// 根据构建目标创建对应的平台配置
    /// </summary>
    public static class PlatformConfigFactory
    {
        /// <summary>
        /// 创建平台配置
        /// </summary>
        /// <param name="builderTarget">构建目标平台</param>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>平台配置实例</returns>
        public static IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            if (buildSetting == null)
            {
                throw new ArgumentNullException(nameof(buildSetting));
            }

            IPlatformConfigRegistryProvider provider = SelectProvider(builderTarget);
            IPlatformConfig config = provider.CreateConfig(builderTarget, buildSetting);
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Platform config provider '{provider.ProviderName}' returned null for target '{builderTarget}'.");
            }

            Debug.Log(
                $"[PlatformConfigFactory] Selected provider '{provider.ProviderName}' for target '{builderTarget}', config '{config.GetType().Name}'.");
            return config;
        }

        /// <summary>
        /// 检查指定的构建目标是否受支持
        /// </summary>
        public static bool IsSupported(BuilderTarget builderTarget)
        {
            try
            {
                SelectProvider(builderTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取构建目标的显示名称
        /// </summary>
        public static string GetDisplayName(BuilderTarget builderTarget)
        {
            switch (builderTarget)
            {
                case BuilderTarget.Windows:
                    return "Windows Standalone";
                case BuilderTarget.Android:
                    return "Android";
                case BuilderTarget.iOS:
                    return "iOS";
                default:
                    return "Unknown";
            }
        }

        private static IPlatformConfigRegistryProvider SelectProvider(BuilderTarget builderTarget)
        {
            List<IPlatformConfigRegistryProvider> providers = GetAllProviders()
                .Where(provider => provider.IsActive)
                .ToList();
            if (providers.Count == 0)
            {
                providers.Add(new DefaultPlatformConfigRegistryProvider());
            }

            int highestPriority = providers.Max(provider => provider.Priority);
            List<IPlatformConfigRegistryProvider> highestPriorityProviders = providers
                .Where(provider => provider.Priority == highestPriority)
                .ToList();
            if (highestPriorityProviders.Count > 1)
            {
                string providerNames = string.Join(", ",
                    highestPriorityProviders.Select(provider => provider.ProviderName));
                throw new InvalidOperationException(
                    $"Multiple platform config providers share the highest priority {highestPriority}: {providerNames}");
            }

            IPlatformConfigRegistryProvider selectedProvider = highestPriorityProviders[0];
            if (!selectedProvider.Supports(builderTarget))
            {
                throw new InvalidOperationException(
                    $"Platform config provider '{selectedProvider.ProviderName}' does not support target '{builderTarget}'.");
            }

            return selectedProvider;
        }

        private static List<IPlatformConfigRegistryProvider> GetAllProviders()
        {
            List<IPlatformConfigRegistryProvider> providers = new List<IPlatformConfigRegistryProvider>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (!typeof(IPlatformConfigRegistryProvider).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        Debug.LogWarning(
                            $"[PlatformConfigFactory] Skip provider '{type.FullName}' because it does not have a parameterless constructor.");
                        continue;
                    }

                    if (Activator.CreateInstance(type) is IPlatformConfigRegistryProvider provider)
                    {
                        providers.Add(provider);
                    }
                }
            }

            return providers;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}
