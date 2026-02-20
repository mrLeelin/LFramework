using System;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// iOS 代码修复器
    /// 负责修复已知的 Unity 和第三方 SDK 问题
    /// </summary>
    public class iOSCodeFixer
    {
        private readonly iOSBuildConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">构建配置</param>
        public iOSCodeFixer(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 应用所有修复
        /// </summary>
        public void ApplyAllFixes()
        {
#if UNITY_IOS
            iOSBuildLogger.LogStep("Code fixes");

            // 修复 Facebook Deep Link 冷启动崩溃
            FixFacebookColdStart();

            // 修复 Deep Link 接收问题
            FixDeepLinkReception();

            // 修复 Podfile 冲突问题
            FixPodfileConflict();

            iOSBuildLogger.LogSuccess("Code fixes");
#else
            iOSBuildLogger.LogWarning("Skipping code fixes (not on iOS platform)");
#endif
        }

#if UNITY_IOS
        /// <summary>
        /// 修复 Facebook Deep Link 冷启动崩溃问题
        /// 问题：当应用通过 Deep Link 冷启动时，Facebook SDK 会崩溃
        /// 解决方案：修改 UnityAppController.mm 中的 isBackgroundLaunchOptions 返回值为 NO
        /// </summary>
        private void FixFacebookColdStart()
        {
            iOSBuildLogger.LogInfo("Fixing Facebook cold start crash...");

            string fullPath = Path.Combine(_config.OutputPath, iOSBuildConstants.UNITY_APP_CONTROLLER_PATH);

            if (!File.Exists(fullPath))
            {
                iOSBuildLogger.LogWarning($"UnityAppController.mm not found: {fullPath}");
                return;
            }

            try
            {
                string data = File.ReadAllText(fullPath);

                // 使用正则表达式替换 isBackgroundLaunchOptions 方法的返回值
                // 将 return YES; 替换为 return NO;
                data = Regex.Replace(
                    data,
                    iOSBuildConstants.REGEX_BACKGROUND_LAUNCH_OPTIONS,
                    "$1NO$2");

                File.WriteAllText(fullPath, data);

                iOSBuildLogger.LogInfo("Facebook cold start crash fix applied");
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogError($"Failed to fix Facebook cold start crash: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 修复 Deep Link 接收问题
        /// 问题：Unity 默认的 AppController 无法正确处理 Deep Link
        /// 解决方案：设置自定义 AppController 类名
        /// </summary>
        private void FixDeepLinkReception()
        {
            iOSBuildLogger.LogInfo("Fixing Deep Link reception...");

            // 1. 修改 Info.plist
            FixDeepLinkInPlist();

            // 2. 修改 main.mm（可选，保险一点）
            FixDeepLinkInMainMM();

            iOSBuildLogger.LogInfo("Deep Link reception fix applied");
        }

        /// <summary>
        /// 在 Info.plist 中设置自定义 AppController
        /// </summary>
        private void FixDeepLinkInPlist()
        {
            string plistPath = Path.Combine(_config.OutputPath, iOSBuildConstants.INFO_PLIST_PATH);

            try
            {
                PlistDocument plist = new PlistDocument();
                plist.ReadFromFile(plistPath);

                PlistElementDict rootDict = plist.root;

                // 设置自定义 AppController 类名
                rootDict.SetString(
                    iOSBuildConstants.PLIST_KEY_APP_CONTROLLER,
                    _config.CustomAppControllerName);

                // AppsFlyer 插件启用 Swizzle
                rootDict.SetBoolean(iOSBuildConstants.PLIST_KEY_APPSFLYER_SWIZZLE, true);

                plist.WriteToFile(plistPath);

                iOSBuildLogger.LogInfo($"Set UnityAppControllerClass={_config.CustomAppControllerName} in Info.plist");
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogError($"Failed to fix Deep Link in Info.plist: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 在 main.mm 中设置自定义 AppController
        /// </summary>
        private void FixDeepLinkInMainMM()
        {
            string mainMMPath = Path.Combine(_config.OutputPath, iOSBuildConstants.MAIN_MM_PATH);

            if (!File.Exists(mainMMPath))
            {
                iOSBuildLogger.LogWarning($"main.mm not found: {mainMMPath}");
                return;
            }

            try
            {
                string content = File.ReadAllText(mainMMPath);

                // 使用正则表达式替换 AppControllerClassName
                string replacement = $@"const char* AppControllerClassName = ""{_config.CustomAppControllerName}"";";

                if (Regex.IsMatch(content, iOSBuildConstants.REGEX_APP_CONTROLLER_NAME))
                {
                    content = Regex.Replace(
                        content,
                        iOSBuildConstants.REGEX_APP_CONTROLLER_NAME,
                        replacement);

                    File.WriteAllText(mainMMPath, content);

                    iOSBuildLogger.LogInfo("Updated AppControllerClassName in main.mm");
                }
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogError($"Failed to fix Deep Link in main.mm: {ex.Message}");
                // 这个修复是可选的，不抛出异常
            }
        }

        /// <summary>
        /// 修复 Firebase + Facebook Podfile 冲突问题
        /// 问题：use_frameworks! :linkage => :static 会导致 Firebase 和 Facebook SDK 冲突
        /// 解决方案：移除 :linkage => :static，改为 use_frameworks!
        /// </summary>
        private void FixPodfileConflict()
        {
            iOSBuildLogger.LogInfo("Fixing Podfile use_frameworks conflict...");

            string podfilePath = Path.Combine(_config.OutputPath, iOSBuildConstants.PODFILE_PATH);

            if (!File.Exists(podfilePath))
            {
                iOSBuildLogger.LogWarning($"Podfile not found: {podfilePath}");
                return;
            }

            try
            {
                string content = File.ReadAllText(podfilePath);

                // 使用正则表达式替换 use_frameworks! :linkage => :static 为 use_frameworks!
                string replaced = Regex.Replace(
                    content,
                    iOSBuildConstants.REGEX_USE_FRAMEWORKS,
                    "use_frameworks!");

                // 添加 CocoaPods 警告抑制配置
                replaced += iOSBuildConstants.COCOAPODS_WARN_SUPPRESSION;

                File.WriteAllText(podfilePath, replaced);

                iOSBuildLogger.LogInfo("Podfile use_frameworks conflict fixed");
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogError($"Failed to fix Podfile conflict: {ex.Message}");
                throw;
            }
        }
#endif
    }
}
