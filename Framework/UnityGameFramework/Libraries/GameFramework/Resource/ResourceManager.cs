using System;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源管理器（唯一实现）
    /// </summary>
    internal sealed class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private string _readOnlyPath;
        private string _readWritePath;
        private ResourceMode _resourceMode;
        private IResourceHelper _resourceHelper;

        private readonly List<ILoadResourceAgentHelper> _agentHelpers;
        private readonly LinkedList<LoadResourceTaskBase> _pendingTasks;

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public ResourceManager()
        {
            _readOnlyPath = null;
            _readWritePath = null;
            _resourceMode = ResourceMode.Unspecified;
            _resourceHelper = null;
            _agentHelpers = new List<ILoadResourceAgentHelper>();
            _pendingTasks = new LinkedList<LoadResourceTaskBase>();
        }

        /// <summary>
        /// 获取资源只读路径
        /// </summary>
        public string ReadOnlyPath => _readOnlyPath;

        /// <summary>
        /// 获取资源读写路径
        /// </summary>
        public string ReadWritePath => _readWritePath;

        /// <summary>
        /// 获取资源模式
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// 获取模块优先级
        /// </summary>
        internal override int Priority => 4;

        /// <summary>
        /// 设置资源只读路径
        /// </summary>
        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
                throw new GameFrameworkException("Read-only path is invalid.");
            _readOnlyPath = readOnlyPath;
        }

        /// <summary>
        /// 设置资源读写路径
        /// </summary>
        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
                throw new GameFrameworkException("Read-write path is invalid.");
            _readWritePath = readWritePath;
        }

        /// <summary>
        /// 设置资源模式
        /// </summary>
        public void SetResourceMode(ResourceMode resourceMode)
        {
            if (resourceMode == ResourceMode.Unspecified)
                throw new GameFrameworkException("Resource mode is invalid.");
            _resourceMode = resourceMode;
        }

        /// <summary>
        /// 设置资源辅助器
        /// </summary>
        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            _resourceHelper = resourceHelper ?? throw new GameFrameworkException("Resource helper is invalid.");
        }

        /// <summary>
        /// 添加加载资源代理辅助器
        /// </summary>
        public void AddLoadResourceAgentHelper(ILoadResourceAgentHelper agentHelper)
        {
            if (agentHelper == null)
                throw new GameFrameworkException("Load resource agent helper is invalid.");
            _agentHelpers.Add(agentHelper);
        }

        /// <summary>
        /// 初始化资源
        /// </summary>
        public void InitResources(InitResourcesCompleteCallback callback)
        {
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            var initCallBack = new ResourceInitCallBack(
                () => callback?.Invoke(),
                (errorMessage) => GameFrameworkLog.Error(errorMessage)
            );
            _resourceHelper.InitializeResources(initCallBack);
        }

        /// <summary>
        /// 查询资源是否存在
        /// </summary>
        public HasAssetResult HasAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");
            return _resourceHelper.HasAsset(assetName);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public void LoadAsset(string assetName, Type assetType, int priority,
                              LoadAssetCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load asset callbacks is invalid.");

            var agent = GetAvailableAgent();
            if (agent != null)
            {
                agent.LoadAsset(assetName, assetType, callbacks, userData);
            }
            else
            {
                _pendingTasks.AddLast(new LoadAssetTask(assetName, assetType, priority, callbacks, userData));
            }
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public void UnloadAsset(object asset)
        {
            if (asset == null)
                throw new GameFrameworkException("Asset is invalid.");
            _resourceHelper.Release(asset);
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public void LoadScene(string sceneAssetName, int priority,
                              LoadSceneCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new GameFrameworkException("Scene asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load scene callbacks is invalid.");

            var agent = GetAvailableAgent();
            if (agent != null)
            {
                agent.LoadScene(sceneAssetName, callbacks, userData);
            }
            else
            {
                _pendingTasks.AddLast(new LoadSceneTask(sceneAssetName, priority, callbacks, userData));
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public void UnloadScene(string sceneAssetName,
                                UnloadSceneCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new GameFrameworkException("Scene asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            _resourceHelper.UnloadScene(sceneAssetName, callbacks, userData);
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public void LoadBinary(string binaryAssetName,
                               LoadBinaryCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
                throw new GameFrameworkException("Binary asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load binary callbacks is invalid.");

            var agent = GetAvailableAgent();
            if (agent != null)
            {
                agent.LoadBinary(binaryAssetName, callbacks, userData);
            }
            else
            {
                _pendingTasks.AddLast(new LoadBinaryTask(binaryAssetName, 0, callbacks, userData));
            }
        }

        /// <summary>
        /// 游戏框架模块轮询
        /// </summary>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            var node = _pendingTasks.First;
            while (node != null)
            {
                var agent = GetAvailableAgent();
                if (agent == null) break;

                node.Value.Execute(agent);
                var next = node.Next;
                _pendingTasks.Remove(node);
                node = next;
            }
        }

        /// <summary>
        /// 关闭并清理资源管理器
        /// </summary>
        internal override void Shutdown()
        {
            _pendingTasks.Clear();
            _agentHelpers.Clear();
            _resourceHelper = null;
        }

        private ILoadResourceAgentHelper GetAvailableAgent()
        {
            for (int i = 0; i < _agentHelpers.Count; i++)
            {
                if (!_agentHelpers[i].IsBusy) return _agentHelpers[i];
            }
            return null;
        }
    }

    /// <summary>
    /// 加载资源任务基类
    /// </summary>
    internal abstract class LoadResourceTaskBase
    {
        public int Priority { get; }

        protected LoadResourceTaskBase(int priority)
        {
            Priority = priority;
        }

        public abstract void Execute(ILoadResourceAgentHelper agent);
    }

    /// <summary>
    /// 加载资源任务
    /// </summary>
    internal sealed class LoadAssetTask : LoadResourceTaskBase
    {
        private readonly string _assetName;
        private readonly Type _assetType;
        private readonly LoadAssetCallbacks _callbacks;
        private readonly object _userData;

        public LoadAssetTask(string assetName, Type assetType, int priority,
                             LoadAssetCallbacks callbacks, object userData) : base(priority)
        {
            _assetName = assetName;
            _assetType = assetType;
            _callbacks = callbacks;
            _userData = userData;
        }

        public override void Execute(ILoadResourceAgentHelper agent)
        {
            agent.LoadAsset(_assetName, _assetType, _callbacks, _userData);
        }
    }

    /// <summary>
    /// 加载场景任务
    /// </summary>
    internal sealed class LoadSceneTask : LoadResourceTaskBase
    {
        private readonly string _sceneAssetName;
        private readonly LoadSceneCallbacks _callbacks;
        private readonly object _userData;

        public LoadSceneTask(string sceneAssetName, int priority,
                             LoadSceneCallbacks callbacks, object userData) : base(priority)
        {
            _sceneAssetName = sceneAssetName;
            _callbacks = callbacks;
            _userData = userData;
        }

        public override void Execute(ILoadResourceAgentHelper agent)
        {
            agent.LoadScene(_sceneAssetName, _callbacks, _userData);
        }
    }

    /// <summary>
    /// 加载二进制任务
    /// </summary>
    internal sealed class LoadBinaryTask : LoadResourceTaskBase
    {
        private readonly string _binaryAssetName;
        private readonly LoadBinaryCallbacks _callbacks;
        private readonly object _userData;

        public LoadBinaryTask(string binaryAssetName, int priority,
                              LoadBinaryCallbacks callbacks, object userData) : base(priority)
        {
            _binaryAssetName = binaryAssetName;
            _callbacks = callbacks;
            _userData = userData;
        }

        public override void Execute(ILoadResourceAgentHelper agent)
        {
            agent.LoadBinary(_binaryAssetName, _callbacks, _userData);
        }
    }
}
