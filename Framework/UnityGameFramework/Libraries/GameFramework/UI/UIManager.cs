//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using GameFramework.DataProvider;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameFramework.UI
{
    /// <summary>
    /// 鐣岄潰绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        private readonly Dictionary<string, UIGroup> m_UIGroups;
        private readonly Dictionary<int, string> m_UIFormsBeingLoaded;
        private readonly HashSet<int> m_UIFormsToReleaseOnLoad;
        private readonly Queue<IUIForm> m_RecycleQueue;
        private readonly LoadAssetCallbacks m_LoadAssetCallbacks;
        private IObjectPoolManager m_ObjectPoolManager;
        private IResourceManager m_ResourceManager;
        private IObjectPool<UIFormInstanceObject> m_InstancePool;
        private IUIFormHelper m_UIFormHelper;
        private int m_Serial;
        private bool m_IsShutdown;
        private EventHandler<OpenUIFormSuccessEventArgs> m_OpenUIFormSuccessEventHandler;
        private EventHandler<OpenUIFormFailureEventArgs> m_OpenUIFormFailureEventHandler;
        private EventHandler<OpenUIFormUpdateEventArgs> m_OpenUIFormUpdateEventHandler;
        private EventHandler<OpenUIFormDependencyAssetEventArgs> m_OpenUIFormDependencyAssetEventHandler;
        private EventHandler<CloseUIFormCompleteEventArgs> m_CloseUIFormCompleteEventHandler;

        /// <summary>
        /// 鍒濆鍖栫晫闈㈢鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public UIManager()
        {
            m_UIGroups = new Dictionary<string, UIGroup>(StringComparer.Ordinal);
            m_UIFormsBeingLoaded = new Dictionary<int, string>();
            m_UIFormsToReleaseOnLoad = new HashSet<int>();
            m_RecycleQueue = new Queue<IUIForm>();
            m_LoadAssetCallbacks = new LoadAssetCallbacks(LoadAssetSuccessCallback, LoadAssetFailureCallback, LoadAssetUpdateCallback, LoadAssetDependencyAssetCallback);
            m_ObjectPoolManager = null;
            m_ResourceManager = null;
            m_InstancePool = null;
            m_UIFormHelper = null;
            m_Serial = 0;
            m_IsShutdown = false;
            m_OpenUIFormSuccessEventHandler = null;
            m_OpenUIFormFailureEventHandler = null;
            m_OpenUIFormUpdateEventHandler = null;
            m_OpenUIFormDependencyAssetEventHandler = null;
            m_CloseUIFormCompleteEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰缁勬暟閲忋€?
        /// </summary>
        public int UIGroupCount
        {
            get
            {
                return m_UIGroups.Count;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃晫闈㈠疄渚嬪璞℃睜鑷姩閲婃斁鍙噴鏀惧璞＄殑闂撮殧绉掓暟銆?
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get
            {
                return m_InstancePool.AutoReleaseInterval;
            }
            set
            {
                m_InstancePool.AutoReleaseInterval = value;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃晫闈㈠疄渚嬪璞℃睜鐨勫閲忋€?
        /// </summary>
        public int InstanceCapacity
        {
            get
            {
                return m_InstancePool.Capacity;
            }
            set
            {
                m_InstancePool.Capacity = value;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃晫闈㈠疄渚嬪璞℃睜瀵硅薄杩囨湡绉掓暟銆?
        /// </summary>
        public float InstanceExpireTime
        {
            get
            {
                return m_InstancePool.ExpireTime;
            }
            set
            {
                m_InstancePool.ExpireTime = value;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃晫闈㈠疄渚嬪璞℃睜鐨勪紭鍏堢骇銆?
        /// </summary>
        public int InstancePriority
        {
            get
            {
                return m_InstancePool.Priority;
            }
            set
            {
                m_InstancePool.Priority = value;
            }
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<OpenUIFormSuccessEventArgs> OpenUIFormSuccess
        {
            add
            {
                m_OpenUIFormSuccessEventHandler += value;
            }
            remove
            {
                m_OpenUIFormSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<OpenUIFormFailureEventArgs> OpenUIFormFailure
        {
            add
            {
                m_OpenUIFormFailureEventHandler += value;
            }
            remove
            {
                m_OpenUIFormFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰鏇存柊浜嬩欢銆?
        /// </summary>
        public event EventHandler<OpenUIFormUpdateEventArgs> OpenUIFormUpdate
        {
            add
            {
                m_OpenUIFormUpdateEventHandler += value;
            }
            remove
            {
                m_OpenUIFormUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰鏃跺姞杞戒緷璧栬祫婧愪簨浠躲€?
        /// </summary>
        public event EventHandler<OpenUIFormDependencyAssetEventArgs> OpenUIFormDependencyAsset
        {
            add
            {
                m_OpenUIFormDependencyAssetEventHandler += value;
            }
            remove
            {
                m_OpenUIFormDependencyAssetEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍏抽棴鐣岄潰瀹屾垚浜嬩欢銆?
        /// </summary>
        public event EventHandler<CloseUIFormCompleteEventArgs> CloseUIFormComplete
        {
            add
            {
                m_CloseUIFormCompleteEventHandler += value;
            }
            remove
            {
                m_CloseUIFormCompleteEventHandler -= value;
            }
        }

        /// <summary>
        /// 鐣岄潰绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            while (m_RecycleQueue.Count > 0)
            {
                IUIForm uiForm = m_RecycleQueue.Dequeue();
                uiForm.OnRecycle();
                m_InstancePool.Unspawn(uiForm.Handle);
            }

            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                uiGroup.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗙晫闈㈢鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            m_IsShutdown = true;
            CloseAllLoadedUIForms();
            m_UIGroups.Clear();
            m_UIFormsBeingLoaded.Clear();
            m_UIFormsToReleaseOnLoad.Clear();
            m_RecycleQueue.Clear();
        }

        /// <summary>
        /// 璁剧疆瀵硅薄姹犵鐞嗗櫒銆?
        /// </summary>
        /// <param name="objectPoolManager">瀵硅薄姹犵鐞嗗櫒銆?/param>
        public void SetObjectPoolManager(IObjectPoolManager objectPoolManager)
        {
            if (objectPoolManager == null)
            {
                throw new GameFrameworkException("Object pool manager is invalid.");
            }

            m_ObjectPoolManager = objectPoolManager;
            m_InstancePool = m_ObjectPoolManager.CreateSingleSpawnObjectPool<UIFormInstanceObject>("UI Instance Pool");
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
        /// 璁剧疆鐣岄潰杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="uiFormHelper">鐣岄潰杈呭姪鍣ㄣ€?/param>
        public void SetUIFormHelper(IUIFormHelper uiFormHelper)
        {
            if (uiFormHelper == null)
            {
                throw new GameFrameworkException("UI form helper is invalid.");
            }

            m_UIFormHelper = uiFormHelper;
        }

        /// <summary>
        /// 鏄惁瀛樺湪鐣岄潰缁勩€?
        /// </summary>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <returns>鏄惁瀛樺湪鐣岄潰缁勩€?/returns>
        public bool HasUIGroup(string uiGroupName)
        {
            if (string.IsNullOrEmpty(uiGroupName))
            {
                throw new GameFrameworkException("UI group name is invalid.");
            }

            return m_UIGroups.ContainsKey(uiGroupName);
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰缁勩€?
        /// </summary>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑鐣岄潰缁勩€?/returns>
        public IUIGroup GetUIGroup(string uiGroupName)
        {
            if (string.IsNullOrEmpty(uiGroupName))
            {
                throw new GameFrameworkException("UI group name is invalid.");
            }

            UIGroup uiGroup = null;
            if (m_UIGroups.TryGetValue(uiGroupName, out uiGroup))
            {
                return uiGroup;
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夌晫闈㈢粍銆?
        /// </summary>
        /// <returns>鎵€鏈夌晫闈㈢粍銆?/returns>
        public IUIGroup[] GetAllUIGroups()
        {
            int index = 0;
            IUIGroup[] results = new IUIGroup[m_UIGroups.Count];
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results[index++] = uiGroup.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夌晫闈㈢粍銆?
        /// </summary>
        /// <param name="results">鎵€鏈夌晫闈㈢粍銆?/param>
        public void GetAllUIGroups(List<IUIGroup> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results.Add(uiGroup.Value);
            }
        }

        /// <summary>
        /// 澧炲姞鐣岄潰缁勩€?
        /// </summary>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="uiGroupHelper">鐣岄潰缁勮緟鍔╁櫒銆?/param>
        /// <returns>鏄惁澧炲姞鐣岄潰缁勬垚鍔熴€?/returns>
        public bool AddUIGroup(string uiGroupName, IUIGroupHelper uiGroupHelper)
        {
            return AddUIGroup(uiGroupName, 0, uiGroupHelper);
        }

        /// <summary>
        /// 澧炲姞鐣岄潰缁勩€?
        /// </summary>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="uiGroupDepth">鐣岄潰缁勬繁搴︺€?/param>
        /// <param name="uiGroupHelper">鐣岄潰缁勮緟鍔╁櫒銆?/param>
        /// <returns>鏄惁澧炲姞鐣岄潰缁勬垚鍔熴€?/returns>
        public bool AddUIGroup(string uiGroupName, int uiGroupDepth, IUIGroupHelper uiGroupHelper)
        {
            if (string.IsNullOrEmpty(uiGroupName))
            {
                throw new GameFrameworkException("UI group name is invalid.");
            }

            if (uiGroupHelper == null)
            {
                throw new GameFrameworkException("UI group helper is invalid.");
            }

            if (HasUIGroup(uiGroupName))
            {
                return false;
            }

            m_UIGroups.Add(uiGroupName, new UIGroup(uiGroupName, uiGroupDepth, uiGroupHelper));

            return true;
        }

        /// <summary>
        /// 鏄惁瀛樺湪鐣岄潰銆?
        /// </summary>
        /// <param name="serialId">鐣岄潰搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁瀛樺湪鐣岄潰銆?/returns>
        public bool HasUIForm(int serialId)
        {
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                if (uiGroup.Value.HasUIForm(serialId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 鏄惁瀛樺湪鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <returns>鏄惁瀛樺湪鐣岄潰銆?/returns>
        public bool HasUIForm(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                if (uiGroup.Value.HasUIForm(uiFormAssetName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰銆?
        /// </summary>
        /// <param name="serialId">鐣岄潰搴忓垪缂栧彿銆?/param>
        /// <returns>瑕佽幏鍙栫殑鐣岄潰銆?/returns>
        public IUIForm GetUIForm(int serialId)
        {
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                IUIForm uiForm = uiGroup.Value.GetUIForm(serialId);
                if (uiForm != null)
                {
                    return uiForm;
                }
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <returns>瑕佽幏鍙栫殑鐣岄潰銆?/returns>
        public IUIForm GetUIForm(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                IUIForm uiForm = uiGroup.Value.GetUIForm(uiFormAssetName);
                if (uiForm != null)
                {
                    return uiForm;
                }
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <returns>瑕佽幏鍙栫殑鐣岄潰銆?/returns>
        public IUIForm[] GetUIForms(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            List<IUIForm> results = new List<IUIForm>();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results.AddRange(uiGroup.Value.GetUIForms(uiFormAssetName));
            }

            return results.ToArray();
        }

        /// <summary>
        /// 鑾峰彇鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="results">瑕佽幏鍙栫殑鐣岄潰銆?/param>
        public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                uiGroup.Value.InternalGetUIForms(uiFormAssetName, results);
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊凡鍔犺浇鐨勭晫闈€?
        /// </summary>
        /// <returns>鎵€鏈夊凡鍔犺浇鐨勭晫闈€?/returns>
        public IUIForm[] GetAllLoadedUIForms()
        {
            List<IUIForm> results = new List<IUIForm>();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results.AddRange(uiGroup.Value.GetAllUIForms());
            }

            return results.ToArray();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊凡鍔犺浇鐨勭晫闈€?
        /// </summary>
        /// <param name="results">鎵€鏈夊凡鍔犺浇鐨勭晫闈€?/param>
        public void GetAllLoadedUIForms(List<IUIForm> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                uiGroup.Value.InternalGetAllUIForms(results);
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋鍦ㄥ姞杞界晫闈㈢殑搴忓垪缂栧彿銆?
        /// </summary>
        /// <returns>鎵€鏈夋鍦ㄥ姞杞界晫闈㈢殑搴忓垪缂栧彿銆?/returns>
        public int[] GetAllLoadingUIFormSerialIds()
        {
            int index = 0;
            int[] results = new int[m_UIFormsBeingLoaded.Count];
            foreach (KeyValuePair<int, string> uiFormBeingLoaded in m_UIFormsBeingLoaded)
            {
                results[index++] = uiFormBeingLoaded.Key;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋鍦ㄥ姞杞界晫闈㈢殑搴忓垪缂栧彿銆?
        /// </summary>
        /// <param name="results">鎵€鏈夋鍦ㄥ姞杞界晫闈㈢殑搴忓垪缂栧彿銆?/param>
        public void GetAllLoadingUIFormSerialIds(List<int> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<int, string> uiFormBeingLoaded in m_UIFormsBeingLoaded)
            {
                results.Add(uiFormBeingLoaded.Key);
            }
        }

        /// <summary>
        /// 鏄惁姝ｅ湪鍔犺浇鐣岄潰銆?
        /// </summary>
        /// <param name="serialId">鐣岄潰搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁姝ｅ湪鍔犺浇鐣岄潰銆?/returns>
        public bool IsLoadingUIForm(int serialId)
        {
            return m_UIFormsBeingLoaded.ContainsKey(serialId);
        }

        /// <summary>
        /// 鏄惁姝ｅ湪鍔犺浇鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <returns>鏄惁姝ｅ湪鍔犺浇鐣岄潰銆?/returns>
        public bool IsLoadingUIForm(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            return m_UIFormsBeingLoaded.ContainsValue(uiFormAssetName);
        }

        /// <summary>
        /// 鏄惁鏄悎娉曠殑鐣岄潰銆?
        /// </summary>
        /// <param name="uiForm">鐣岄潰銆?/param>
        /// <returns>鐣岄潰鏄惁鍚堟硶銆?/returns>
        public bool IsValidUIForm(IUIForm uiForm)
        {
            if (uiForm == null)
            {
                return false;
            }

            return HasUIForm(uiForm.SerialId);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, Constant.DefaultPriority, false, null);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇鐣岄潰璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, false, null);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="pauseCoveredUIForm">鏄惁鏆傚仠琚鐩栫殑鐣岄潰銆?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, Constant.DefaultPriority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, Constant.DefaultPriority, false, userData);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇鐣岄潰璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="pauseCoveredUIForm">鏄惁鏆傚仠琚鐩栫殑鐣岄潰銆?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇鐣岄潰璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, false, userData);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="pauseCoveredUIForm">鏄惁鏆傚仠琚鐩栫殑鐣岄潰銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, Constant.DefaultPriority, pauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 鎵撳紑鐣岄潰銆?
        /// </summary>
        /// <param name="uiFormAssetName">鐣岄潰璧勬簮鍚嶇О銆?/param>
        /// <param name="uiGroupName">鐣岄潰缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇鐣岄潰璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="pauseCoveredUIForm">鏄惁鏆傚仠琚鐩栫殑鐣岄潰銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鐣岄潰鐨勫簭鍒楃紪鍙枫€?/returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
        {
            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (m_UIFormHelper == null)
            {
                throw new GameFrameworkException("You must set UI form helper first.");
            }

            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            if (string.IsNullOrEmpty(uiGroupName))
            {
                throw new GameFrameworkException("UI group name is invalid.");
            }

            UIGroup uiGroup = (UIGroup)GetUIGroup(uiGroupName);
            if (uiGroup == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("UI group '{0}' is not exist.", uiGroupName));
            }

            int serialId = ++m_Serial;
            UIFormInstanceObject uiFormInstanceObject = m_InstancePool.Spawn(uiFormAssetName);
            if (uiFormInstanceObject == null)
            {
                m_UIFormsBeingLoaded.Add(serialId, uiFormAssetName);
                m_ResourceManager.InstantiateAsset(uiFormAssetName, m_LoadAssetCallbacks, OpenUIFormInfo.Create(serialId, uiGroup, pauseCoveredUIForm, userData));
            }
            else
            {
                InternalOpenUIForm(serialId, uiFormAssetName, uiGroup, uiFormInstanceObject.Target, pauseCoveredUIForm, false, 0f, userData);
            }

            return serialId;
        }

        /// <summary>
        /// 鍏抽棴鐣岄潰銆?
        /// </summary>
        /// <param name="serialId">瑕佸叧闂晫闈㈢殑搴忓垪缂栧彿銆?/param>
        public void CloseUIForm(int serialId)
        {
            CloseUIForm(serialId, null);
        }

        /// <summary>
        /// 鍏抽棴鐣岄潰銆?
        /// </summary>
        /// <param name="serialId">瑕佸叧闂晫闈㈢殑搴忓垪缂栧彿銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void CloseUIForm(int serialId, object userData)
        {
            if (IsLoadingUIForm(serialId))
            {
                m_UIFormsToReleaseOnLoad.Add(serialId);
                m_UIFormsBeingLoaded.Remove(serialId);
                return;
            }

            IUIForm uiForm = GetUIForm(serialId);
            if (uiForm == null)
            {
                Debug.LogError(Utility.Text.Format("Can not find UI form '{0}'.", serialId));
                //throw new GameFrameworkException(Utility.Text.Format("Can not find UI form '{0}'.", serialId));
                return;
            }

            CloseUIForm(uiForm, userData);
        }

        /// <summary>
        /// 鍏抽棴鐣岄潰銆?
        /// </summary>
        /// <param name="uiForm">瑕佸叧闂殑鐣岄潰銆?/param>
        public void CloseUIForm(IUIForm uiForm)
        {
            CloseUIForm(uiForm, null);
        }

        /// <summary>
        /// 鍏抽棴鐣岄潰銆?
        /// </summary>
        /// <param name="uiForm">瑕佸叧闂殑鐣岄潰銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void CloseUIForm(IUIForm uiForm, object userData)
        {
            if (uiForm == null)
            {
                throw new GameFrameworkException("UI form is invalid.");
            }

            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;
            if (uiGroup == null)
            {
                throw new GameFrameworkException("UI group is invalid.");
            }

            uiGroup.RemoveUIForm(uiForm);
            uiForm.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUIFormCompleteEventHandler != null)
            {
                CloseUIFormCompleteEventArgs closeUIFormCompleteEventArgs = CloseUIFormCompleteEventArgs.Create(uiForm.SerialId, uiForm.UIFormAssetName, uiGroup, userData);
                m_CloseUIFormCompleteEventHandler(this, closeUIFormCompleteEventArgs);
                ReferencePool.Release(closeUIFormCompleteEventArgs);
            }

            m_RecycleQueue.Enqueue(uiForm);
        }

        /// <summary>
        /// 鍏抽棴鎵€鏈夊凡鍔犺浇鐨勭晫闈€?
        /// </summary>
        public void CloseAllLoadedUIForms()
        {
            CloseAllLoadedUIForms(null);
        }

        /// <summary>
        /// 鍏抽棴鎵€鏈夊凡鍔犺浇鐨勭晫闈€?
        /// </summary>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void CloseAllLoadedUIForms(object userData)
        {
            IUIForm[] uiForms = GetAllLoadedUIForms();
            foreach (IUIForm uiForm in uiForms)
            {
                if (!HasUIForm(uiForm.SerialId))
                {
                    continue;
                }

                CloseUIForm(uiForm, userData);
            }
        }

        /// <summary>
        /// 鍏抽棴鎵€鏈夋鍦ㄥ姞杞界殑鐣岄潰銆?
        /// </summary>
        public void CloseAllLoadingUIForms()
        {
            foreach (KeyValuePair<int, string> uiFormBeingLoaded in m_UIFormsBeingLoaded)
            {
                m_UIFormsToReleaseOnLoad.Add(uiFormBeingLoaded.Key);
            }

            m_UIFormsBeingLoaded.Clear();
        }

        /// <summary>
        /// 婵€娲荤晫闈€?
        /// </summary>
        /// <param name="uiForm">瑕佹縺娲荤殑鐣岄潰銆?/param>
        public void RefocusUIForm(IUIForm uiForm)
        {
            RefocusUIForm(uiForm, null);
        }

        /// <summary>
        /// 婵€娲荤晫闈€?
        /// </summary>
        /// <param name="uiForm">瑕佹縺娲荤殑鐣岄潰銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void RefocusUIForm(IUIForm uiForm, object userData)
        {
            if (uiForm == null)
            {
                throw new GameFrameworkException("UI form is invalid.");
            }

            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;
            if (uiGroup == null)
            {
                throw new GameFrameworkException("UI group is invalid.");
            }

            uiGroup.RefocusUIForm(uiForm, userData);
            uiGroup.Refresh();
            uiForm.OnRefocus(userData);
        }

        /// <summary>
        /// 璁剧疆鐣岄潰瀹炰緥鏄惁琚姞閿併€?
        /// </summary>
        /// <param name="uiFormInstance">瑕佽缃槸鍚﹁鍔犻攣鐨勭晫闈㈠疄渚嬨€?/param>
        /// <param name="locked">鐣岄潰瀹炰緥鏄惁琚姞閿併€?/param>
        public void SetUIFormInstanceLocked(object uiFormInstance, bool locked)
        {
            if (uiFormInstance == null)
            {
                throw new GameFrameworkException("UI form instance is invalid.");
            }

            m_InstancePool.SetLocked(uiFormInstance, locked);
        }

        /// <summary>
        /// 璁剧疆鐣岄潰瀹炰緥鐨勪紭鍏堢骇銆?
        /// </summary>
        /// <param name="uiFormInstance">瑕佽缃紭鍏堢骇鐨勭晫闈㈠疄渚嬨€?/param>
        /// <param name="priority">鐣岄潰瀹炰緥浼樺厛绾с€?/param>
        public void SetUIFormInstancePriority(object uiFormInstance, int priority)
        {
            if (uiFormInstance == null)
            {
                throw new GameFrameworkException("UI form instance is invalid.");
            }

            m_InstancePool.SetPriority(uiFormInstance, priority);
        }

        private void InternalOpenUIForm(int serialId, string uiFormAssetName, UIGroup uiGroup, object uiFormInstance, bool pauseCoveredUIForm, bool isNewInstance, float duration, object userData)
        {
            try
            {
                IUIForm uiForm = m_UIFormHelper.CreateUIForm(uiFormInstance, uiGroup,isNewInstance, userData);
                if (uiForm == null)
                {
                    throw new GameFrameworkException("Can not create UI form in UI form helper.");
                }

                uiForm.OnInit(serialId, uiFormAssetName, uiGroup, pauseCoveredUIForm, isNewInstance, userData);
                uiGroup.AddUIForm(uiForm);
                uiForm.OnOpen(userData);
                uiGroup.Refresh();

                if (m_OpenUIFormSuccessEventHandler != null)
                {
                    OpenUIFormSuccessEventArgs openUIFormSuccessEventArgs = OpenUIFormSuccessEventArgs.Create(uiForm, duration, userData);
                    m_OpenUIFormSuccessEventHandler(this, openUIFormSuccessEventArgs);
                    ReferencePool.Release(openUIFormSuccessEventArgs);
                }
            }
            catch (Exception exception)
            {
                if (m_OpenUIFormFailureEventHandler != null)
                {
                    OpenUIFormFailureEventArgs openUIFormFailureEventArgs = OpenUIFormFailureEventArgs.Create(serialId, uiFormAssetName, uiGroup.Name, pauseCoveredUIForm, exception.ToString(), userData);
                    m_OpenUIFormFailureEventHandler(this, openUIFormFailureEventArgs);
                    ReferencePool.Release(openUIFormFailureEventArgs);
                    return;
                }

                throw;
            }
        }

        private void LoadAssetSuccessCallback(string uiFormAssetName, object uiFormAsset, float duration, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            if (m_UIFormsToReleaseOnLoad.Contains(openUIFormInfo.SerialId))
            {
                m_UIFormsToReleaseOnLoad.Remove(openUIFormInfo.SerialId);
                ReferencePool.Release(openUIFormInfo);
                m_UIFormHelper.ReleaseUIForm(uiFormAsset, null);
                return;
            }

            m_UIFormsBeingLoaded.Remove(openUIFormInfo.SerialId);
            UIFormInstanceObject uiFormInstanceObject = UIFormInstanceObject.Create(uiFormAssetName, uiFormAsset, uiFormAsset, m_UIFormHelper);
            m_InstancePool.Register(uiFormInstanceObject, true);

            InternalOpenUIForm(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.UIGroup, uiFormInstanceObject.Target, openUIFormInfo.PauseCoveredUIForm, true, duration, openUIFormInfo.UserData);
            ReferencePool.Release(openUIFormInfo);
        }

        private void LoadAssetFailureCallback(string uiFormAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            if (m_UIFormsToReleaseOnLoad.Contains(openUIFormInfo.SerialId))
            {
                m_UIFormsToReleaseOnLoad.Remove(openUIFormInfo.SerialId);
                return;
            }

            m_UIFormsBeingLoaded.Remove(openUIFormInfo.SerialId);
            string appendErrorMessage = Utility.Text.Format("Load UI form failure, asset name '{0}', status '{1}', error message '{2}'.", uiFormAssetName, status, errorMessage);
            if (m_OpenUIFormFailureEventHandler != null)
            {
                OpenUIFormFailureEventArgs openUIFormFailureEventArgs = OpenUIFormFailureEventArgs.Create(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.UIGroup.Name, openUIFormInfo.PauseCoveredUIForm, appendErrorMessage, openUIFormInfo.UserData);
                m_OpenUIFormFailureEventHandler(this, openUIFormFailureEventArgs);
                ReferencePool.Release(openUIFormFailureEventArgs);
                return;
            }

            throw new GameFrameworkException(appendErrorMessage);
        }

        private void LoadAssetUpdateCallback(string uiFormAssetName, float progress, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            if (m_OpenUIFormUpdateEventHandler != null)
            {
                OpenUIFormUpdateEventArgs openUIFormUpdateEventArgs = OpenUIFormUpdateEventArgs.Create(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.UIGroup.Name, openUIFormInfo.PauseCoveredUIForm, progress, openUIFormInfo.UserData);
                m_OpenUIFormUpdateEventHandler(this, openUIFormUpdateEventArgs);
                ReferencePool.Release(openUIFormUpdateEventArgs);
            }
        }

        private void LoadAssetDependencyAssetCallback(string uiFormAssetName, string dependencyAssetName, int loadedCount, int totalCount, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            if (m_OpenUIFormDependencyAssetEventHandler != null)
            {
                OpenUIFormDependencyAssetEventArgs openUIFormDependencyAssetEventArgs = OpenUIFormDependencyAssetEventArgs.Create(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.UIGroup.Name, openUIFormInfo.PauseCoveredUIForm, dependencyAssetName, loadedCount, totalCount, openUIFormInfo.UserData);
                m_OpenUIFormDependencyAssetEventHandler(this, openUIFormDependencyAssetEventArgs);
                ReferencePool.Release(openUIFormDependencyAssetEventArgs);
            }
        }
    }
}
