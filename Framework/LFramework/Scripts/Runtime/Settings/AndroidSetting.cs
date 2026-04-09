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
        [Title("Android 签名配置")]
        [InfoBox("Release 构建且非导出 Android Project 时，会读取这里的签名配置。")]
        [ToggleLeft]
        [SerializeField] private bool useCustomKeystore = true;

        [ShowIf(nameof(useCustomKeystore))]
        [FilePath(RequireExistingPath = true, Extensions = "keystore,jks")]
        [LabelText("Keystore 文件")]
        [SerializeField] private string keystorePath = "BuildBat/keystore/partygo.keystore";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Keystore 密码")]
        [SerializeField] private string keystorePass = "123456";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Alias 名称")]
        [SerializeField] private string keyaliasName = "partygo";

        [ShowIf(nameof(useCustomKeystore))]
        [LabelText("Alias 密码")]
        [SerializeField] private string keyaliasPass = "123456";

        public bool UseCustomKeystore => useCustomKeystore;
        public string KeystorePath => keystorePath;
        public string KeystorePass => keystorePass;
        public string KeyaliasName => keyaliasName;
        public string KeyaliasPass => keyaliasPass;
        
        public override bool Validate(out string errorMessage)
        {
            if (!useCustomKeystore)
            {
                errorMessage = null;
                return true;
            }

            if (string.IsNullOrWhiteSpace(keystorePath))
            {
                errorMessage = "Keystore Path 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(keystorePass))
            {
                errorMessage = "Keystore Password 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(keyaliasName))
            {
                errorMessage = "Key Alias Name 不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(keyaliasPass))
            {
                errorMessage = "Key Alias Password 不能为空";
                return false;
            }

            string resolvedPath = Path.IsPathRooted(keystorePath)
                ? Path.GetFullPath(keystorePath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", keystorePath));
            if (!File.Exists(resolvedPath))
            {
                errorMessage = $"Keystore 文件不存在: {resolvedPath}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
