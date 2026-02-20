using System;
using System.IO;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// iOS Entitlements 配置器
    /// 负责配置应用权限，包括 Keychain、Push、Game Center、IAP、Sign in with Apple 等
    /// </summary>
    public class iOSEntitlementsConfigurator
    {
        private readonly iOSBuildConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">构建配置</param>
        public iOSEntitlementsConfigurator(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 执行 Entitlements 配置
        /// </summary>
        public void Configure()
        {
#if UNITY_IOS
            iOSBuildLogger.LogStep("Entitlements configuration");

            string projectPath = PBXProject.GetPBXProjectPath(_config.OutputPath);
            string entitlementsPath = Path.Combine(_config.OutputPath, iOSBuildConstants.ENTITLEMENTS_FILE_NAME);

            // 确保 entitlements 文件存在
            EnsureEntitlementsFileExists(entitlementsPath);

            // 读取 PBXProject（需要获取 Target GUID）
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);

            // 创建 ProjectCapabilityManager
            var manager = new ProjectCapabilityManager(
                projectPath,
                iOSBuildConstants.ENTITLEMENTS_FILE_NAME,
                null,
                pbxProject.GetUnityMainTargetGuid());

            // 添加各种能力
            AddKeychainSharing(manager);
            AddPushNotifications(manager);
            AddGameCenter(manager);
            AddInAppPurchase(manager);
            AddSignInWithApple(manager);

            // 写入文件
            manager.WriteToFile();

            iOSBuildLogger.LogSuccess("Entitlements configuration");
#else
            iOSBuildLogger.LogWarning("Skipping Entitlements configuration (not on iOS platform)");
#endif
        }

#if UNITY_IOS
        /// <summary>
        /// 确保 Entitlements 文件存在
        /// 如果不存在则创建空文件
        /// </summary>
        private void EnsureEntitlementsFileExists(string entitlementsPath)
        {
            if (!File.Exists(entitlementsPath))
            {
                var emptyEntitlement = new PlistDocument();
                emptyEntitlement.WriteToFile(entitlementsPath);
                iOSBuildLogger.LogInfo($"Created entitlements file: {entitlementsPath}");
            }
        }

        /// <summary>
        /// 添加 Keychain Sharing 能力
        /// 允许应用访问 Keychain 共享组
        /// </summary>
        private void AddKeychainSharing(ProjectCapabilityManager manager)
        {
            manager.AddKeychainSharing(new[]
            {
                $"$(AppIdentifierPrefix){Application.identifier}"
            });

            iOSBuildLogger.LogInfo("Added Keychain Sharing capability");
        }

        /// <summary>
        /// 添加 Push Notifications 能力
        /// 区分开发和生产环境
        /// </summary>
        private void AddPushNotifications(ProjectCapabilityManager manager)
        {
            manager.AddPushNotifications(_config.IsDevelopment);

            string environment = _config.IsDevelopment ? "Development" : "Production";
            iOSBuildLogger.LogInfo($"Added Push Notifications capability ({environment})");
        }

        /// <summary>
        /// 添加 Game Center 能力
        /// </summary>
        private void AddGameCenter(ProjectCapabilityManager manager)
        {
            manager.AddGameCenter();
            iOSBuildLogger.LogInfo("Added Game Center capability");
        }

        /// <summary>
        /// 添加 In-App Purchase 能力
        /// </summary>
        private void AddInAppPurchase(ProjectCapabilityManager manager)
        {
            manager.AddInAppPurchase();
            iOSBuildLogger.LogInfo("Added In-App Purchase capability");
        }

        /// <summary>
        /// 添加 Sign in with Apple 能力
        /// </summary>
        private void AddSignInWithApple(ProjectCapabilityManager manager)
        {
            manager.AddSignInWithApple();
            iOSBuildLogger.LogInfo("Added Sign in with Apple capability");
        }
#endif
    }
}
