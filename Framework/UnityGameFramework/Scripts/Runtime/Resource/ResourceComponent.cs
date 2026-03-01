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
        private AsyncOperation _unloadOperation;
        private float _lastUnloadTime;

        // ─── 配置属性（由 Setting 注入） ───

        /// <summary>
        /// 资源模式
        /// </summary>
        public ResourceMode ResourceMode { get; set; } = ResourceMode.Unspecified;

        /// <summary>
        /// 最小卸载间隔（秒）
        /// </summary>
        public float MinUnloadInterval { get; set; } = 60f;

        /// <summary>
        /// 最大卸载间隔（秒）
        /// </summary>
        public float MaxUnloadInterval { get; set; } = 300f;

        /// <summary>
        /// 资源辅助器类型名称
        /// </summary>
        public string ResourceHelperTypeName { get; set; }

        /// <summary>
        /// 自定义资源辅助器
        /// </summary>
        public ResourceHelperBase CustomResourceHelper { get; set; }

        /// <summary>
        /// 加载资源代理辅助器类型名称
        /// </summary>
        public string LoadResourceAgentHelperTypeName { get; set; }

        /// <summary>
        /// 自定义加载资源代理辅助器
        /// </summary>
        public LoadResourceAgentHelperBase CustomLoadResourceAgentHelper { get; set; }

        /// <summary>
        /// 加载资源代理辅助器数量
        /// </summary>
        public int LoadResourceAgentHelperCount { get; set; } = 3;

        /// <summary>
        /// YooAsset 包名称
        /// </summary>
        public string YooAssetPackageName { get; set; } = "DefaultPackage";

        /// <summary>
        /// YooAsset 运行模式
        /// </summary>
        public YooAssetPlayMode YooAssetPlayMode { get; set; } = YooAssetPlayMode.EditorSimulateMode;

        /// <summary>
        /// YooAsset 资源服务器地址
        /// </summary>
        public string YooAssetHostServerUrl { get; set; }

        /// <summary>
        /// YooAsset 备用服务器地址
        /// </summary>
        public string YooAssetFallbackHostServerUrl { get; set; }

        // ─── 生命周期 ───

        /// <summary>
        /// 初始化组件
        /// </summary>
        public override void AwakeComponent()
        {
            _resourceManager = GameFrameworkEntry.GetModule<IResourceManager>();
            _resourceManager.SetResourceMode(ResourceMode);
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
            _resourceHelper = CreateComponentHelper<ResourceHelperBase>(
                ResourceHelperTypeName, CustomResourceHelper);
            _resourceManager.SetResourceHelper(_resourceHelper);

            // 配置 YooAsset ResourceHelper
            if (_resourceHelper is YooAssetResourceHelper yooHelper)
            {
                yooHelper.PackageName = YooAssetPackageName;
                yooHelper.PlayMode = YooAssetPlayMode;
                yooHelper.HostServerUrl = YooAssetHostServerUrl;
                yooHelper.FallbackHostServerUrl = YooAssetFallbackHostServerUrl;
            }

            // 创建 AgentHelper
            for (int i = 0; i < LoadResourceAgentHelperCount; i++)
            {
                var agentHelper = CreateComponentHelper<LoadResourceAgentHelperBase>(
                    LoadResourceAgentHelperTypeName, CustomLoadResourceAgentHelper);

                // 配置 YooAsset AgentHelper
                if (agentHelper is YooAssetLoadResourceAgentHelper yooAgentHelper)
                {
                    yooAgentHelper.PackageName = YooAssetPackageName;
                }

                _resourceManager.AddLoadResourceAgentHelper(agentHelper);
            }
        }

        /// <summary>
        /// 轮询组件
        /// </summary>
        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            _lastUnloadTime += realElapseSeconds;

            if (_unloadOperation != null)
            {
                if (_unloadOperation.isDone)
                {
                    _unloadOperation = null;
                    _lastUnloadTime = 0f;
                }
                return;
            }

            if (_forceUnloadUnusedAssets || _lastUnloadTime >= MaxUnloadInterval)
            {
                UnloadUnusedAssets();
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
        /// 卸载资源
        /// </summary>
        public void UnloadAsset(object asset)
        {
            _resourceManager.UnloadAsset(asset);
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

        /// <summary>
        /// 卸载未使用资源
        /// </summary>
        public void UnloadUnusedAssets()
        {
            _forceUnloadUnusedAssets = false;
            _lastUnloadTime = 0f;
            _unloadOperation = Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 强制卸载未使用资源
        /// </summary>
        public void ForceUnloadUnusedAssets()
        {
            _forceUnloadUnusedAssets = true;
        }

        // ─── 辅助方法 ───

        /// <summary>
        /// 创建组件辅助器
        /// </summary>
        private T CreateComponentHelper<T>(string typeName, T customHelper) where T : MonoBehaviour
        {
            if (customHelper != null)
            {
                return customHelper;
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new GameFrameworkException(
                    $"Helper type name for '{typeof(T).Name}' is not set.");
            }

            Type helperType = Type.GetType(typeName);
            if (helperType == null)
            {
                throw new GameFrameworkException(
                    $"Can not find helper type '{typeName}'.");
            }

            if (Instance == null)
            {
                CreateInstance();
            }

            var helperGo = new GameObject($"[{helperType.Name}]");
            helperGo.transform.SetParent(Instance);
            return helperGo.AddComponent(helperType) as T;
        }
    }
}
