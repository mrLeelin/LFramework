using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// iOS PBXProject 配置器
    /// 负责配置 Xcode 项目文件，包括 Framework、Library、本地化文件等
    /// </summary>
    public class iOSProjectConfigurator
    {
        private readonly iOSBuildConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">构建配置</param>
        public iOSProjectConfigurator(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 执行 PBXProject 配置
        /// </summary>
        public void Configure()
        {
#if UNITY_IOS
            iOSBuildLogger.LogStep("PBXProject configuration");

            string projectPath = PBXProject.GetPBXProjectPath(_config.OutputPath);
            iOSBuildLogger.LogInfo($"Project path: {projectPath}");

            // 读取项目文件
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);

            // 配置 UnityFramework Target
            ConfigureUnityFramework(pbxProject);

            // 配置 UnityMain Target
            ConfigureUnityMain(pbxProject);

            // 添加本地化文件
            AddLocalizations(pbxProject);

            // 写回项目文件
            pbxProject.WriteToFile(projectPath);

            iOSBuildLogger.LogSuccess("PBXProject configuration");
#else
            iOSBuildLogger.LogWarning("Skipping PBXProject configuration (not on iOS platform)");
#endif
        }

#if UNITY_IOS
        /// <summary>
        /// 配置 UnityFramework Target
        /// </summary>
        private void ConfigureUnityFramework(PBXProject pbxProject)
        {
            string unityFrameworkGuid = pbxProject.GetUnityFrameworkTargetGuid();

            // 设置 Swift 标准库嵌入（Framework 不需要）
            pbxProject.SetBuildProperty(unityFrameworkGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

            // 关闭 BitCode
            pbxProject.SetBuildProperty(unityFrameworkGuid, "ENABLE_BITCODE", "NO");

            // 添加 Frameworks
            foreach (var framework in iOSBuildConstants.UNITY_FRAMEWORK_FRAMEWORKS)
            {
                pbxProject.AddFrameworkToProject(unityFrameworkGuid, framework, false);
            }

            iOSBuildLogger.LogInfo("UnityFramework Target configured");
        }

        /// <summary>
        /// 配置 UnityMain Target
        /// </summary>
        private void ConfigureUnityMain(PBXProject pbxProject)
        {
            string unityMainGuid = pbxProject.GetUnityMainTargetGuid();

            // 设置 Entitlements 文件路径
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_ENTITLEMENTS",
                iOSBuildConstants.ENTITLEMENTS_FILE_NAME);

            // 设置 Swift 标准库嵌入（Main Target 需要）
            pbxProject.SetBuildProperty(unityMainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            // 设置代码签名身份
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_IDENTITY",
                iOSBuildData.CODE_SIGN_IDENTITY);

            // 设置团队配置
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_STYLE", "Manual");
            pbxProject.SetBuildProperty(unityMainGuid, "DEVELOPMENT_TEAM",
                iOSBuildData.AppleDevelopTeamId);

            // 设置架构（仅支持 arm64）
            pbxProject.SetBuildProperty(unityMainGuid, "ARCHS", "arm64");

            // 关闭 BitCode
            pbxProject.SetBuildProperty(unityMainGuid, "ENABLE_BITCODE", "NO");

            // 添加 Frameworks
            AddFrameworks(pbxProject, unityMainGuid);

            // 添加 Libraries
            AddLibraries(pbxProject, unityMainGuid);

            iOSBuildLogger.LogInfo("UnityMain Target configured");
        }

        /// <summary>
        /// 添加系统框架
        /// </summary>
        private void AddFrameworks(PBXProject pbxProject, string targetGuid)
        {
            foreach (var framework in iOSBuildConstants.UNITY_MAIN_FRAMEWORKS)
            {
                pbxProject.AddFrameworkToProject(targetGuid, framework, true);
            }

            iOSBuildLogger.LogInfo($"Added {iOSBuildConstants.UNITY_MAIN_FRAMEWORKS.Length} frameworks");
        }

        /// <summary>
        /// 添加系统库
        /// </summary>
        private void AddLibraries(PBXProject pbxProject, string targetGuid)
        {
            foreach (var library in iOSBuildConstants.REQUIRED_LIBRARIES)
            {
                string fileGuid = pbxProject.AddFile(
                    iOSBuildConstants.SYSTEM_LIB_PATH + library,
                    iOSBuildConstants.FRAMEWORKS_PATH + library,
                    PBXSourceTree.Sdk);
                pbxProject.AddFileToBuild(targetGuid, fileGuid);
            }

            iOSBuildLogger.LogInfo($"Added {iOSBuildConstants.REQUIRED_LIBRARIES.Length} libraries");
        }

        /// <summary>
        /// 添加本地化文件
        /// 从 ExportData/IOS/InfoPlist 读取 *.lproj 文件夹并添加到项目
        /// </summary>
        private void AddLocalizations(PBXProject pbxProject)
        {
            string folderPath = _config.LocalizationFolderPath;
            DirectoryInfo dir = new DirectoryInfo(folderPath);

            if (!dir.Exists)
            {
                iOSBuildLogger.LogWarning($"Localization folder not found: {folderPath}");
                return;
            }

            string unityMainGuid = pbxProject.GetUnityMainTargetGuid();
            List<string> locales = new List<string>();

            // 查找所有 *.lproj 文件夹
            var localeDirs = dir.GetDirectories("*.lproj", SearchOption.TopDirectoryOnly);
            foreach (var locale in localeDirs)
            {
                string localeName = Path.GetFileNameWithoutExtension(locale.Name);
                locales.Add(localeName);
            }

            // 添加到项目
            foreach (var locale in locales)
            {
                string fileName = $"{locale}.lproj";
                var guid = pbxProject.AddFolderReference($"{folderPath}/{fileName}", fileName);
                pbxProject.AddFileToBuild(unityMainGuid, guid);
                iOSBuildLogger.LogInfo($"Added localization: {fileName}");
            }

            if (locales.Count > 0)
            {
                iOSBuildLogger.LogInfo($"Added {locales.Count} localizations");
            }
        }
#endif
    }
}
