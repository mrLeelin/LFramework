
using GameFramework.Resource;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 资源组件配置，支持 Addressable 和 YooAsset 两种资源模式的隔离配置
    /// </summary>
    [CreateAssetMenu(order = 1, fileName = "ResourceComponentSetting",
        menuName = "LFramework/Settings/ResourceComponentSetting")]
    public class ResourceComponentSetting : ComponentSetting
    {
        #region 通用设置

        /// <summary>
        /// 资源模式
        /// </summary>
        [BoxGroup("通用设置")]
        [LabelText("资源模式")]
        [SerializeField]
        private ResourceMode _resourceMode = ResourceMode.YooAsset;

        /// <summary>
        /// 资源辅助器类型名称
        /// </summary>
        [BoxGroup("通用设置")]
        [LabelText("资源辅助器类型名称")]
        [SerializeField]
        private string m_ResourceHelperTypeName = "LFramework.Runtime.YooAssetResourceHelper";

        [SerializeField]
        private SettingHelperBase m_CustomResourceHelper = null;
        #endregion

        #region 资源释放

        /// <summary>
        /// 最小卸载未使用资源间隔（秒）
        /// </summary>
        [BoxGroup("资源释放")]
        [LabelText("最小卸载间隔（秒）")]
        [SerializeField]
        private float _minUnloadInterval = 60f;

        /// <summary>
        /// 最大卸载未使用资源间隔（秒）
        /// </summary>
        [BoxGroup("资源释放")]
        [LabelText("最大卸载间隔（秒）")]
        [SerializeField]
        private float _maxUnloadInterval = 300f;

        #endregion
        

        #region YooAsset 设置

        /// <summary>
        /// YooAsset 资源包名称
        /// </summary>
        [BoxGroup("YooAsset 设置")]
        [LabelText("资源包名称")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private string _yooAssetPackageName = "DefaultPackage";

        /// <summary>
        /// YooAsset 运行模式
        /// </summary>
        [BoxGroup("YooAsset 设置")]
        [LabelText("运行模式")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private YooAssetPlayMode _yooAssetPlayMode = YooAssetPlayMode.EditorSimulateMode;
        
        #endregion

        #region Addreaable 设置

        [BoxGroup("Addressable 设置")]
        [SerializeField]
        [LabelText("资源包名称")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        private string _hotfixProfileName;
        

        #endregion


        /// <summary>
        /// 热更新Profile地址
        /// </summary>
        public string HotfixProfileName => _hotfixProfileName;
        /// <summary>
        /// 资源模式
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// YooAssets DefaultPackageName
        /// </summary>
        public string YooAssetPackageName => _yooAssetPackageName;

    }
}
