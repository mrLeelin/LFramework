//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Sound
{
    /// <summary>
    /// 澹伴煶绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class SoundManager : GameFrameworkModule, ISoundManager
    {
        private readonly Dictionary<string, SoundGroup> m_SoundGroups;
        private readonly Dictionary<int, SoundPlayRequest> m_PendingRequests;
        private readonly Dictionary<string, SoundAssetEntry> m_SoundAssets;
        private readonly Dictionary<SoundPlaybackLaneKey, SoundPlaybackLane> m_PlaybackLanes;
        private readonly HashSet<SoundGroup> m_QueueWarningRaisedGroups;
        private readonly List<SoundAssetEntry> m_IdleEvictionCandidates;
        private readonly LoadAssetCallbacks m_LoadAssetCallbacks;
        private IResourceManager m_ResourceManager;
        private ISoundHelper m_SoundHelper;
        private int m_Serial;
        private double m_RealTime;
        private bool m_IsShuttingDown;
        private bool m_IsScheduling;
        private bool m_IsEvictingIdleSoundAssets;
        private float m_IdleReleaseSeconds;
        private int m_MaxIdleEntryCount;
        private int m_QueueWarningThreshold;
        private int m_QueueWarningResetThreshold;
        private EventHandler<PlaySoundSuccessEventArgs> m_PlaySoundSuccessEventHandler;
        private EventHandler<PlaySoundFailureEventArgs> m_PlaySoundFailureEventHandler;
        private EventHandler<PlaySoundCanceledEventArgs> m_PlaySoundCanceledEventHandler;
        private EventHandler<PlaySoundUpdateEventArgs> m_PlaySoundUpdateEventHandler;
        private EventHandler<PlaySoundDependencyAssetEventArgs> m_PlaySoundDependencyAssetEventHandler;

        /// <summary>
        /// 鍒濆鍖栧０闊崇鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public SoundManager()
        {
            m_SoundGroups = new Dictionary<string, SoundGroup>(StringComparer.Ordinal);
            m_PendingRequests = new Dictionary<int, SoundPlayRequest>();
            m_SoundAssets = new Dictionary<string, SoundAssetEntry>(StringComparer.Ordinal);
            m_PlaybackLanes = new Dictionary<SoundPlaybackLaneKey, SoundPlaybackLane>();
            m_QueueWarningRaisedGroups = new HashSet<SoundGroup>();
            m_IdleEvictionCandidates = new List<SoundAssetEntry>();
            m_LoadAssetCallbacks = new LoadAssetCallbacks(LoadAssetSuccessCallback, LoadAssetFailureCallback, LoadAssetUpdateCallback, LoadAssetDependencyAssetCallback);
            m_ResourceManager = null;
            m_SoundHelper = null;
            m_Serial = 0;
            m_RealTime = 0d;
            m_IsShuttingDown = false;
            m_IsScheduling = false;
            m_IsEvictingIdleSoundAssets = false;
            m_IdleReleaseSeconds = 2f;
            m_MaxIdleEntryCount = 64;
            m_QueueWarningThreshold = 64;
            m_QueueWarningResetThreshold = 32;
            m_PlaySoundSuccessEventHandler = null;
            m_PlaySoundFailureEventHandler = null;
            m_PlaySoundCanceledEventHandler = null;
            m_PlaySoundUpdateEventHandler = null;
            m_PlaySoundDependencyAssetEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇澹伴煶缁勬暟閲忋€?
        /// </summary>
        public int SoundGroupCount
        {
            get
            {
                return m_SoundGroups.Count;
            }
        }

        /// <summary>
        /// Gets whether the sound manager is shutting down and rejects new playback requests.
        /// </summary>
        public bool IsShuttingDown
        {
            get
            {
                return m_IsShuttingDown;
            }
        }

        /// <summary>
        /// 鎾斁澹伴煶鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<PlaySoundSuccessEventArgs> PlaySoundSuccess
        {
            add
            {
                m_PlaySoundSuccessEventHandler += value;
            }
            remove
            {
                m_PlaySoundSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎾斁澹伴煶澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<PlaySoundFailureEventArgs> PlaySoundFailure
        {
            add
            {
                m_PlaySoundFailureEventHandler += value;
            }
            remove
            {
                m_PlaySoundFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎾斁澹伴煶鏇存柊浜嬩欢銆?
        /// </summary>
        public event EventHandler<PlaySoundUpdateEventArgs> PlaySoundUpdate
        {
            add
            {
                m_PlaySoundUpdateEventHandler += value;
            }
            remove
            {
                m_PlaySoundUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 鎾斁澹伴煶鏃跺姞杞戒緷璧栬祫婧愪簨浠躲€?
        /// </summary>
        public event EventHandler<PlaySoundDependencyAssetEventArgs> PlaySoundDependencyAsset
        {
            add
            {
                m_PlaySoundDependencyAssetEventHandler += value;
            }
            remove
            {
                m_PlaySoundDependencyAssetEventHandler -= value;
            }
        }

        /// <summary>
        /// 澹伴煶绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (realElapseSeconds > 0f)
            {
                m_RealTime += realElapseSeconds;
            }

            Exception exception = ScheduleReadyRequests();
            if (exception != null)
            {
                throw exception;
            }

            EvictIdleSoundAssets();
        }

        /// <summary>
        /// Prevents new sound requests while existing playback is drained during shutdown.
        /// </summary>
        public void PrepareForShutdown()
        {
            m_IsShuttingDown = true;
        }

        /// <summary>
        /// Play sound canceled event.
        /// </summary>
        public event EventHandler<PlaySoundCanceledEventArgs> PlaySoundCanceled
        {
            add
            {
                m_PlaySoundCanceledEventHandler += value;
            }
            remove
            {
                m_PlaySoundCanceledEventHandler -= value;
            }
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗗０闊崇鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            m_IsShuttingDown = true;
            int[] pendingSerialIds = new int[m_PendingRequests.Count];
            m_PendingRequests.Keys.CopyTo(pendingSerialIds, 0);
            foreach (int serialId in pendingSerialIds)
            {
                SoundPlayRequest request = null;
                if (m_PendingRequests.TryGetValue(serialId, out request))
                {
                    CancelRequest(request);
                }
            }

            foreach (KeyValuePair<string, SoundGroup> pair in m_SoundGroups)
            {
                pair.Value.ResetAllSoundAgents();
            }

            m_PlaybackLanes.Clear();
            m_QueueWarningRaisedGroups.Clear();
            SoundAssetEntry[] entries = new SoundAssetEntry[m_SoundAssets.Count];
            m_SoundAssets.Values.CopyTo(entries, 0);
            foreach (SoundAssetEntry entry in entries)
            {
                if (entry.IsLoading)
                {
                    entry.ReleaseOnLoad = true;
                }
                else
                {
                    ReleaseAssetEntry(entry);
                }
            }

            m_SoundAssets.Clear();
            m_SoundGroups.Clear();
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
        /// 璁剧疆澹伴煶杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="soundHelper">澹伴煶杈呭姪鍣ㄣ€?/param>
        public void SetSoundHelper(ISoundHelper soundHelper)
        {
            if (soundHelper == null)
            {
                throw new GameFrameworkException("Sound helper is invalid.");
            }

            m_SoundHelper = soundHelper;
        }

        /// <summary>
        /// Sets sound asset retention and queue warning settings.
        /// </summary>
        public void SetSoundCacheSettings(float idleReleaseSeconds, int maxIdleEntryCount, int queueWarningThreshold, int queueWarningResetThreshold)
        {
            if (float.IsNaN(idleReleaseSeconds) || idleReleaseSeconds < 0f)
            {
                throw new GameFrameworkException("Idle release seconds is invalid.");
            }

            if (maxIdleEntryCount < 0)
            {
                throw new GameFrameworkException("Maximum idle sound entry count is invalid.");
            }

            if (queueWarningThreshold <= 0)
            {
                throw new GameFrameworkException("Queue warning threshold is invalid.");
            }

            if (queueWarningResetThreshold < 0 || queueWarningResetThreshold >= queueWarningThreshold)
            {
                throw new GameFrameworkException("Queue warning reset threshold is invalid.");
            }

            m_IdleReleaseSeconds = idleReleaseSeconds;
            m_MaxIdleEntryCount = maxIdleEntryCount;
            m_QueueWarningThreshold = queueWarningThreshold;
            m_QueueWarningResetThreshold = queueWarningResetThreshold;
            m_QueueWarningRaisedGroups.Clear();
            foreach (KeyValuePair<string, SoundGroup> pair in m_SoundGroups)
            {
                OnQueuedRequestCountChanged(pair.Value);
            }

            EvictIdleSoundAssets();
        }

        /// <summary>
        /// 鏄惁瀛樺湪鎸囧畾澹伴煶缁勩€?
        /// </summary>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <returns>鎸囧畾澹伴煶缁勬槸鍚﹀瓨鍦ㄣ€?/returns>
        public bool HasSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                throw new GameFrameworkException("Sound group name is invalid.");
            }

            return m_SoundGroups.ContainsKey(soundGroupName);
        }

        /// <summary>
        /// 鑾峰彇鎸囧畾澹伴煶缁勩€?
        /// </summary>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑澹伴煶缁勩€?/returns>
        public ISoundGroup GetSoundGroup(string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                throw new GameFrameworkException("Sound group name is invalid.");
            }

            SoundGroup soundGroup = null;
            if (m_SoundGroups.TryGetValue(soundGroupName, out soundGroup))
            {
                return soundGroup;
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊０闊崇粍銆?
        /// </summary>
        /// <returns>鎵€鏈夊０闊崇粍銆?/returns>
        public ISoundGroup[] GetAllSoundGroups()
        {
            int index = 0;
            ISoundGroup[] results = new ISoundGroup[m_SoundGroups.Count];
            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                results[index++] = soundGroup.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊０闊崇粍銆?
        /// </summary>
        /// <param name="results">鎵€鏈夊０闊崇粍銆?/param>
        public void GetAllSoundGroups(List<ISoundGroup> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                results.Add(soundGroup.Value);
            }
        }

        /// <summary>
        /// 澧炲姞澹伴煶缁勩€?
        /// </summary>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="soundGroupHelper">澹伴煶缁勮緟鍔╁櫒銆?/param>
        /// <returns>鏄惁澧炲姞澹伴煶缁勬垚鍔熴€?/returns>
        public bool AddSoundGroup(string soundGroupName, ISoundGroupHelper soundGroupHelper)
        {
            return AddSoundGroup(soundGroupName, false, Constant.DefaultMute, Constant.DefaultVolume, SoundOverflowPolicy.ReplaceByPriority, soundGroupHelper);
        }

        /// <summary>
        /// 澧炲姞澹伴煶缁勩€?
        /// </summary>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="soundGroupAvoidBeingReplacedBySamePriority">澹伴煶缁勪腑鐨勫０闊虫槸鍚﹂伩鍏嶈鍚屼紭鍏堢骇澹伴煶鏇挎崲銆?/param>
        /// <param name="soundGroupMute">澹伴煶缁勬槸鍚﹂潤闊炽€?/param>
        /// <param name="soundGroupVolume">澹伴煶缁勯煶閲忋€?/param>
        /// <param name="soundGroupHelper">澹伴煶缁勮緟鍔╁櫒銆?/param>
        /// <returns>鏄惁澧炲姞澹伴煶缁勬垚鍔熴€?/returns>
        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, ISoundGroupHelper soundGroupHelper)
        {
            return AddSoundGroup(soundGroupName, soundGroupAvoidBeingReplacedBySamePriority, soundGroupMute, soundGroupVolume, SoundOverflowPolicy.ReplaceByPriority, soundGroupHelper);
        }

        /// <summary>
        /// Adds a sound group with an explicit overflow policy.
        /// </summary>
        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, SoundOverflowPolicy overflowPolicy, ISoundGroupHelper soundGroupHelper)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                throw new GameFrameworkException("Sound group name is invalid.");
            }

            if (soundGroupHelper == null)
            {
                throw new GameFrameworkException("Sound group helper is invalid.");
            }

            if (HasSoundGroup(soundGroupName))
            {
                return false;
            }

            SoundGroup soundGroup = new SoundGroup(this, soundGroupName, soundGroupHelper)
            {
                AvoidBeingReplacedBySamePriority = soundGroupAvoidBeingReplacedBySamePriority,
                OverflowPolicy = overflowPolicy,
                Mute = soundGroupMute,
                Volume = soundGroupVolume
            };

            m_SoundGroups.Add(soundGroupName, soundGroup);

            return true;
        }

        /// <summary>
        /// 澧炲姞澹伴煶浠ｇ悊杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="soundAgentHelper">瑕佸鍔犵殑澹伴煶浠ｇ悊杈呭姪鍣ㄣ€?/param>
        public void AddSoundAgentHelper(string soundGroupName, ISoundAgentHelper soundAgentHelper)
        {
            if (m_SoundHelper == null)
            {
                throw new GameFrameworkException("You must set sound helper first.");
            }

            SoundGroup soundGroup = (SoundGroup)GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Sound group '{0}' is not exist.", soundGroupName));
            }

            soundGroup.AddSoundAgentHelper(soundAgentHelper);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?
        /// </summary>
        /// <returns>鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?/returns>
        public int[] GetAllLoadingSoundSerialIds()
        {
            List<int> results = new List<int>();
            GetAllLoadingSoundSerialIds(results);
            return results.ToArray();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?
        /// </summary>
        /// <param name="results">鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?/param>
        public void GetAllLoadingSoundSerialIds(List<int> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<int, SoundPlayRequest> pair in m_PendingRequests)
            {
                if (pair.Value.State == SoundPlayRequestState.WaitingAsset)
                {
                    results.Add(pair.Key);
                }
            }

            results.Sort();
        }

        /// <summary>
        /// 鏄惁姝ｅ湪鍔犺浇澹伴煶銆?
        /// </summary>
        /// <param name="serialId">澹伴煶搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁姝ｅ湪鍔犺浇澹伴煶銆?/returns>
        public bool IsLoadingSound(int serialId)
        {
            SoundPlayRequest request = null;
            return m_PendingRequests.TryGetValue(serialId, out request) && request.State == SoundPlayRequestState.WaitingAsset;
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName)
        {
            return PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, null);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇澹伴煶璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, null, null);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="playSoundParams">鎾斁澹伴煶鍙傛暟銆?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, playSoundParams, null);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, Constant.DefaultPriority, null, userData);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇澹伴煶璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="playSoundParams">鎾斁澹伴煶鍙傛暟銆?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇澹伴煶璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, null, userData);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="playSoundParams">鎾斁澹伴煶鍙傛暟銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName,Constant.DefaultPriority, playSoundParams, userData);
        }

        /// <summary>
        /// 鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="soundAssetName">澹伴煶璧勬簮鍚嶇О銆?/param>
        /// <param name="soundGroupName">澹伴煶缁勫悕绉般€?/param>
        /// <param name="priority">鍔犺浇澹伴煶璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="playSoundParams">鎾斁澹伴煶鍙傛暟銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>澹伴煶鐨勫簭鍒楃紪鍙枫€?/returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, object userData)
        {
            if (m_IsShuttingDown)
            {
                PlaySoundParams.ReleaseIfReferenced(playSoundParams);
                throw new GameFrameworkException("Sound manager is shutting down.");
            }

            if (m_ResourceManager == null)
            {
                PlaySoundParams.ReleaseIfReferenced(playSoundParams);
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (m_SoundHelper == null)
            {
                PlaySoundParams.ReleaseIfReferenced(playSoundParams);
                throw new GameFrameworkException("You must set sound helper first.");
            }

            if (playSoundParams == null)
            {
                playSoundParams = PlaySoundParams.Create();
            }

            int serialId = ++m_Serial;
            PlaySoundErrorCode? errorCode = null;
            string errorMessage = null;
            SoundGroup soundGroup = (SoundGroup)GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                errorCode = PlaySoundErrorCode.SoundGroupNotExist;
                errorMessage = Utility.Text.Format("Sound group '{0}' is not exist.", soundGroupName);
            }
            else if (soundGroup.SoundAgentCount <= 0)
            {
                errorCode = PlaySoundErrorCode.SoundGroupHasNoAgent;
                errorMessage = Utility.Text.Format("Sound group '{0}' is have no sound agent.", soundGroupName);
            }
            else if (soundGroup.OverflowPolicy == SoundOverflowPolicy.QueueNonLooping && playSoundParams.Loop)
            {
                errorCode = PlaySoundErrorCode.LoopingSoundNotSupportedByQueue;
                errorMessage = Utility.Text.Format("Sound group '{0}' does not support looping sounds with queue overflow policy.", soundGroupName);
            }

            if (errorCode.HasValue)
            {
                try
                {
                    if (m_PlaySoundFailureEventHandler != null)
                    {
                        PlaySoundFailureEventArgs eventArgs = PlaySoundFailureEventArgs.Create(serialId, soundAssetName, soundGroupName, playSoundParams, errorCode.Value, errorMessage, true, userData);
                        Exception callbackException = null;
                        try
                        {
                            callbackException = InvokePlaySoundFailureHandlers(eventArgs);
                        }
                        finally
                        {
                            ReferencePool.Release(eventArgs);
                        }

                        if (callbackException != null)
                        {
                            throw callbackException;
                        }

                        return serialId;
                    }
                }
                finally
                {
                    if (playSoundParams.Referenced)
                    {
                        ReferencePool.Release(playSoundParams);
                    }
                }

                throw new GameFrameworkException(errorMessage);
            }

            SoundPlayRequest request = SoundPlayRequest.Create(serialId, soundAssetName, soundGroup, priority, playSoundParams, userData, GetRealTime());
            m_PendingRequests.Add(serialId, request);

            SoundAssetEntry entry = null;
            if (m_SoundAssets.TryGetValue(soundAssetName, out entry))
            {
                AttachRequestToEntry(request, entry);
                if (entry.IsLoading)
                {
                    entry.ReleaseOnLoad = false;
                    request.WaitedForLoad = true;
                    request.State = SoundPlayRequestState.WaitingAsset;
                    entry.LoadingRequests.Add(request);
                }
                else
                {
                    request.State = SoundPlayRequestState.WaitingAgent;
                    Exception exception;
                    if (soundGroup.OverflowPolicy == SoundOverflowPolicy.QueueNonLooping)
                    {
                        EnqueueSoundRequest(request, entry);
                        exception = ScheduleReadyRequests();
                    }
                    else
                    {
                        exception = StartSoundRequest(request, entry, 0f);
                    }

                    if (exception != null)
                    {
                        throw exception;
                    }
                }

                NotifyQueuedRequestAccepted(serialId, request);
                return serialId;
            }

            entry = new SoundAssetEntry(soundAssetName);
            AttachRequestToEntry(request, entry);
            request.WaitedForLoad = true;
            request.State = SoundPlayRequestState.WaitingAsset;
            entry.LoadingRequests.Add(request);
            m_SoundAssets.Add(soundAssetName, entry);
            NotifyQueuedRequestAccepted(serialId, request);
            SoundAssetEntry cachedEntry = null;
            if (!m_IsShuttingDown &&
                m_SoundAssets.TryGetValue(soundAssetName, out cachedEntry) &&
                ReferenceEquals(cachedEntry, entry))
            {
                if (entry.LoadingRequests.Count > 0)
                {
                    m_ResourceManager.LoadAsset(soundAssetName, null, priority, m_LoadAssetCallbacks, entry);
                }
                else
                {
                    ReleaseAssetEntry(entry);
                }
            }

            return serialId;
        }

        /// <summary>
        /// 鍋滄鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佸仠姝㈡挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁鍋滄鎾斁澹伴煶鎴愬姛銆?/returns>
        public bool StopSound(int serialId)
        {
            return StopSound(serialId, Constant.DefaultFadeOutSeconds);
        }

        /// <summary>
        /// 鍋滄鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佸仠姝㈡挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        /// <param name="fadeOutSeconds">澹伴煶娣″嚭鏃堕棿锛屼互绉掍负鍗曚綅銆?/param>
        /// <returns>鏄惁鍋滄鎾斁澹伴煶鎴愬姛銆?/returns>
        public bool StopSound(int serialId, float fadeOutSeconds)
        {
            SoundPlayRequest request = null;
            if (m_PendingRequests.TryGetValue(serialId, out request))
            {
                CancelRequest(request);
                return true;
            }

            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                if (soundGroup.Value.StopSound(serialId, fadeOutSeconds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 鍋滄鎵€鏈夊凡鍔犺浇鐨勫０闊炽€?
        /// </summary>
        public void StopAllLoadedSounds()
        {
            StopAllLoadedSounds(Constant.DefaultFadeOutSeconds);
        }

        /// <summary>
        /// 鍋滄鎵€鏈夊凡鍔犺浇鐨勫０闊炽€?
        /// </summary>
        /// <param name="fadeOutSeconds">澹伴煶娣″嚭鏃堕棿锛屼互绉掍负鍗曚綅銆?/param>
        public void StopAllLoadedSounds(float fadeOutSeconds)
        {
            List<int> waitingAgentSerialIds = new List<int>();
            foreach (KeyValuePair<int, SoundPlayRequest> pair in m_PendingRequests)
            {
                if (pair.Value.State == SoundPlayRequestState.WaitingAgent)
                {
                    waitingAgentSerialIds.Add(pair.Key);
                }
            }

            waitingAgentSerialIds.Sort();
            foreach (int serialId in waitingAgentSerialIds)
            {
                SoundPlayRequest request = null;
                if (m_PendingRequests.TryGetValue(serialId, out request) && request.State == SoundPlayRequestState.WaitingAgent)
                {
                    CancelRequest(request);
                }
            }

            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                soundGroup.Value.StopAllLoadedSounds(fadeOutSeconds);
            }
        }

        /// <summary>
        /// 鍋滄鎵€鏈夋鍦ㄥ姞杞界殑澹伴煶銆?
        /// </summary>
        public void StopAllLoadingSounds()
        {
            int[] serialIds = GetAllLoadingSoundSerialIds();
            foreach (int serialId in serialIds)
            {
                StopSound(serialId);
            }
        }

        /// <summary>
        /// 鏆傚仠鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佹殏鍋滄挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        public void PauseSound(int serialId)
        {
            PauseSound(serialId, Constant.DefaultFadeOutSeconds);
        }

        /// <summary>
        /// 鏆傚仠鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佹殏鍋滄挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        /// <param name="fadeOutSeconds">澹伴煶娣″嚭鏃堕棿锛屼互绉掍负鍗曚綅銆?/param>
        public void PauseSound(int serialId, float fadeOutSeconds)
        {
            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                if (soundGroup.Value.PauseSound(serialId, fadeOutSeconds))
                {
                    return;
                }
            }

            throw new GameFrameworkException(Utility.Text.Format("Can not find sound '{0}'.", serialId));
        }

        /// <summary>
        /// 鎭㈠鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佹仮澶嶆挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        public void ResumeSound(int serialId)
        {
            ResumeSound(serialId, Constant.DefaultFadeInSeconds);
        }

        /// <summary>
        /// 鎭㈠鎾斁澹伴煶銆?
        /// </summary>
        /// <param name="serialId">瑕佹仮澶嶆挱鏀惧０闊崇殑搴忓垪缂栧彿銆?/param>
        /// <param name="fadeInSeconds">澹伴煶娣″叆鏃堕棿锛屼互绉掍负鍗曚綅銆?/param>
        public void ResumeSound(int serialId, float fadeInSeconds)
        {
            foreach (KeyValuePair<string, SoundGroup> soundGroup in m_SoundGroups)
            {
                if (soundGroup.Value.ResumeSound(serialId, fadeInSeconds))
                {
                    return;
                }
            }

            throw new GameFrameworkException(Utility.Text.Format("Can not find sound '{0}'.", serialId));
        }

        private void RetainAgentBorrow(SoundAssetEntry entry)
        {
            entry.IsIdle = false;
            entry.AgentBorrowCount++;
        }

        private void ReleaseAgentBorrow(SoundAssetEntry entry)
        {
            entry.AgentBorrowCount--;
            if (entry.AgentBorrowCount < 0)
            {
                throw new GameFrameworkException("Sound asset entry borrow count is invalid.");
            }

            if (entry.AgentBorrowCount == 0)
            {
                if (m_IsShuttingDown || entry.ReleaseWhenIdle)
                {
                    ReleaseAssetEntry(entry);
                    return;
                }

                TryMarkEntryIdle(entry);
            }
        }

        private void AttachRequestToEntry(SoundPlayRequest request, SoundAssetEntry entry)
        {
            request.AssetEntry = entry;
            entry.PendingRequestCount++;
            entry.IsIdle = false;
            if (request.SoundGroup.OverflowPolicy == SoundOverflowPolicy.QueueNonLooping)
            {
                request.CountsTowardQueue = true;
                request.SoundGroup.IncrementQueuedRequestCount();
            }
        }

        private void NotifyQueuedRequestAccepted(int serialId, SoundPlayRequest request)
        {
            SoundPlayRequest pendingRequest = null;
            if (m_PendingRequests.TryGetValue(serialId, out pendingRequest) &&
                ReferenceEquals(pendingRequest, request) &&
                request.CountsTowardQueue)
            {
                OnQueuedRequestCountChanged(request.SoundGroup);
            }
        }

        private void DetachRequestFromEntry(SoundPlayRequest request)
        {
            SoundAssetEntry entry = request.AssetEntry;
            request.AssetEntry = null;
            if (entry != null)
            {
                entry.LoadingRequests.Remove(request);
                entry.PendingRequestCount--;
                if (entry.PendingRequestCount < 0)
                {
                    throw new GameFrameworkException("Sound asset entry pending request count is invalid.");
                }
            }

            if (request.CountsTowardQueue)
            {
                request.CountsTowardQueue = false;
                request.SoundGroup.DecrementQueuedRequestCount();
            }

            if (entry != null)
            {
                TryMarkEntryIdle(entry);
            }
        }

        private void EnqueueSoundRequest(SoundPlayRequest request, SoundAssetEntry entry)
        {
            SoundPlaybackLaneKey key = new SoundPlaybackLaneKey(request.SoundGroup, request.SoundAssetName);
            SoundPlaybackLane lane = null;
            if (!m_PlaybackLanes.TryGetValue(key, out lane))
            {
                lane = new SoundPlaybackLane(key, request.SoundGroup, entry);
                m_PlaybackLanes.Add(key, lane);
            }
            else if (!ReferenceEquals(lane.AssetEntry, entry))
            {
                throw new GameFrameworkException(Utility.Text.Format("Sound playback lane '{0}' has a different asset entry.", request.SoundAssetName));
            }

            request.LaneNode = lane.Requests.AddLast(request);
        }

        private void RemoveSoundRequestFromLane(SoundPlayRequest request)
        {
            LinkedListNode<SoundPlayRequest> laneNode = request.LaneNode;
            if (laneNode == null)
            {
                return;
            }

            request.LaneNode = null;
            SoundPlaybackLaneKey key = new SoundPlaybackLaneKey(request.SoundGroup, request.SoundAssetName);
            SoundPlaybackLane lane = null;
            if (!m_PlaybackLanes.TryGetValue(key, out lane) || !ReferenceEquals(lane.Requests, laneNode.List))
            {
                return;
            }

            lane.Requests.Remove(laneNode);
            if (lane.Requests.Count == 0)
            {
                m_PlaybackLanes.Remove(key);
            }
        }

        private Exception ScheduleReadyRequests()
        {
            if (m_IsScheduling || m_IsShuttingDown)
            {
                return null;
            }

            m_IsScheduling = true;
            Exception firstException = null;
            try
            {
                while (true)
                {
                    SoundPlaybackLane selectedLane = null;
                    SoundPlayRequest selectedRequest = null;
                    double selectedDueTime = 0d;
                    foreach (KeyValuePair<SoundPlaybackLaneKey, SoundPlaybackLane> pair in m_PlaybackLanes)
                    {
                        SoundPlaybackLane lane = pair.Value;
                        if (lane.Requests.Count == 0 || !lane.SoundGroup.HasIdleSoundAgent)
                        {
                            continue;
                        }

                        SoundPlayRequest request = lane.Requests.First.Value;
                        double dueTime = lane.GetDueTime(request, GetRealTime());
                        if (dueTime > GetRealTime())
                        {
                            continue;
                        }

                        if (selectedRequest == null ||
                            request.PlaySoundParams.Priority > selectedRequest.PlaySoundParams.Priority ||
                            (request.PlaySoundParams.Priority == selectedRequest.PlaySoundParams.Priority && request.SerialId < selectedRequest.SerialId))
                        {
                            selectedLane = lane;
                            selectedRequest = request;
                            selectedDueTime = dueTime;
                        }
                    }

                    if (selectedRequest == null)
                    {
                        break;
                    }

                    selectedLane.Requests.RemoveFirst();
                    selectedRequest.LaneNode = null;
                    if (selectedLane.Requests.Count == 0)
                    {
                        m_PlaybackLanes.Remove(selectedLane.Key);
                    }

                    double actualStartTime = GetRealTime();
                    Exception exception = StartSoundRequest(
                        selectedRequest,
                        selectedLane.AssetEntry,
                        selectedRequest.WaitedForLoad ? selectedLane.AssetEntry.LoadDuration : 0f);
                    selectedLane.ShiftAfterLateStart(selectedDueTime, actualStartTime);
                    if (firstException == null)
                    {
                        firstException = exception;
                    }
                }
            }
            finally
            {
                m_IsScheduling = false;
            }

            return firstException;
        }

        private Exception StartSoundRequest(SoundPlayRequest request, SoundAssetEntry entry, float loadDuration)
        {
            PlaySoundErrorCode? errorCode = null;
            ISoundAgent soundAgent = request.SoundGroup.TryPlaySound(request.SerialId, entry, request.PlaySoundParams, GetRealTime(), out errorCode);
            if (soundAgent != null)
            {
                return CompleteStartedRequest(request, soundAgent, loadDuration);
            }

            string errorMessage = Utility.Text.Format("Sound group '{0}' play sound '{1}' failure.", request.SoundGroup.Name, request.SoundAssetName);
            if (errorCode == PlaySoundErrorCode.SetSoundAssetFailure)
            {
                InvalidateSoundAsset(entry);
            }

            return FailRequest(request, errorCode.Value, errorMessage, true);
        }

        private Exception CompleteStartedRequest(SoundPlayRequest request, ISoundAgent soundAgent, float loadDuration)
        {
            SoundPlayRequest pendingRequest = null;
            if (!m_PendingRequests.TryGetValue(request.SerialId, out pendingRequest) || !ReferenceEquals(pendingRequest, request))
            {
                return null;
            }

            Exception callbackException = null;
            request.State = SoundPlayRequestState.Started;
            RemoveSoundRequestFromLane(request);
            m_PendingRequests.Remove(request.SerialId);
            DetachRequestFromEntry(request);
            try
            {
                if (m_PlaySoundSuccessEventHandler != null)
                {
                    PlaySoundSuccessEventArgs eventArgs = PlaySoundSuccessEventArgs.Create(request.SerialId, request.SoundAssetName, soundAgent, loadDuration, request.UserData);
                    try
                    {
                        Delegate[] handlers = m_PlaySoundSuccessEventHandler.GetInvocationList();
                        foreach (EventHandler<PlaySoundSuccessEventArgs> handler in handlers)
                        {
                            try
                            {
                                handler(this, eventArgs);
                            }
                            catch (Exception exception)
                            {
                                if (callbackException == null)
                                {
                                    callbackException = exception;
                                }
                            }
                        }
                    }
                    finally
                    {
                        ReferencePool.Release(eventArgs);
                    }
                }
            }
            finally
            {
                ReleaseRequest(request);
            }

            return callbackException;
        }

        private Exception FailRequest(SoundPlayRequest request, PlaySoundErrorCode errorCode, string errorMessage, bool isPrimaryFailure)
        {
            SoundPlayRequest pendingRequest = null;
            if (!m_PendingRequests.TryGetValue(request.SerialId, out pendingRequest) || !ReferenceEquals(pendingRequest, request))
            {
                return null;
            }

            bool hasFailureHandler = m_PlaySoundFailureEventHandler != null;
            Exception callbackException = null;
            request.State = SoundPlayRequestState.Failed;
            RemoveSoundRequestFromLane(request);
            m_PendingRequests.Remove(request.SerialId);
            DetachRequestFromEntry(request);
            try
            {
                if (hasFailureHandler)
                {
                    PlaySoundFailureEventArgs eventArgs = PlaySoundFailureEventArgs.Create(request.SerialId, request.SoundAssetName, request.SoundGroup.Name, request.PlaySoundParams, errorCode, errorMessage, isPrimaryFailure, request.UserData);
                    try
                    {
                        callbackException = InvokePlaySoundFailureHandlers(eventArgs);
                    }
                    finally
                    {
                        ReferencePool.Release(eventArgs);
                    }
                }
            }
            finally
            {
                ReleaseRequest(request);
            }

            return callbackException ?? (hasFailureHandler ? null : new GameFrameworkException(errorMessage));
        }

        private Exception InvokePlaySoundFailureHandlers(PlaySoundFailureEventArgs eventArgs)
        {
            Exception firstException = null;
            Delegate[] handlers = m_PlaySoundFailureEventHandler.GetInvocationList();
            foreach (EventHandler<PlaySoundFailureEventArgs> handler in handlers)
            {
                try
                {
                    handler(this, eventArgs);
                }
                catch (Exception exception)
                {
                    if (firstException == null)
                    {
                        firstException = exception;
                    }
                }
            }

            return firstException;
        }

        private void CancelRequest(SoundPlayRequest request)
        {
            SoundPlayRequest pendingRequest = null;
            if (!m_PendingRequests.TryGetValue(request.SerialId, out pendingRequest) || !ReferenceEquals(pendingRequest, request))
            {
                return;
            }

            request.State = SoundPlayRequestState.Cancelled;
            SoundAssetEntry entry = request.AssetEntry;
            if (entry != null)
            {
                entry.LoadingRequests.Remove(request);
                if (entry.IsLoading && entry.LoadingRequests.Count == 0)
                {
                    entry.ReleaseOnLoad = true;
                }
            }

            RemoveSoundRequestFromLane(request);
            m_PendingRequests.Remove(request.SerialId);
            DetachRequestFromEntry(request);
            try
            {
                if (m_PlaySoundCanceledEventHandler != null)
                {
                    PlaySoundCanceledEventArgs eventArgs = PlaySoundCanceledEventArgs.Create(request.SerialId, request.SoundAssetName, request.SoundGroup.Name, request.UserData);
                    try
                    {
                        Delegate[] handlers = m_PlaySoundCanceledEventHandler.GetInvocationList();
                        foreach (EventHandler<PlaySoundCanceledEventArgs> handler in handlers)
                        {
                            try
                            {
                                handler(this, eventArgs);
                            }
                            catch
                            {
                            }
                        }
                    }
                    finally
                    {
                        ReferencePool.Release(eventArgs);
                    }
                }
            }
            finally
            {
                ReleaseRequest(request);
            }
        }

        private void ReleaseRequest(SoundPlayRequest request)
        {
            if (request.PlaySoundParams.Referenced)
            {
                ReferencePool.Release(request.PlaySoundParams);
            }

            ReferencePool.Release(request);
        }

        private void TryMarkEntryIdle(SoundAssetEntry entry)
        {
            if (entry.Released || entry.IsLoading || entry.Asset == null || entry.PendingRequestCount != 0 || entry.AgentBorrowCount != 0)
            {
                return;
            }

            if (!entry.IsIdle)
            {
                entry.IsIdle = true;
                entry.IdleSince = GetRealTime();
            }
        }

        private void EvictIdleSoundAssets()
        {
            if (m_IsShuttingDown || m_IsEvictingIdleSoundAssets || m_SoundAssets.Count == 0)
            {
                return;
            }

            m_IsEvictingIdleSoundAssets = true;
            try
            {
                CollectIdleEvictionCandidates();
                double realTime = GetRealTime();
                foreach (SoundAssetEntry entry in m_IdleEvictionCandidates)
                {
                    if (IsIdleEvictionCandidate(entry) &&
                        realTime - entry.IdleSince + 0.0001d >= m_IdleReleaseSeconds)
                    {
                        ReleaseAssetEntry(entry);
                    }
                }

                CollectIdleEvictionCandidates();
                while (m_IdleEvictionCandidates.Count > m_MaxIdleEntryCount)
                {
                    SoundAssetEntry entry = m_IdleEvictionCandidates[0];
                    if (!IsIdleEvictionCandidate(entry))
                    {
                        CollectIdleEvictionCandidates();
                        continue;
                    }

                    ReleaseAssetEntry(entry);
                    CollectIdleEvictionCandidates();
                }
            }
            finally
            {
                m_IdleEvictionCandidates.Clear();
                m_IsEvictingIdleSoundAssets = false;
            }
        }

        private void CollectIdleEvictionCandidates()
        {
            m_IdleEvictionCandidates.Clear();
            foreach (KeyValuePair<string, SoundAssetEntry> pair in m_SoundAssets)
            {
                if (IsIdleEvictionCandidate(pair.Value))
                {
                    m_IdleEvictionCandidates.Add(pair.Value);
                }
            }

            m_IdleEvictionCandidates.Sort((left, right) =>
            {
                int timeComparison = left.IdleSince.CompareTo(right.IdleSince);
                return timeComparison != 0 ? timeComparison : string.CompareOrdinal(left.SoundAssetName, right.SoundAssetName);
            });
        }

        private bool IsIdleEvictionCandidate(SoundAssetEntry entry)
        {
            SoundAssetEntry cachedEntry = null;
            return entry != null &&
                   !entry.Released &&
                   entry.IsIdle &&
                   !entry.IsLoading &&
                   entry.Asset != null &&
                   entry.PendingRequestCount == 0 &&
                   entry.AgentBorrowCount == 0 &&
                   m_SoundAssets.TryGetValue(entry.SoundAssetName, out cachedEntry) &&
                   ReferenceEquals(cachedEntry, entry);
        }

        private void ReleaseAssetEntry(SoundAssetEntry entry)
        {
            if (entry.Released)
            {
                return;
            }

            entry.Released = true;
            entry.IsIdle = false;
            SoundAssetEntry cachedEntry = null;
            if (m_SoundAssets.TryGetValue(entry.SoundAssetName, out cachedEntry) && ReferenceEquals(cachedEntry, entry))
            {
                m_SoundAssets.Remove(entry.SoundAssetName);
            }

            object asset = entry.Asset;
            entry.Asset = null;
            if (asset != null)
            {
                m_SoundHelper.ReleaseSoundAsset(asset);
            }
        }

        private void InvalidateSoundAsset(SoundAssetEntry entry)
        {
            SoundAssetEntry cachedEntry = null;
            if (m_SoundAssets.TryGetValue(entry.SoundAssetName, out cachedEntry) && ReferenceEquals(cachedEntry, entry))
            {
                m_SoundAssets.Remove(entry.SoundAssetName);
            }

            entry.ReleaseWhenIdle = true;
            if (entry.AgentBorrowCount == 0)
            {
                ReleaseAssetEntry(entry);
            }
        }

        private void OnQueuedRequestCountChanged(SoundGroup soundGroup)
        {
            int queuedRequestCount = soundGroup.QueuedRequestCount;
            if (queuedRequestCount <= m_QueueWarningResetThreshold)
            {
                m_QueueWarningRaisedGroups.Remove(soundGroup);
                return;
            }

            if (m_IsShuttingDown || queuedRequestCount < m_QueueWarningThreshold || !m_QueueWarningRaisedGroups.Add(soundGroup))
            {
                return;
            }

            GameFrameworkLog.Warning(Utility.Text.Format(
                "Sound group '{0}' queued request count reached {1}, agent count is {2}, overflow policy is '{3}'.",
                soundGroup.Name, queuedRequestCount, soundGroup.SoundAgentCount, soundGroup.OverflowPolicy));
        }

        private int[] GetLoadingRequestSerialIds(SoundAssetEntry entry)
        {
            int count = entry.LoadingRequests.Count;
            int[] serialIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                serialIds[i] = entry.LoadingRequests[i].SerialId;
            }

            return serialIds;
        }

        private bool TryGetWaitingAssetRequest(int serialId, SoundAssetEntry entry, out SoundPlayRequest request)
        {
            return m_PendingRequests.TryGetValue(serialId, out request) &&
                   request.State == SoundPlayRequestState.WaitingAsset &&
                   ReferenceEquals(request.AssetEntry, entry);
        }

        private double GetRealTime()
        {
            return m_RealTime;
        }

        private void LoadAssetSuccessCallback(string soundAssetName, object soundAsset, float duration, object userData)
        {
            SoundAssetEntry entry = userData as SoundAssetEntry;
            if (entry == null || !string.Equals(entry.SoundAssetName, soundAssetName, StringComparison.Ordinal))
            {
                throw new GameFrameworkException("Sound asset entry is invalid.");
            }

            entry.IsLoading = false;
            if (entry.Released)
            {
                if (soundAsset != null)
                {
                    m_SoundHelper.ReleaseSoundAsset(soundAsset);
                }

                return;
            }

            entry.Asset = soundAsset;
            entry.LoadDuration = duration;
            if (m_IsShuttingDown || entry.ReleaseOnLoad)
            {
                ReleaseAssetEntry(entry);
                return;
            }

            int[] serialIds = GetLoadingRequestSerialIds(entry);
            entry.LoadingRequests.Clear();
            Exception firstException = null;
            foreach (int serialId in serialIds)
            {
                SoundPlayRequest request = null;
                if (!TryGetWaitingAssetRequest(serialId, entry, out request))
                {
                    continue;
                }

                request.State = SoundPlayRequestState.WaitingAgent;
                Exception exception = null;
                if (request.SoundGroup.OverflowPolicy == SoundOverflowPolicy.QueueNonLooping)
                {
                    EnqueueSoundRequest(request, entry);
                }
                else
                {
                    exception = StartSoundRequest(request, entry, request.WaitedForLoad ? duration : 0f);
                }

                if (firstException == null)
                {
                    firstException = exception;
                }
            }

            Exception schedulingException = ScheduleReadyRequests();
            if (firstException == null)
            {
                firstException = schedulingException;
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private void LoadAssetFailureCallback(string soundAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            SoundAssetEntry entry = userData as SoundAssetEntry;
            if (entry == null || !string.Equals(entry.SoundAssetName, soundAssetName, StringComparison.Ordinal))
            {
                throw new GameFrameworkException("Sound asset entry is invalid.");
            }

            entry.IsLoading = false;
            if (entry.Released)
            {
                return;
            }

            entry.Released = true;
            SoundAssetEntry cachedEntry = null;
            if (m_SoundAssets.TryGetValue(soundAssetName, out cachedEntry) && ReferenceEquals(cachedEntry, entry))
            {
                m_SoundAssets.Remove(soundAssetName);
            }
            int[] serialIds = GetLoadingRequestSerialIds(entry);
            entry.LoadingRequests.Clear();
            string appendErrorMessage = Utility.Text.Format("Load sound failure, asset name '{0}', status '{1}', error message '{2}'.", soundAssetName, status, errorMessage);
            Exception firstException = null;
            bool hasPrimaryFailure = false;
            foreach (int serialId in serialIds)
            {
                SoundPlayRequest request = null;
                if (!TryGetWaitingAssetRequest(serialId, entry, out request))
                {
                    continue;
                }

                bool isPrimaryFailure = !hasPrimaryFailure;
                hasPrimaryFailure = true;
                Exception exception = FailRequest(request, PlaySoundErrorCode.LoadAssetFailure, appendErrorMessage, isPrimaryFailure);
                if (firstException == null)
                {
                    firstException = exception;
                }
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private void LoadAssetUpdateCallback(string soundAssetName, float progress, object userData)
        {
            SoundAssetEntry entry = userData as SoundAssetEntry;
            if (entry == null || !string.Equals(entry.SoundAssetName, soundAssetName, StringComparison.Ordinal))
            {
                throw new GameFrameworkException("Sound asset entry is invalid.");
            }

            if (m_PlaySoundUpdateEventHandler == null)
            {
                return;
            }

            int[] serialIds = GetLoadingRequestSerialIds(entry);
            Exception firstException = null;
            foreach (int serialId in serialIds)
            {
                SoundPlayRequest request = null;
                if (!TryGetWaitingAssetRequest(serialId, entry, out request))
                {
                    continue;
                }

                PlaySoundUpdateEventArgs eventArgs = PlaySoundUpdateEventArgs.Create(request.SerialId, soundAssetName, request.SoundGroup.Name, request.PlaySoundParams, progress, request.UserData);
                try
                {
                    Delegate[] handlers = m_PlaySoundUpdateEventHandler.GetInvocationList();
                    foreach (EventHandler<PlaySoundUpdateEventArgs> handler in handlers)
                    {
                        try
                        {
                            handler(this, eventArgs);
                        }
                        catch (Exception exception)
                        {
                            if (firstException == null)
                            {
                                firstException = exception;
                            }
                        }
                    }
                }
                finally
                {
                    ReferencePool.Release(eventArgs);
                }
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private void LoadAssetDependencyAssetCallback(string soundAssetName, string dependencyAssetName, int loadedCount, int totalCount, object userData)
        {
            SoundAssetEntry entry = userData as SoundAssetEntry;
            if (entry == null || !string.Equals(entry.SoundAssetName, soundAssetName, StringComparison.Ordinal))
            {
                throw new GameFrameworkException("Sound asset entry is invalid.");
            }

            if (m_PlaySoundDependencyAssetEventHandler == null)
            {
                return;
            }

            int[] serialIds = GetLoadingRequestSerialIds(entry);
            Exception firstException = null;
            foreach (int serialId in serialIds)
            {
                SoundPlayRequest request = null;
                if (!TryGetWaitingAssetRequest(serialId, entry, out request))
                {
                    continue;
                }

                PlaySoundDependencyAssetEventArgs eventArgs = PlaySoundDependencyAssetEventArgs.Create(request.SerialId, soundAssetName, request.SoundGroup.Name, request.PlaySoundParams, dependencyAssetName, loadedCount, totalCount, request.UserData);
                try
                {
                    Delegate[] handlers = m_PlaySoundDependencyAssetEventHandler.GetInvocationList();
                    foreach (EventHandler<PlaySoundDependencyAssetEventArgs> handler in handlers)
                    {
                        try
                        {
                            handler(this, eventArgs);
                        }
                        catch (Exception exception)
                        {
                            if (firstException == null)
                            {
                                firstException = exception;
                            }
                        }
                    }
                }
                finally
                {
                    ReferencePool.Release(eventArgs);
                }
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }
    }
}
