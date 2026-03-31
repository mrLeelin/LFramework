using System;
using System.Collections.Generic;
using GameFramework;
using LFramework.Runtime;
using UnityGameFramework.Runtime;
using Zenject;


namespace LFramework.Hotfix
{
    /// <summary>
    ///The Game hotfix entry
    /// </summary>
    public static class Entry
    {
        // The main entry for Hotfix
        public static void HotfixEntryStart()
        {
            Log.Info("Enter Hotfix Entry");
            var application = SingletonManager.AddSingleton<LSystemApplication>();
            LSystemApplication.ClearReflectionCache();
            var procedureComponent = LFrameworkAspect.Instance.Get<ProcedureComponent>();
            if (procedureComponent == null)
            {
                Log.Fatal("ProcedureComponent is null.");
                return;
            }

            var hotfixComponent = LFrameworkAspect.Instance.Get<HotfixComponent>();
            if (hotfixComponent == null)
            {
                Log.Fatal("HotfixComponent is null.");
                return;
            }

            application.RegisterHotfixComponents(hotfixComponent);
            AddHotfixProcedure(procedureComponent, hotfixComponent);
            RegisterPreloadInjects(hotfixComponent);
            var entranceProcedure = procedureComponent.EntranceHotfixProcedureTypeName;
            var dict =  hotfixComponent.GetHotfixAssemblyAllTypes();
            if (string.IsNullOrEmpty(entranceProcedure))
            {
                Log.Error("Enter hotfix force change procedure is null.please check [ProcedureComponentSetting].");
            }
            else if (dict.TryGetValue(entranceProcedure,out var hotfixEntranceProcedureType))
            {
                procedureComponent.ForceChangedProcedure(hotfixEntranceProcedureType);
                Log.Info($"Enter Hotfix Force Changed Procedure. '{hotfixEntranceProcedureType?.FullName}'");
            }
            else
            {
                Log.Error($"Enter Hotfix Force Change procedure error. '{entranceProcedure}' is not exist.");   
            }
        }


        /// <summary>
        /// 获取热更的Procedure 类
        /// </summary>
        /// <returns></returns>
        private static bool GetHotfixProcedureTypes(HotfixComponent hotfixComponent,
            out GameFrameworkLinkedListRange<Type> result)
        {
            var attributes = hotfixComponent.GetTypesFromAttribute<HotfixProcedureAttribute>();
            if (attributes.HasValue)
            {
                result = attributes.Value;
            }
            else
            {
                result = default;
            }

            return attributes.HasValue;
        }


        /// <summary>
        /// 打了 Preload 标签的类
        /// 会在加载热更新的时候缓存进来
        /// 避免之后卡顿
        /// </summary>
        /// <param name="hotfixComponent"></param>
        private static void RegisterPreloadInjects(HotfixComponent hotfixComponent)
        {
            var preLoadAttributes = hotfixComponent.GetTypesFromAttribute<PreLoadZenjectAttribute>();
            if (!preLoadAttributes.HasValue)
            {
                return;
            }

            foreach (var preloadType in preLoadAttributes)
            {
                if (preloadType == null)
                {
                    continue;
                }

                if (preloadType.IsInterface)
                {
                    continue;
                }

                //预加载
                TypeAnalyzer.TryGetInfo(preloadType);
            }
        }

        /// <summary>
        /// 添加热更流程进入流程
        /// </summary>
        private static void AddHotfixProcedure(ProcedureComponent procedureComponent, HotfixComponent hotfixComponent)
        {
            var objs = new List<object>();
            if (!GetHotfixProcedureTypes(hotfixComponent, out var types))
            {
                return;
            }

            foreach (var type in types)
            {
                if (type.IsAbstract)
                {
                    //Ignore base abstract
                    continue;
                }

                var instance = Activator.CreateInstance(type);
                objs.Add(instance);
            }

            procedureComponent.AddHotfixProcedure(objs);
        }
    }
}