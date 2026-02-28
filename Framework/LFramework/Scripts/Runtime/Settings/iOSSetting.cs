using UnityEngine;
using Sirenix.OdinInspector;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// iOS 平台配置
    /// </summary>
    [CreateAssetMenu(fileName = "iOSSetting", menuName = "LFramework/Settings/iOSSetting")]
    public class iOSSetting : BaseSetting
    {
        [FoldoutGroup("iOS 配置")]
        [SerializeField] private string bundleIdentifier = "com.company.game";

        [FoldoutGroup("iOS 配置")]
        [SerializeField] private string targetOSVersion = "12.0";

        [FoldoutGroup("iOS 配置")]
        [SerializeField] private bool requiresFullScreen = true;

        [FoldoutGroup("权限描述")]
        [SerializeField, TextArea] private string cameraUsageDescription;

        [FoldoutGroup("权限描述")]
        [SerializeField, TextArea] private string locationUsageDescription;

        public string BundleIdentifier => bundleIdentifier;
        public string TargetOSVersion => targetOSVersion;
        public bool RequiresFullScreen => requiresFullScreen;
        public string CameraUsageDescription => cameraUsageDescription;
        public string LocationUsageDescription => locationUsageDescription;

        public override bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(bundleIdentifier))
            {
                errorMessage = "Bundle Identifier 不能为空";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
