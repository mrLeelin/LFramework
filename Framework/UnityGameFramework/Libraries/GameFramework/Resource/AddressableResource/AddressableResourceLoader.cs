using System;
using System.Collections;
using System.Collections.Generic;


namespace GameFramework.Resource
{
    public sealed class AddressableResourceLoader
    {
        private readonly TaskPool<AddressableLoadResourceTask> m_TaskPool;
        private readonly LoadBytesCallbacks m_LoadBytesCallbacks;
        private readonly AddressableResourceManager m_ResourceManager;
        internal readonly Dictionary<string, object> m_SceneToAssetMap;


        /// <summary>
        /// 获取加载资源代理总数量。
        /// </summary>
        public int TotalAgentCount
        {
            get { return m_TaskPool.TotalAgentCount; }
        }

        /// <summary>
        /// 获取可用加载资源代理数量。
        /// </summary>
        public int FreeAgentCount
        {
            get { return m_TaskPool.FreeAgentCount; }
        }

        /// <summary>
        /// 获取工作中加载资源代理数量。
        /// </summary>
        public int WorkingAgentCount
        {
            get { return m_TaskPool.WorkingAgentCount; }
        }

        /// <summary>
        /// 获取等待加载资源任务数量。
        /// </summary>
        public int WaitingTaskCount
        {
            get { return m_TaskPool.WaitingTaskCount; }
        }


        public AddressableResourceLoader(AddressableResourceManager addressableResourceManager)
        {
            m_ResourceManager = addressableResourceManager;
            m_TaskPool = new TaskPool<AddressableLoadResourceTask>();
            m_SceneToAssetMap = new Dictionary<string, object>();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TaskPool.Update(elapseSeconds, realElapseSeconds);
        }

        public void ShutDown()
        {
            m_TaskPool.Shutdown();
            m_SceneToAssetMap.Clear();
        }

        /// <summary>
        /// 增加加载资源代理辅助器。
        /// </summary>
        /// <param name="loadResourceAgentHelper">要增加的加载资源代理辅助器。</param>
        /// <param name="resourceHelper">资源辅助器。</param>
        /// <param name="readOnlyPath">资源只读区路径。</param>
        /// <param name="readWritePath">资源读写区路径。</param>
        /// <param name="decryptResourceCallback">要设置的解密资源回调函数。</param>
        public void AddLoadResourceAgentHelper(ILoadResourceAgentHelper loadResourceAgentHelper,
            IResourceHelper resourceHelper)
        {
            AddressableLoadResourceAgent agent =
                new AddressableLoadResourceAgent(loadResourceAgentHelper, resourceHelper, this);
            m_TaskPool.AddAgent(agent);
        }

        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData)
        {
            AddressableLoadAssetTask mainTask = AddressableLoadAssetTask.Create(assetName, priority, assetType,
                loadAssetCallbacks, false, userData);
            m_TaskPool.AddTask(mainTask);
        }

        public void InstantiateAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData)
        {
            AddressableLoadAssetTask mainTask =
                AddressableLoadAssetTask.Create(assetName, priority, null, loadAssetCallbacks, true, userData);
            m_TaskPool.AddTask(mainTask);
        }

        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks,
            object userData)
        {
            AddressableLoadSceneTask mainTask =
                AddressableLoadSceneTask.Create(sceneAssetName, priority, loadSceneCallbacks, userData);
            m_TaskPool.AddTask(mainTask);
        }

        public void UnloadAsset(object asset)
        {
            m_ResourceManager.m_ResourceHelper.Release(asset);
        }


        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
            if (m_ResourceManager.m_ResourceHelper == null)
            {
                throw new GameFrameworkException("You must set resource helper first.");
            }

            object asset = null;
            if (m_SceneToAssetMap.TryGetValue(sceneAssetName, out asset))
            {
                m_SceneToAssetMap.Remove(sceneAssetName);
            }
            else
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find asset of scene '{0}'.",
                    sceneAssetName));
            }

            m_ResourceManager.m_ResourceHelper.UnloadScene(sceneAssetName, unloadSceneCallbacks, asset);
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object o)
        {
            throw new NotImplementedException();
        }

        public HasAssetResult HasAsset(string assetName)
        {
            return HasAssetResult.Addressable;
        }
    }
}