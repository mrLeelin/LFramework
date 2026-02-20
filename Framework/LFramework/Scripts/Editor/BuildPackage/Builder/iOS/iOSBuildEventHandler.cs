using System;
using System.Collections.Generic;
using LFramework.Editor.Builder.iOS.Configurators;
using LFramework.Editor.Builder.iOS.Installers;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using Debug = UnityEngine.Debug;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建事件处理器（协调器）
    /// 实现 IBuildEventHandler 接口，负责协调各个配置器的执行
    ///
    /// 架构说明：
    /// - 本类作为协调器，不包含具体实现逻辑
    /// - 所有具体功能由专门的配置器类实现
    /// - 使用统一的步骤执行包装器，提供一致的日志和错误处理
    ///
    /// 执行流程：
    /// 1. 创建并验证配置
    /// 2. 配置 PBXProject（iOSProjectConfigurator）
    /// 3. 配置 Entitlements（iOSEntitlementsConfigurator）
    /// 4. 配置 Info.plist（iOSPlistConfigurator）
    /// 5. 应用代码修复（iOSCodeFixer）
    /// 6. 执行 CocoaPods 安装（iOSCocoaPodsInstaller）
    /// </summary>
    public class iOSBuildEventHandler : IBuildEventHandler
    {
        /// <summary>
        /// 预处理打包资源事件
        /// </summary>
        public void OnPreprocessBuildApp(BuildSetting mBuildData)
        {
            // iOS 构建前无需特殊处理
        }

        /// <summary>
        /// 执行自定义宏事件
        /// </summary>
        public void OnProcessScriptingDefineSymbols(BuildSetting mBuildData, List<string> defineList)
        {
            // iOS 平台无需添加额外的宏定义
        }

        /// <summary>
        /// 执行打包前事件
        /// </summary>
        public void OnPreprocessBuildResources(BuildSetting buildSetting)
        {
            // iOS 资源打包前无需特殊处理
        }

        /// <summary>
        /// 执行打包后事件
        /// </summary>
        public void OnPostprocessBuildResources(BuildSetting buildSetting)
        {
            // iOS 资源打包后无需特殊处理
        }

        /// <summary>
        /// 执行打包后事件（核心方法）
        /// 在 iOS 应用构建完成后执行所有后处理逻辑
        /// </summary>
        public void OnPostprocessBuildApp(BuildSetting mBuildData,string outPutFolder)
        {
            // 仅在 iOS 平台执行
            if (mBuildData.builderTarget != BuilderTarget.iOS)
            {
                return;
            }

            iOSBuildLogger.LogInfo("Starting iOS post-build processing...");

#if UNITY_IOS
            try
            {
                // 1. 创建并验证配置
                iOSBuildConfig config = null;
                ExecuteStep("Configuration creation and validation", () =>
                {
                    config = iOSBuildConfig.CreateFromBuildSetting(mBuildData, outPutFolder);
                    config.Validate();

                    iOSBuildLogger.LogInfo($"Output path: {config.OutputPath}");
                    iOSBuildLogger.LogInfo($"Build mode: {(config.IsDevelopment ? "Development" : "Release")}");
                    iOSBuildLogger.LogInfo($"Pod command: {config.PodCommandPath}");
                });

                // 2. 配置 PBXProject
                ExecuteStep("PBXProject configuration", () =>
                {
                    var projectConfigurator = new iOSProjectConfigurator(config);
                    projectConfigurator.Configure();
                });

                // 3. 配置 Entitlements
                ExecuteStep("Entitlements configuration", () =>
                {
                    var entitlementsConfigurator = new iOSEntitlementsConfigurator(config);
                    entitlementsConfigurator.Configure();
                });

                // 4. 配置 Info.plist
                ExecuteStep("Info.plist configuration", () =>
                {
                    var plistConfigurator = new iOSPlistConfigurator(config);
                    plistConfigurator.Configure();
                });

                // 5. 应用代码修复
                ExecuteStep("Code fixes", () =>
                {
                    var codeFixer = new iOSCodeFixer(config);
                    codeFixer.ApplyAllFixes();
                });

                // 6. 执行 CocoaPods 安装
                ExecuteStep("CocoaPods installation", () =>
                {
                    var podsInstaller = new iOSCocoaPodsInstaller(config);
                    podsInstaller.Install();
                });

                iOSBuildLogger.LogInfo("iOS post-build processing completed successfully!");
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogException("iOS post-build processing", ex);
                throw;
            }
#else
            iOSBuildLogger.LogWarning("Skipping iOS post-build processing (not on iOS platform)");
#endif
        }

        /// <summary>
        /// 统一的步骤执行包装器
        /// 提供统一的日志记录和错误处理
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        /// <param name="action">要执行的操作</param>
        private void ExecuteStep(string stepName, Action action)
        {
            iOSBuildLogger.LogStep(stepName);
            try
            {
                action();
                iOSBuildLogger.LogSuccess(stepName);
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogException(stepName, ex);
                throw;
            }
        }
    }
}

