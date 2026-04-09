//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using System;
using System.Collections.Generic;
using GameFramework.DataProvider;
using UnityEngine.Scripting;

namespace GameFramework.Scene
{
    /// <summary>
    /// 鍦烘櫙绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class SceneManager : GameFrameworkModule, ISceneManager
    {
        private readonly List<string> m_LoadedSceneAssetNames;
        private readonly List<string> m_LoadingSceneAssetNames;
        private readonly List<string> m_UnloadingSceneAssetNames;
        private readonly LoadSceneCallbacks m_LoadSceneCallbacks;
        private readonly UnloadSceneCallbacks m_UnloadSceneCallbacks;
        private IResourceManager m_ResourceManager;
        private EventHandler<LoadSceneSuccessEventArgs> m_LoadSceneSuccessEventHandler;
        private EventHandler<LoadSceneFailureEventArgs> m_LoadSceneFailureEventHandler;
        private EventHandler<LoadSceneUpdateEventArgs> m_LoadSceneUpdateEventHandler;
        private EventHandler<LoadSceneDependencyAssetEventArgs> m_LoadSceneDependencyAssetEventHandler;
        private EventHandler<UnloadSceneSuccessEventArgs> m_UnloadSceneSuccessEventHandler;
        private EventHandler<UnloadSceneFailureEventArgs> m_UnloadSceneFailureEventHandler;

        /// <summary>
        /// 鍒濆鍖栧満鏅鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public SceneManager()
        {
            m_LoadedSceneAssetNames = new List<string>();
            m_LoadingSceneAssetNames = new List<string>();
            m_UnloadingSceneAssetNames = new List<string>();
            m_LoadSceneCallbacks = new LoadSceneCallbacks(LoadSceneSuccessCallback, LoadSceneFailureCallback, LoadSceneUpdateCallback, LoadSceneDependencyAssetCallback);
            m_UnloadSceneCallbacks = new UnloadSceneCallbacks(UnloadSceneSuccessCallback, UnloadSceneFailureCallback);
            m_ResourceManager = null;
            m_LoadSceneSuccessEventHandler = null;
            m_LoadSceneFailureEventHandler = null;
            m_LoadSceneUpdateEventHandler = null;
            m_LoadSceneDependencyAssetEventHandler = null;
            m_UnloadSceneSuccessEventHandler = null;
            m_UnloadSceneFailureEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<LoadSceneSuccessEventArgs> LoadSceneSuccess
        {
            add
            {
                m_LoadSceneSuccessEventHandler += value;
            }
            remove
            {
                m_LoadSceneSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<LoadSceneFailureEventArgs> LoadSceneFailure
        {
            add
            {
                m_LoadSceneFailureEventHandler += value;
            }
            remove
            {
                m_LoadSceneFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙鏇存柊浜嬩欢銆?
        /// </summary>
        public event EventHandler<LoadSceneUpdateEventArgs> LoadSceneUpdate
        {
            add
            {
                m_LoadSceneUpdateEventHandler += value;
            }
            remove
            {
                m_LoadSceneUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙鏃跺姞杞戒緷璧栬祫婧愪簨浠躲€?
        /// </summary>
        public event EventHandler<LoadSceneDependencyAssetEventArgs> LoadSceneDependencyAsset
        {
            add
            {
                m_LoadSceneDependencyAssetEventHandler += value;
            }
            remove
            {
                m_LoadSceneDependencyAssetEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍗歌浇鍦烘櫙鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<UnloadSceneSuccessEventArgs> UnloadSceneSuccess
        {
            add
            {
                m_UnloadSceneSuccessEventHandler += value;
            }
            remove
            {
                m_UnloadSceneSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍗歌浇鍦烘櫙澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<UnloadSceneFailureEventArgs> UnloadSceneFailure
        {
            add
            {
                m_UnloadSceneFailureEventHandler += value;
            }
            remove
            {
                m_UnloadSceneFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍦烘櫙绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗗満鏅鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            string[] loadedSceneAssetNames = m_LoadedSceneAssetNames.ToArray();
            foreach (string loadedSceneAssetName in loadedSceneAssetNames)
            {
                if (SceneIsUnloading(loadedSceneAssetName))
                {
                    continue;
                }

                UnloadScene(loadedSceneAssetName);
            }

            m_LoadedSceneAssetNames.Clear();
            m_LoadingSceneAssetNames.Clear();
            m_UnloadingSceneAssetNames.Clear();
        }

        /// <summary>
        /// 璁剧疆璧勬簮绠＄悊鍣ㄣ€?
        /// </summary>
        /// <param name="resourceManager">璧勬簮绠＄悊鍣ㄣ€?/param>
        public void SetResourceManager(IResourceManager resourceManager)
        {
            if (resourceManager == null)
            {
                throw new GameFrameworkException("Resource manager is invalid.");
            }

            m_ResourceManager = resourceManager;
        }

        /// <summary>
        /// 鑾峰彇鍦烘櫙鏄惁宸插姞杞姐€?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <returns>鍦烘櫙鏄惁宸插姞杞姐€?/returns>
        public bool SceneIsLoaded(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            return m_LoadedSceneAssetNames.Contains(sceneAssetName);
        }

        /// <summary>
        /// 鑾峰彇宸插姞杞藉満鏅殑璧勬簮鍚嶇О銆?
        /// </summary>
        /// <returns>宸插姞杞藉満鏅殑璧勬簮鍚嶇О銆?/returns>
        public string[] GetLoadedSceneAssetNames()
        {
            return m_LoadedSceneAssetNames.ToArray();
        }

        /// <summary>
        /// 鑾峰彇宸插姞杞藉満鏅殑璧勬簮鍚嶇О銆?
        /// </summary>
        /// <param name="results">宸插姞杞藉満鏅殑璧勬簮鍚嶇О銆?/param>
        public void GetLoadedSceneAssetNames(List<string> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            results.AddRange(m_LoadedSceneAssetNames);
        }

        /// <summary>
        /// 鑾峰彇鍦烘櫙鏄惁姝ｅ湪鍔犺浇銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <returns>鍦烘櫙鏄惁姝ｅ湪鍔犺浇銆?/returns>
        public bool SceneIsLoading(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            return m_LoadingSceneAssetNames.Contains(sceneAssetName);
        }

        /// <summary>
        /// 鑾峰彇姝ｅ湪鍔犺浇鍦烘櫙鐨勮祫婧愬悕绉般€?
        /// </summary>
        /// <returns>姝ｅ湪鍔犺浇鍦烘櫙鐨勮祫婧愬悕绉般€?/returns>
        public string[] GetLoadingSceneAssetNames()
        {
            return m_LoadingSceneAssetNames.ToArray();
        }

        /// <summary>
        /// 鑾峰彇姝ｅ湪鍔犺浇鍦烘櫙鐨勮祫婧愬悕绉般€?
        /// </summary>
        /// <param name="results">姝ｅ湪鍔犺浇鍦烘櫙鐨勮祫婧愬悕绉般€?/param>
        public void GetLoadingSceneAssetNames(List<string> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            results.AddRange(m_LoadingSceneAssetNames);
        }

        /// <summary>
        /// 鑾峰彇鍦烘櫙鏄惁姝ｅ湪鍗歌浇銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <returns>鍦烘櫙鏄惁姝ｅ湪鍗歌浇銆?/returns>
        public bool SceneIsUnloading(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            return m_UnloadingSceneAssetNames.Contains(sceneAssetName);
        }

        /// <summary>
        /// 鑾峰彇姝ｅ湪鍗歌浇鍦烘櫙鐨勮祫婧愬悕绉般€?
        /// </summary>
        /// <returns>姝ｅ湪鍗歌浇鍦烘櫙鐨勮祫婧愬悕绉般€?/returns>
        public string[] GetUnloadingSceneAssetNames()
        {
            return m_UnloadingSceneAssetNames.ToArray();
        }

        /// <summary>
        /// 鑾峰彇姝ｅ湪鍗歌浇鍦烘櫙鐨勮祫婧愬悕绉般€?
        /// </summary>
        /// <param name="results">姝ｅ湪鍗歌浇鍦烘櫙鐨勮祫婧愬悕绉般€?/param>
        public void GetUnloadingSceneAssetNames(List<string> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            results.AddRange(m_UnloadingSceneAssetNames);
        }

        /// <summary>
        /// 妫€鏌ュ満鏅祫婧愭槸鍚﹀瓨鍦ㄣ€?
        /// </summary>
        /// <param name="sceneAssetName">瑕佹鏌ュ満鏅祫婧愮殑鍚嶇О銆?/param>
        /// <returns>鍦烘櫙璧勬簮鏄惁瀛樺湪銆?/returns>
        public bool HasScene(string sceneAssetName)
        {
            return m_ResourceManager.HasAsset(sceneAssetName) != HasAssetResult.NotExist;
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        public void LoadScene(string sceneAssetName)
        {
            LoadScene(sceneAssetName, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇鍦烘櫙璧勬簮鐨勪紭鍏堢骇銆?/param>
        public void LoadScene(string sceneAssetName, int priority)
        {
            LoadScene(sceneAssetName, priority, null);
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void LoadScene(string sceneAssetName, object userData)
        {
            LoadScene(sceneAssetName, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇鍦烘櫙璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void LoadScene(string sceneAssetName, int priority, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (SceneIsUnloading(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is being unloaded.", sceneAssetName));
            }

            if (SceneIsLoading(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is being loaded.", sceneAssetName));
            }

            if (SceneIsLoaded(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is already loaded.", sceneAssetName));
            }

            m_LoadingSceneAssetNames.Add(sceneAssetName);
            m_ResourceManager.LoadScene(sceneAssetName, priority, m_LoadSceneCallbacks, userData);
        }

        /// <summary>
        /// 鍗歌浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        public void UnloadScene(string sceneAssetName)
        {
            UnloadScene(sceneAssetName, null);
        }

        /// <summary>
        /// 鍗歌浇鍦烘櫙銆?
        /// </summary>
        /// <param name="sceneAssetName">鍦烘櫙璧勬簮鍚嶇О銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void UnloadScene(string sceneAssetName, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (SceneIsUnloading(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is being unloaded.", sceneAssetName));
            }

            if (SceneIsLoading(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is being loaded.", sceneAssetName));
            }

            if (!SceneIsLoaded(sceneAssetName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Scene asset '{0}' is not loaded yet.", sceneAssetName));
            }

            m_UnloadingSceneAssetNames.Add(sceneAssetName);
            m_ResourceManager.UnloadScene(sceneAssetName, m_UnloadSceneCallbacks, userData);
        }

        private void LoadSceneSuccessCallback(string sceneAssetName, float duration, object userData)
        {
            m_LoadingSceneAssetNames.Remove(sceneAssetName);
            m_LoadedSceneAssetNames.Add(sceneAssetName);
            if (m_LoadSceneSuccessEventHandler != null)
            {
                LoadSceneSuccessEventArgs loadSceneSuccessEventArgs = LoadSceneSuccessEventArgs.Create(sceneAssetName, duration, userData);
                m_LoadSceneSuccessEventHandler(this, loadSceneSuccessEventArgs);
                ReferencePool.Release(loadSceneSuccessEventArgs);
            }
        }

        private void LoadSceneFailureCallback(string sceneAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            m_LoadingSceneAssetNames.Remove(sceneAssetName);
            string appendErrorMessage = Utility.Text.Format("Load scene failure, scene asset name '{0}', status '{1}', error message '{2}'.", sceneAssetName, status, errorMessage);
            if (m_LoadSceneFailureEventHandler != null)
            {
                LoadSceneFailureEventArgs loadSceneFailureEventArgs = LoadSceneFailureEventArgs.Create(sceneAssetName, appendErrorMessage, userData);
                m_LoadSceneFailureEventHandler(this, loadSceneFailureEventArgs);
                ReferencePool.Release(loadSceneFailureEventArgs);
                return;
            }

            throw new GameFrameworkException(appendErrorMessage);
        }

        private void LoadSceneUpdateCallback(string sceneAssetName, float progress, object userData)
        {
            if (m_LoadSceneUpdateEventHandler != null)
            {
                LoadSceneUpdateEventArgs loadSceneUpdateEventArgs = LoadSceneUpdateEventArgs.Create(sceneAssetName, progress, userData);
                m_LoadSceneUpdateEventHandler(this, loadSceneUpdateEventArgs);
                ReferencePool.Release(loadSceneUpdateEventArgs);
            }
        }

        private void LoadSceneDependencyAssetCallback(string sceneAssetName, string dependencyAssetName, int loadedCount, int totalCount, object userData)
        {
            if (m_LoadSceneDependencyAssetEventHandler != null)
            {
                LoadSceneDependencyAssetEventArgs loadSceneDependencyAssetEventArgs = LoadSceneDependencyAssetEventArgs.Create(sceneAssetName, dependencyAssetName, loadedCount, totalCount, userData);
                m_LoadSceneDependencyAssetEventHandler(this, loadSceneDependencyAssetEventArgs);
                ReferencePool.Release(loadSceneDependencyAssetEventArgs);
            }
        }

        private void UnloadSceneSuccessCallback(string sceneAssetName, object userData)
        {
            m_UnloadingSceneAssetNames.Remove(sceneAssetName);
            m_LoadedSceneAssetNames.Remove(sceneAssetName);
            if (m_UnloadSceneSuccessEventHandler != null)
            {
                UnloadSceneSuccessEventArgs unloadSceneSuccessEventArgs = UnloadSceneSuccessEventArgs.Create(sceneAssetName, userData);
                m_UnloadSceneSuccessEventHandler(this, unloadSceneSuccessEventArgs);
                ReferencePool.Release(unloadSceneSuccessEventArgs);
            }
        }

        private void UnloadSceneFailureCallback(string sceneAssetName, object userData)
        {
            m_UnloadingSceneAssetNames.Remove(sceneAssetName);
            if (m_UnloadSceneFailureEventHandler != null)
            {
                UnloadSceneFailureEventArgs unloadSceneFailureEventArgs = UnloadSceneFailureEventArgs.Create(sceneAssetName, userData);
                m_UnloadSceneFailureEventHandler(this, unloadSceneFailureEventArgs);
                ReferencePool.Release(unloadSceneFailureEventArgs);
                return;
            }

            throw new GameFrameworkException(Utility.Text.Format("Unload scene failure, scene asset name '{0}'.", sceneAssetName));
        }
    }
}
