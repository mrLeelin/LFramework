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
        [SerializeField] private string targetOSVersion = "12.0";
        [FoldoutGroup("iOS 配置")]
        [SerializeField] private string mobileProvisionUUid;

        [FoldoutGroup("iOS 配置")]
        [SerializeField] private string appleDevelopTeamId ;
        [FoldoutGroup("iOS 配置")]
        [SerializeField] private string codeSignIdentity ;

        
        [FoldoutGroup("iOS 配置")]
        [SerializeField] private bool requiresFullScreen = true;
        [FoldoutGroup("deep link")] [SerializeField]
        private string urlScheme;
        [FoldoutGroup("deep link")] [SerializeField]
        private string bundleURLName;

        [FoldoutGroup("App Controller Name")]
        [SerializeField] private string appControllerName;
        
        
        [FoldoutGroup("权限描述")]
        [SerializeField, TextArea] private string cameraUsageDescription;

        [FoldoutGroup("权限描述")]
        [SerializeField, TextArea] private string locationUsageDescription;
        
        public string TargetOSVersion => targetOSVersion;
        public bool RequiresFullScreen => requiresFullScreen;
        public string CameraUsageDescription => cameraUsageDescription;
        public string LocationUsageDescription => locationUsageDescription;
        
        public string CodeSignIdentity => codeSignIdentity;
        
        public string URLScheme => urlScheme;
        
        public string BundleURLName => bundleURLName;
        public string AppControllerName => appControllerName;
        


        public string MobileProvisionUUid => mobileProvisionUUid;
        public string AppleDevelopTeamId => appleDevelopTeamId;
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}
