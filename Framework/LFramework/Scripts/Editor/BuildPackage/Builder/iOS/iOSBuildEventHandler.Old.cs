using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LFramework.Editor.Builder;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using Debug = UnityEngine.Debug;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建事件处理器
    /// 实现 IBuildEventHandler 接口，处理 iOS 平台的构建后处理逻辑
    /// 包括：PBXProject 配置、Entitlements 配置、Info.plist 配置、各种修复功能
    /// </summary>
    public class iOSBuildEventHandlerOld
    {
        private static string ProjectEntitlementsPath = "";
        private static string RelativeEntitlementPath = "game.entitlements";

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

        public void OnPostprocessBuildApp(BuildSetting mBuildData, string outPutFolder)
        {
            
        }

        /// <summary>
        /// 执行打包后事件（核心方法）
        /// 在 iOS 应用构建完成后执行所有后处理逻辑
        /// </summary>
        public void OnPostprocessBuildApp(BuildSetting mBuildData)
        {
            // 仅在 iOS 平台执行
            if (mBuildData.builderTarget != BuilderTarget.iOS)
            {
                return;
            }

            Debug.Log("[iOSBuildEventHandler] Starting iOS post-build processing...");

#if UNITY_IOS
            try
            {
                // 获取构建输出路径
                string outputPath = Application.dataPath + "/../Builds/IOS/Project";
                bool isDevelopment = !mBuildData.isRelease;

                Debug.Log($"[iOSBuildEventHandler] Output path: {outputPath}");
                Debug.Log($"[iOSBuildEventHandler] Is development build: {isDevelopment}");

                // 1. 配置 PBXProject
                OnPostprocessBuildInternal(outputPath, isDevelopment);

                // 2. 执行修复函数
                FixColdStartFacebook(outputPath);
                FixColdStartDeeplink(outputPath);
                FixUseFrameworksBug(outputPath);

                // 3. 执行 CocoaPods 安装
                RunPodInstall(outputPath);

                Debug.Log("[iOSBuildEventHandler] iOS post-build processing completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBuildEventHandler] iOS post-build processing failed: {ex.Message}");
                Debug.LogError($"[iOSBuildEventHandler] Stack trace: {ex.StackTrace}");
            }
#else
            Debug.LogWarning("[iOSBuildEventHandler] Skipping iOS post-build processing (not on iOS platform)");
#endif
        }

#if UNITY_IOS
        /// <summary>
        /// iOS 构建后处理内部实现
        /// </summary>
        private void OnPostprocessBuildInternal(string path, bool isDevelopment)
        {
            Debug.Log($"[iOSBuildEventHandler] Starting PBXProject configuration...");

            string projectPath = PBXProject.GetPBXProjectPath(path);
            Debug.Log($"[iOSBuildEventHandler] Project path: {projectPath}");

            ProjectEntitlementsPath = Path.Combine(path, RelativeEntitlementPath);

            // 确保 entitlements 文件存在
            if (!File.Exists(ProjectEntitlementsPath))
            {
                var emptyEntitlement = new PlistDocument();
                emptyEntitlement.WriteToFile(ProjectEntitlementsPath);
                Debug.Log($"[iOSBuildEventHandler] Created entitlements file: {ProjectEntitlementsPath}");
            }

            // 项目编辑
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);
            OnGenerateProjectFile(pbxProject, isDevelopment);
            pbxProject.WriteToFile(projectPath);
            OnGenerateEntitlement(pbxProject, projectPath, isDevelopment);

            // plist 编辑
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            OnGeneratePlistFile(plist.root, isDevelopment);
            SetURLSchemeByPlist(plist.root);
            SetFacebookMessenger(plist.root);
            File.WriteAllText(plistPath, plist.WriteToString());

            Debug.Log($"[iOSBuildEventHandler] PBXProject configuration completed.");
        }

        /// <summary>
        /// 添加库文件到项目
        /// </summary>
        private static void AddLibToProject(PBXProject inst, string targetGuid, string lib)
        {
            string fileGuid = inst.AddFile("usr/lib/" + lib, "Frameworks/" + lib, PBXSourceTree.Sdk);
            inst.AddFileToBuild(targetGuid, fileGuid);
        }

        /// <summary>
        /// 项目编辑
        /// 配置 UnityFramework 和 UnityMain Target
        /// </summary>
        private static void OnGenerateProjectFile(PBXProject pbxProject, bool isDevelopment)
        {
            Debug.Log("[iOSBuildEventHandler] Configuring PBXProject...");

            // Unity Framework 编辑
            string unityFrameWork = pbxProject.GetUnityFrameworkTargetGuid();

            pbxProject.SetBuildProperty(unityFrameWork, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            pbxProject.SetBuildProperty(unityFrameWork, "ENABLE_BITCODE", "NO");
            pbxProject.AddFrameworkToProject(unityFrameWork, "GameKit.framework", false);
            pbxProject.AddFrameworkToProject(unityFrameWork, "AppTrackingTransparency.framework", false);

            // Unity-iPhone 操作
            string unityMainGuid = pbxProject.GetUnityMainTargetGuid();
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_ENTITLEMENTS", RelativeEntitlementPath);
            pbxProject.SetBuildProperty(unityMainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_IDENTITY", iOSBuildData.CODE_SIGN_IDENTITY);

            // 团队配置
            pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_STYLE", "Manual");
            pbxProject.SetBuildProperty(unityMainGuid, "DEVELOPMENT_TEAM", iOSBuildData.AppleDevelopTeamId);

            // 必填 - 新规
            pbxProject.SetBuildProperty(unityMainGuid, "ARCHS", "arm64");

            // 关闭 BitCode（有些 SDK 不支持 BitCode）
            pbxProject.SetBuildProperty(unityMainGuid, "ENABLE_BITCODE", "NO");

            // 添加 Frameworks
            pbxProject.AddFrameworkToProject(unityMainGuid, "GameKit.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "SystemConfiguration.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "CoreTelephony.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "Security.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "JavaScriptCore.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "Photos.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "iAd.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "WebKit.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "AppTrackingTransparency.framework", true);
            pbxProject.AddFrameworkToProject(unityMainGuid, "UserNotifications.framework", true);

            // 添加库文件
            AddLibToProject(pbxProject, unityMainGuid, "libz.tbd");
            AddLibToProject(pbxProject, unityMainGuid, "libsqlite3.tbd");
            AddLibToProject(pbxProject, unityMainGuid, "libc++.tbd");

            // 处理 Info.plist 本地化文件
            OnInfoPlist(pbxProject, isDevelopment);

            Debug.Log("[iOSBuildEventHandler] PBXProject configuration completed.");
        }

        /// <summary>
        /// 生成 Entitlements 配置
        /// 添加 Keychain、Push、Game Center、IAP、Sign in with Apple 等能力
        /// </summary>
        private static void OnGenerateEntitlement(PBXProject pbxProject, string projectPath, bool isDevelopment)
        {
            Debug.Log("[iOSBuildEventHandler] Configuring Entitlements...");

            var manager = new ProjectCapabilityManager(
                projectPath,
                RelativeEntitlementPath,
                null,
                pbxProject.GetUnityMainTargetGuid());

            // 添加 Keychain Sharing
            manager.AddKeychainSharing(new[]
            {
                $"$(AppIdentifierPrefix)" + Application.identifier,
            });

            // 添加 Push Notifications（区分开发和生产环境）
            manager.AddPushNotifications(isDevelopment);

            // 添加 Game Center
            manager.AddGameCenter();

            // 添加 In-App Purchase
            manager.AddInAppPurchase();

            // 添加 Sign in with Apple
            manager.AddSignInWithApple();

            manager.WriteToFile();

            Debug.Log("[iOSBuildEventHandler] Entitlements configuration completed.");
        }

        /// <summary>
        /// 生成 Info.plist 配置
        /// 配置加密合规、ATT 权限说明等
        /// </summary>
        private static void OnGeneratePlistFile(PlistElementDict rootDict, bool isDevelopment)
        {
            Debug.Log("[iOSBuildEventHandler] Configuring Info.plist...");

            // 必填 - 新规：加密合规声明
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            // ATT（App Tracking Transparency）权限说明
            rootDict.SetString("NSUserTrackingUsageDescription",
                "Your data will be used to deliver personalized ads to you.");

            Debug.Log("[iOSBuildEventHandler] Info.plist configuration completed.");
        }

        /// <summary>
        /// 处理 Info.plist 本地化文件
        /// 从 ExportData/IOS/InfoPlist 读取本地化文件并添加到项目
        /// </summary>
        private static void OnInfoPlist(PBXProject pbxProject, bool isDevelopment)
        {
            string folderPath = Application.dataPath + "/../ExportData/IOS/InfoPlist";
            DirectoryInfo dir = new DirectoryInfo(folderPath);
            if (!dir.Exists)
            {
                Debug.LogWarning($"[iOSBuildEventHandler] InfoPlist localization folder not found: {folderPath}");
                return;
            }

            string unityMainGuid = pbxProject.GetUnityMainTargetGuid();

            List<string> locales = new List<string>();
            var localeDirs = dir.GetDirectories("*.lproj", SearchOption.TopDirectoryOnly);
            foreach (var locale in localeDirs)
            {
                string s = Path.GetFileNameWithoutExtension(locale.Name);
                locales.Add(s);
            }

            foreach (var locale in locales)
            {
                string fileName = $"{locale}.lproj";
                var guid = pbxProject.AddFolderReference($"{folderPath}/{fileName}", $"{fileName}");
                pbxProject.AddFileToBuild(unityMainGuid, guid);
                Debug.Log($"[iOSBuildEventHandler] Added localization: {fileName}");
            }
        }

        /// <summary>
        /// 设置 URL Schemes
        /// 配置应用的 URL Scheme，支持 Deep Link
        /// </summary>
        private static void SetURLSchemeByPlist(PlistElementDict rootDict)
        {
            Debug.Log("[iOSBuildEventHandler] Configuring URL Schemes...");

            PlistElementArray urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
            PlistElementDict urlTypeDict = urlTypesArray.AddDict();
            urlTypeDict.SetString("CFBundleURLName", "applink.partygamesvc.com");
            PlistElementArray urlSchemesArray = urlTypeDict.CreateArray("CFBundleURLSchemes");
            urlSchemesArray.AddString("https");
            urlSchemesArray.AddString("http");
            urlSchemesArray.AddString("partygame");

            Debug.Log("[iOSBuildEventHandler] URL Schemes configured.");
        }

        /// <summary>
        /// 设置 Facebook Messenger 支持
        /// 添加 LSApplicationQueriesSchemes 以支持 Facebook SDK
        /// </summary>
        private static void SetFacebookMessenger(PlistElementDict rootDict)
        {
            Debug.Log("[iOSBuildEventHandler] Configuring Facebook Messenger support...");

            var urlTypesArray = rootDict.CreateArray("LSApplicationQueriesSchemes");
            urlTypesArray.AddString("fbapi");
            urlTypesArray.AddString("fb-messenger-api");
            urlTypesArray.AddString("fbshareextension");
            urlTypesArray.AddString("fbauth2");

            Debug.Log("[iOSBuildEventHandler] Facebook Messenger support configured.");
        }

        /// <summary>
        /// 修复 Facebook Deep Link 冷启动崩溃问题
        /// 修改 UnityAppController.mm 中的 isBackgroundLaunchOptions 返回值
        /// </summary>
        private static void FixColdStartFacebook(string path)
        {
            Debug.Log("[iOSBuildEventHandler] Fixing Facebook cold start crash...");

            string fullPath = Path.Combine(path, Path.Combine("Classes", "UnityAppController.mm"));
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[iOSBuildEventHandler] UnityAppController.mm not found: {fullPath}");
                return;
            }

            string data = File.ReadAllText(fullPath);

            const string IsBackgroundLaunchOptions =
                @"(?x)(isBackgroundLaunchOptions.+(?:.*\n)+?\s*return\ )YES(\;\n\})# }";

            data = Regex.Replace(data, IsBackgroundLaunchOptions, "$1NO$2");

            File.WriteAllText(fullPath, data);

            Debug.Log("[iOSBuildEventHandler] Facebook cold start crash fix applied.");
        }

        /// <summary>
        /// 修复 Deep Link 接收问题
        /// 设置自定义 AppController 以正确处理 Deep Link
        /// </summary>
        private static void FixColdStartDeeplink(string pathToBuiltProject)
        {
            Debug.Log("[iOSBuildEventHandler] Fixing Deep Link reception...");

            const string CustomAppControllerName = "MyAppController";

            // 1. 修改 Info.plist
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict rootDict = plist.root;
            rootDict.SetString("UnityAppControllerClass", "MyAppController");
            // AppsFlyer 插件启用 Swizzle
            rootDict.SetBoolean("AppsFlyerShouldSwizzle", true);

            plist.WriteToFile(plistPath);
            Debug.Log("[iOSBuildEventHandler] Set UnityAppControllerClass=MyAppController in Info.plist.");

            // 2. 修改 main.mm（可选，保险一点）
            var mainMMPath = Path.Combine(pathToBuiltProject, "Classes/main.mm");

            if (File.Exists(mainMMPath))
            {
                string content = File.ReadAllText(mainMMPath);
                string pattern = @"const char\* AppControllerClassName\s*=\s*""UnityAppController"";";
                string replacement = $@"const char* AppControllerClassName = ""{CustomAppControllerName}"";";

                if (Regex.IsMatch(content, pattern))
                {
                    content = Regex.Replace(content, pattern, replacement);
                    File.WriteAllText(mainMMPath, content);

                    Debug.Log("[iOSBuildEventHandler] Updated AppControllerClassName in main.mm.");
                }
            }

            Debug.Log("[iOSBuildEventHandler] Deep Link reception fix applied.");
        }

        /// <summary>
        /// 修复 Firebase + Facebook Podfile 冲突问题
        /// 移除 use_frameworks! :linkage => :static，改为 use_frameworks!
        /// </summary>
        private static void FixUseFrameworksBug(string pathToBuiltProject)
        {
            Debug.Log("[iOSBuildEventHandler] Fixing Podfile use_frameworks bug...");

            string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");
            if (!File.Exists(podfilePath))
            {
                Debug.LogWarning($"[iOSBuildEventHandler] Podfile not found: {podfilePath}");
                return;
            }

            string content = File.ReadAllText(podfilePath);

            // 使用正则表达式替换 use_frameworks! :linkage => :static 为 use_frameworks!
            string pattern = @"use_frameworks!\s*:linkage\s*=>\s*:static";
            string replaced = Regex.Replace(content, pattern, "use_frameworks!");
            replaced += "\n";
            replaced +=
                "# 在安装 CocoaPods 依赖时，抑制未使用 Master Specs 仓库的警告\ninstall! 'cocoapods', :warn_for_unused_master_specs_repo => false\n";

            File.WriteAllText(podfilePath, replaced);

            Debug.Log("[iOSBuildEventHandler] Podfile use_frameworks bug fixed.");
        }

        /// <summary>
        /// 执行 CocoaPods 安装
        /// 自动运行 pod install 命令
        /// </summary>
        private static void RunPodInstall(string path)
        {
            Debug.Log("[iOSBuildEventHandler] Running pod install...");

            var podFilePath = Path.Combine(path, "Podfile");
            Debug.Log($"[iOSBuildEventHandler] Pod file: {podFilePath}");

            if (!File.Exists(podFilePath))
            {
                Debug.LogError("[iOSBuildEventHandler] Podfile not found, skipping pod install.");
                return;
            }

            // 创建 shell 脚本
            string shPath = Path.Combine(path, "podinstall.sh");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#!/bin/sh");
            sb.AppendLine("export LANG=en_US.UTF-8");
            sb.AppendLine("cd " + path);
            sb.AppendLine("/opt/homebrew/bin/pod install");

            using (FileStream file = new FileStream(shPath, FileMode.Create))
            {
                var bts = Encoding.UTF8.GetBytes(sb.ToString());
                file.Write(bts, 0, bts.Length);
                file.Flush();
            }

            // 执行权限 shell
            Process chmodProcess = new Process();
            chmodProcess.StartInfo.FileName = "chmod";
            chmodProcess.StartInfo.Arguments = "+x podinstall.sh";
            chmodProcess.StartInfo.CreateNoWindow = true;
            chmodProcess.StartInfo.UseShellExecute = false;
            chmodProcess.StartInfo.WorkingDirectory = path;

            chmodProcess.Start();
            chmodProcess.WaitForExit();
            chmodProcess.Close();

            // 执行生成 shell
            Process process = new Process();
            process.StartInfo.FileName = "sh";
            process.StartInfo.Arguments = "podinstall.sh";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = path;

            process.Start();

            // 读取输出和错误流
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // 打印输出和错误信息
            Debug.Log("[iOSBuildEventHandler] Pod install output: " + output);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[iOSBuildEventHandler] Pod install error: " + error);
            }

            process.Close();

            Debug.Log("[iOSBuildEventHandler] Pod install completed.");
        }
#endif
    }
}

