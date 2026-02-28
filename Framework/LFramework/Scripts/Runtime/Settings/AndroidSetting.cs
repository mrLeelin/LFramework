using UnityEngine;
using Sirenix.OdinInspector;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Android 平台配置
    /// </summary>
    [CreateAssetMenu(fileName = "AndroidSetting", menuName = "LFramework/Settings/AndroidSetting")]
    public class AndroidSetting : BaseSetting
    {
        [FoldoutGroup("Android 配置")]
        [SerializeField] private string bundleIdentifier = "com.company.game";

        [FoldoutGroup("Android 配置")]
        [SerializeField] private int minSdkVersion = 21;

        [FoldoutGroup("Android 配置")]
        [SerializeField] private int targetSdkVersion = 30;

        [FoldoutGroup("Android 配置")]
        [SerializeField] private bool useIL2CPP = true;

        public string BundleIdentifier => bundleIdentifier;
        public int MinSdkVersion => minSdkVersion;
        public int TargetSdkVersion => targetSdkVersion;
        public bool UseIL2CPP => useIL2CPP;

        public override bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(bundleIdentifier))
            {
                errorMessage = "Bundle Identifier 不能为空";
                return false;
            }

            if (minSdkVersion > targetSdkVersion)
            {
                errorMessage = "Min SDK Version 不能大于 Target SDK Version";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
