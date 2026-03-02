
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
        private ResourceMode _resourceMode = ResourceMode.Unspecified;

        /// <summary>
        /// 资源辅助器类型名称
        /// </summary>
        [BoxGroup("通用设置")]
        [LabelText("资源辅助器类型名称")]
        [SerializeField]
        private string _resourceHelperTypeName = "UnityGameFramework.Runtime.AddressableResourceHelper";

        /// <summary>
        /// 自定义资源辅助器
        /// </summary>
        [BoxGroup("通用设置")]
        [LabelText("自定义资源辅助器")]
        [SerializeField]
        private ResourceHelperBase _customResourceHelper = null;

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

        #region Addressable 设置

        /// <summary>
        /// Addressable 是否自动初始化
        /// </summary>
        [BoxGroup("Addressable 设置")]
        [LabelText("自动初始化")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        [SerializeField]
        private bool _addressableAutoInitialize = true;

        /// <summary>
        /// Addressable 远程目录 URL
        /// </summary>
        [BoxGroup("Addressable 设置")]
        [LabelText("远程目录 URL")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        [SerializeField]
        private string _addressableRemoteCatalogUrl = string.Empty;

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

        /// <summary>
        /// YooAsset 主机服务器 URL
        /// </summary>
        [BoxGroup("YooAsset 设置")]
        [LabelText("主机服务器 URL")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private string _yooAssetHostServerUrl = string.Empty;

        /// <summary>
        /// YooAsset 备用主机服务器 URL
        /// </summary>
        [BoxGroup("YooAsset 设置")]
        [LabelText("备用主机服务器 URL")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private string _yooAssetFallbackHostServerUrl = string.Empty;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取资源模式
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// 获取资源辅助器类型名称
        /// </summary>
        public string ResourceHelperTypeName => _resourceHelperTypeName;

        /// <summary>
        /// 获取自定义资源辅助器
        /// </summary>
        public ResourceHelperBase CustomResourceHelper => _customResourceHelper;

        /// <summary>
        /// 获取最小卸载间隔（秒）
        /// </summary>
        public float MinUnloadInterval => _minUnloadInterval;

        /// <summary>
        /// 获取最大卸载间隔（秒）
        /// </summary>
        public float MaxUnloadInterval => _maxUnloadInterval;

        /// <summary>
        /// 获取 Addressable 是否自动初始化
        /// </summary>
        public bool AddressableAutoInitialize => _addressableAutoInitialize;

        /// <summary>
        /// 获取 Addressable 远程目录 URL
        /// </summary>
        public string AddressableRemoteCatalogUrl => _addressableRemoteCatalogUrl;

        /// <summary>
        /// 获取 YooAsset 资源包名称
        /// </summary>
        public string YooAssetPackageName => _yooAssetPackageName;

        /// <summary>
        /// 获取 YooAsset 运行模式
        /// </summary>
        public YooAssetPlayMode YooAssetPlayMode => _yooAssetPlayMode;

        /// <summary>
        /// 获取 YooAsset 主机服务器 URL
        /// </summary>
        public string YooAssetHostServerUrl => _yooAssetHostServerUrl;

        /// <summary>
        /// 获取 YooAsset 备用主机服务器 URL
        /// </summary>
        public string YooAssetFallbackHostServerUrl => _yooAssetFallbackHostServerUrl;

        #endregion
    }
}
