using System;
using GameFramework;
using GameFramework.Resource;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源组件（薄代理，路由到 ResourceManager）
    /// </summary>
    public sealed class ResourceComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IResourceManager _resourceManager;
        private ResourceHelperBase _resourceHelper;

        private bool _forceUnloadUnusedAssets;
        private float m_LastUnloadUnusedAssetsOperationElapseSeconds = 0f;
        private AsyncOperation m_AsyncOperation = null;
        private bool m_PerformGCCollect = false;
        private bool m_PreorderUnloadUnusedAssets = false;
        // ─── 配置属性（由 Setting 注入） ───

        /// <summary>
        /// 资源模式
        /// </summary>
        [SerializeField] private ResourceMode _resourceMode;

        /// <summary>
        /// 资源辅助器类型名称
        /// </summary>
        [SerializeField]
        private string m_ResourceHelperTypeName = "UnityGameFramework.Runtime.AddressableResourceHelper";

        [SerializeField] private ResourceHelperBase m_CustomResourceHelper = null;

        /// <summary>
        /// 最小卸载间隔（秒）
        /// </summary>
        [SerializeField] private float _minUnloadInterval;

        /// <summary>
        /// 最大卸载间隔（秒）
        /// </summary>
        [SerializeField] private float _maxUnloadInterval;


        /// <summary>
        /// YooAsset 包名称
        /// </summary>
        [SerializeField] private string _yooAssetPackageName;

        /// <summary>
        /// YooAsset 运行模式
        /// </summary>
        [SerializeField] private YooAssetPlayMode _yooAssetPlayMode;


        // ─── 生命周期 ───

        /// <summary>
        ///  Resource Mode
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        public string ResourceHelperTypeName => m_ResourceHelperTypeName;

        public float MinUnloadInterval => _minUnloadInterval;

        public float MaxUnloadInterval => _maxUnloadInterval;

        public string YooAssetPackageName => _yooAssetPackageName;

        public YooAssetPlayMode YooAssetsPlayModel => _yooAssetPlayMode;

        /// <summary>
        /// 初始化组件
        /// </summary>
        public override void AwakeComponent()
        {
            _resourceManager = GameFrameworkEntry.GetModule<IResourceManager>();
            _resourceManager.SetResourceMode(_resourceMode);
        }

        /// <summary>
        /// 启动组件
        /// </summary>
        public override void StartComponent()
        {
            // 设置路径
            _resourceManager.SetReadOnlyPath(Application.streamingAssetsPath);
            _resourceManager.SetReadWritePath(Application.persistentDataPath);

            CreateInstance();

            // 创建 ResourceHelper
            _resourceHelper = Helper.CreateHelper(m_ResourceHelperTypeName, m_CustomResourceHelper);

            _resourceHelper.name = "Resource Helper";
            Transform transform = _resourceHelper.transform;
            transform.SetParent(Instance);
            transform.localScale = Vector3.one;

            _resourceManager.SetResourceHelper(_resourceHelper);

            // 配置 YooAsset ResourceHelper
            if (_resourceHelper is YooAssetResourceHelper yooHelper)
            {
                yooHelper.PackageName = _yooAssetPackageName;
                yooHelper.PlayMode = _yooAssetPlayMode;
            }
        }

        /// <summary>
        /// 轮询组件
        /// </summary>
        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            m_LastUnloadUnusedAssetsOperationElapseSeconds += Time.unscaledDeltaTime;

            if (m_AsyncOperation == null && (_forceUnloadUnusedAssets ||
                                             m_LastUnloadUnusedAssetsOperationElapseSeconds >=
                                             _maxUnloadInterval || m_PreorderUnloadUnusedAssets &&
                                             m_LastUnloadUnusedAssetsOperationElapseSeconds >=
                                             _minUnloadInterval))
            {
                Log.Info("Unload unused assets...");
                _forceUnloadUnusedAssets = false;
                m_PreorderUnloadUnusedAssets = false;
                m_LastUnloadUnusedAssetsOperationElapseSeconds = 0f;
                m_AsyncOperation = Resources.UnloadUnusedAssets();
            }

            if (m_AsyncOperation != null && m_AsyncOperation.isDone)
            {
                m_AsyncOperation = null;
                if (m_PerformGCCollect)
                {
                    Log.Info("GC.Collect...");
                    m_PerformGCCollect = false;
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// 关闭组件
        /// </summary>
        public override void ShutDown()
        {
            _resourceManager = null;
            _resourceHelper = null;
        }

        // ─── 公共 API ───

        /// <summary>
        /// 初始化资源
        /// </summary>
        /// <param name="callback">初始化完成回调</param>
        public void InitResources(InitResourcesCompleteCallback callback)
        {
            _resourceManager.InitResources(callback);
        }

        /// <summary>
        /// 查询资源是否存在
        /// </summary>
        public HasAssetResult HasAsset(string assetName)
        {
            return _resourceManager.HasAsset(assetName);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public void LoadAsset(string assetName, LoadAssetCallbacks callbacks)
        {
            _resourceManager.LoadAsset(assetName, null, DefaultPriority, callbacks, null);
        }

        /// <summary>
        /// 加载资源（指定类型）
        /// </summary>
        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks)
        {
            _resourceManager.LoadAsset(assetName, assetType, DefaultPriority, callbacks, null);
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callbacks"></param>
        public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks) =>
            _resourceManager.InstantiateAsset(assetName, callbacks, null);

        /// <summary>
        /// 实例化资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callbacks"></param>
        /// <param name="userData"></param>
        public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData) =>
            _resourceManager.InstantiateAsset(assetName, callbacks, userData);

        /// <summary>
        /// 加载资源（指定优先级）
        /// </summary>
        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks callbacks)
        {
            _resourceManager.LoadAsset(assetName, null, priority, callbacks, null);
        }

        /// <summary>
        /// 加载资源（全参数版本）
        /// </summary>
        public void LoadAsset(string assetName, Type assetType, int priority,
            LoadAssetCallbacks callbacks, object userData)
        {
            _resourceManager.LoadAsset(assetName, assetType, priority, callbacks, userData);
        }

        /// <summary>
        /// 加载资源（全参数版本）
        /// </summary>
        public void LoadAsset(string assetName, Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            _resourceManager.LoadAsset(assetName, assetType, 0, callbacks, userData);
        }

        /// <summary>
        /// 加载资源（V2 版本，返回 IResourceHandle）
        /// </summary>
        public void LoadAssetV2(string assetName, Type assetType,
            LoadAssetCallbacksV2 callbacks, object userData)
        {
            _resourceManager.LoadAssetV2(assetName, assetType, callbacks, userData);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public void UnloadAsset(object asset)
        {
            _resourceManager.UnloadAsset(asset);
        }

        /// <summary>
        /// 预订执行释放未被使用的资源。
        /// </summary>
        /// <param name="performGCCollect">是否使用垃圾回收。</param>
        public void UnloadUnusedAssets(bool performGCCollect)
        {
            m_PreorderUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }

        /// <summary>
        /// 强制执行释放未被使用的资源。
        /// </summary>
        /// <param name="performGCCollect">是否使用垃圾回收。</param>
        public void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            _forceUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }


        /// <summary>
        /// 加载场景
        /// </summary>
        public void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks)
        {
            _resourceManager.LoadScene(sceneAssetName, DefaultPriority, callbacks, null);
        }

        /// <summary>
        /// 加载场景（全参数版本）
        /// </summary>
        public void LoadScene(string sceneAssetName, int priority,
            LoadSceneCallbacks callbacks, object userData)
        {
            _resourceManager.LoadScene(sceneAssetName, priority, callbacks, userData);
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks)
        {
            _resourceManager.UnloadScene(sceneAssetName, callbacks, null);
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            _resourceManager.LoadBinary(binaryAssetName, callbacks, userData);
        }


        // ─── 辅助方法 ───
    }
}