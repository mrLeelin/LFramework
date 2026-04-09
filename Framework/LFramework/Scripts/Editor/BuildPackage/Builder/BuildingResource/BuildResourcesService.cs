using System;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder
{
    /// <summary>
    /// 资源构建服务
    /// 负责协调资源构建流程，符合 Task 架构原则
    /// 不依赖任何特定的资源系统（Addressable、YooAssets）
    /// </summary>
    public static class BuildResourcesService
    {

        /// <summary>
        /// 构建资源（从 BuildSetting 调用，用于构建管线）
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        public static void Build(BuildSetting buildSetting)
        {
            Debug.Log($"[BuildResourcesService] Starting resource build...");
            Debug.Log($"[BuildResourcesService] Active build target: '{EditorUserBuildSettings.activeBuildTarget}'");

            if (buildSetting == null)
            {
                Debug.LogError("[BuildResourcesService] BuildSetting is null.");
                return;
            }

            // 检查资源系统是否受支持
            if (!ResourceBuildSystemFactory.IsSupported(buildSetting.ResourceSystem))
            {
                Debug.LogError($"[BuildResourcesService] Resource system '{buildSetting.ResourceSystem}' is not supported. Please check your configuration.");
                return;
            }

            Debug.Log($"[BuildResourcesService] Using resource system: {ResourceBuildSystemFactory.GetDisplayName(buildSetting.ResourceSystem)}");

            // 设置构建目标平台
            SetBuildTarget(BuildPackageWindow.ConvertToBuilderTarget(buildSetting.builderTarget));

            // 使用工厂模式创建资源构建系统
            Debug.Log($"[BuildResourcesService] Creating resource build system...");
            var buildSystem = ResourceBuildSystemFactory.Create(buildSetting.ResourceSystem);

            // 执行构建 - 每个系统自己负责获取所需的配置
            Debug.Log($"[BuildResourcesService] Executing resource build...");
            buildSystem.Build(buildSetting);

            Debug.Log($"[BuildResourcesService] Resource build completed successfully.");
        }
        
        /// <summary>
        /// 设置构建目标平台
        /// </summary>
        /// <param name="newBuildTarget">新的构建目标</param>
        private static void SetBuildTarget(BuildTarget newBuildTarget)
        {
            if (newBuildTarget == EditorUserBuildSettings.activeBuildTarget)
            {
                Debug.Log($"[BuildResourcesService] Build target already set to '{newBuildTarget}'");
                return;
            }

            Debug.Log($"[BuildResourcesService] Switching build target to '{newBuildTarget}'...");

            var targetGroup = BuildPipeline.GetBuildTargetGroup(newBuildTarget);
            var success = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, newBuildTarget);

            if (success)
            {
                Debug.Log($"[BuildResourcesService] Build target switched to '{newBuildTarget}' successfully.");
            }
            else
            {
                throw new Exception($"[BuildResourcesService] Failed to switch build target to '{newBuildTarget}'.");
            }
        }
    }
}
