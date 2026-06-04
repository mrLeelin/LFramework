using UnityEngine;
using Sirenix.OdinInspector;
using System.IO;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Android 平台配置
    /// </summary>
    [CreateAssetMenu(fileName = "AndroidSetting", menuName = "LFramework/Settings/AndroidSetting")]
    public class AndroidSetting : BaseSetting
    {
        
        [Title("Android 配置")]
        [SerializeField] private string bundleIdentifier;
        
        
        [Title("Android 签名配置")]
        [InfoBox("APK/AAB 构建时，会按 Debug/Release 构建模式读取对应的签名配置。")]
        [ToggleLeft]
        [SerializeField] private bool useCustomKeystore = true;

        [Title("Debug 签名配置")]
        [ShowIf(nameof(useCustomKeystore))]
        [FilePath(RequireExistingPath = true, Extensions = "keystore,jks")]
        [LabelText("Debug Keystore 文件")]
        [SerializeField] private string debugKeystorePath = "BuildBat/keystore/debug.keystore";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Debug Keystore 密码")]
        [SerializeField] private string debugKeystorePass = "123456";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Debug Alias 名称")]
        [SerializeField] private string debugKeyaliasName = "debug";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Debug Alias 密码")]
        [SerializeField] private string debugKeyaliasPass = "123456";

        [Title("Release 签名配置")]
        [ShowIf(nameof(useCustomKeystore))]
        [FilePath(RequireExistingPath = true, Extensions = "keystore,jks")]
        [LabelText("Release Keystore 文件")]
        [SerializeField] private string keystorePath = "BuildBat/keystore/partygo.keystore";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Release Keystore 密码")]
        [SerializeField] private string keystorePass = "123456";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Release Alias 名称")]
        [SerializeField] private string keyaliasName = "partygo";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Release Alias 密码")]
        [SerializeField] private string keyaliasPass = "123456";

        public bool UseCustomKeystore => useCustomKeystore;
        public string DebugKeystorePath => debugKeystorePath;
        public string DebugKeystorePass => debugKeystorePass;
        public string DebugKeyaliasName => debugKeyaliasName;
        public string DebugKeyaliasPass => debugKeyaliasPass;
        public string KeystorePath => keystorePath;
        public string KeystorePass => keystorePass;
        public string KeyaliasName => keyaliasName;
        public string KeyaliasPass => keyaliasPass;
        public string ReleaseKeystorePath => keystorePath;
        public string ReleaseKeystorePass => keystorePass;
        public string ReleaseKeyaliasName => keyaliasName;
        public string ReleaseKeyaliasPass => keyaliasPass;
        public string BundleIdentifier => bundleIdentifier;

        public AndroidKeystoreConfig GetKeystoreConfig(bool isRelease)
        {
            return isRelease
                ? new AndroidKeystoreConfig("Release", keystorePath, keystorePass, keyaliasName, keyaliasPass)
                : new AndroidKeystoreConfig("Debug", debugKeystorePath, debugKeystorePass, debugKeyaliasName, debugKeyaliasPass);
        }

        public override bool Validate(out string errorMessage)
        {
            if (!ValidateBundleIdentifier(out errorMessage))
            {
                return false;
            }

            if (!useCustomKeystore)
            {
                errorMessage = null;
                return true;
            }

            return ValidateKeystoreConfig(GetKeystoreConfig(false), out errorMessage) &&
                   ValidateKeystoreConfig(GetKeystoreConfig(true), out errorMessage);
        }

        public bool ValidateForBuild(bool isRelease, out string errorMessage)
        {
            return ValidateForBuild(isRelease, true, out errorMessage);
        }

        public bool ValidateForBuild(bool isRelease, bool requireKeystore, out string errorMessage)
        {
            if (!ValidateBundleIdentifier(out errorMessage))
            {
                return false;
            }

            if (!requireKeystore || !useCustomKeystore)
            {
                errorMessage = null;
                return true;
            }

            return ValidateKeystoreConfig(GetKeystoreConfig(isRelease), out errorMessage);
        }

        private bool ValidateBundleIdentifier(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(bundleIdentifier))
            {
                errorMessage = "Bundle Identifier 不存在";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateKeystoreConfig(AndroidKeystoreConfig config, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(config.KeystorePath))
            {
                errorMessage = $"{config.BuildMode} Keystore Path 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.KeystorePass))
            {
                errorMessage = $"{config.BuildMode} Keystore Password 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.KeyaliasName))
            {
                errorMessage = $"{config.BuildMode} Key Alias Name 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.KeyaliasPass))
            {
                errorMessage = $"{config.BuildMode} Key Alias Password 不能为空";
                return false;
            }

            string resolvedPath = ResolveKeystorePath(config.KeystorePath);
            if (!File.Exists(resolvedPath))
            {
                errorMessage = $"{config.BuildMode} Keystore 文件不存在: {resolvedPath}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static string ResolveKeystorePath(string keystorePath)
        {
            return Path.IsPathRooted(keystorePath)
                ? Path.GetFullPath(keystorePath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", keystorePath));
        }
    }

    public readonly struct AndroidKeystoreConfig
    {
        public AndroidKeystoreConfig(
            string buildMode,
            string keystorePath,
            string keystorePass,
            string keyaliasName,
            string keyaliasPass)
        {
            BuildMode = buildMode;
            KeystorePath = keystorePath;
            KeystorePass = keystorePass;
            KeyaliasName = keyaliasName;
            KeyaliasPass = keyaliasPass;
        }

        public string BuildMode { get; }
        public string KeystorePath { get; }
        public string KeystorePass { get; }
        public string KeyaliasName { get; }
        public string KeyaliasPass { get; }
    }
}
