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
        private readonly List<int> m_SoundsBeingLoaded;
        private readonly HashSet<int> m_SoundsToReleaseOnLoad;
        private readonly LoadAssetCallbacks m_LoadAssetCallbacks;
        private IResourceManager m_ResourceManager;
        private ISoundHelper m_SoundHelper;
        private int m_Serial;
        private EventHandler<PlaySoundSuccessEventArgs> m_PlaySoundSuccessEventHandler;
        private EventHandler<PlaySoundFailureEventArgs> m_PlaySoundFailureEventHandler;
        private EventHandler<PlaySoundUpdateEventArgs> m_PlaySoundUpdateEventHandler;
        private EventHandler<PlaySoundDependencyAssetEventArgs> m_PlaySoundDependencyAssetEventHandler;

        /// <summary>
        /// 鍒濆鍖栧０闊崇鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public SoundManager()
        {
            m_SoundGroups = new Dictionary<string, SoundGroup>(StringComparer.Ordinal);
            m_SoundsBeingLoaded = new List<int>();
            m_SoundsToReleaseOnLoad = new HashSet<int>();
            m_LoadAssetCallbacks = new LoadAssetCallbacks(LoadAssetSuccessCallback, LoadAssetFailureCallback, LoadAssetUpdateCallback, LoadAssetDependencyAssetCallback);
            m_ResourceManager = null;
            m_SoundHelper = null;
            m_Serial = 0;
            m_PlaySoundSuccessEventHandler = null;
            m_PlaySoundFailureEventHandler = null;
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
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗗０闊崇鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            StopAllLoadedSounds();
            m_SoundGroups.Clear();
            m_SoundsBeingLoaded.Clear();
            m_SoundsToReleaseOnLoad.Clear();
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
            return AddSoundGroup(soundGroupName, false, Constant.DefaultMute, Constant.DefaultVolume, soundGroupHelper);
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

            SoundGroup soundGroup = new SoundGroup(soundGroupName, soundGroupHelper)
            {
                AvoidBeingReplacedBySamePriority = soundGroupAvoidBeingReplacedBySamePriority,
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

            soundGroup.AddSoundAgentHelper(m_SoundHelper, soundAgentHelper);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?
        /// </summary>
        /// <returns>鎵€鏈夋鍦ㄥ姞杞藉０闊崇殑搴忓垪缂栧彿銆?/returns>
        public int[] GetAllLoadingSoundSerialIds()
        {
            return m_SoundsBeingLoaded.ToArray();
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
            results.AddRange(m_SoundsBeingLoaded);
        }

        /// <summary>
        /// 鏄惁姝ｅ湪鍔犺浇澹伴煶銆?
        /// </summary>
        /// <param name="serialId">澹伴煶搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁姝ｅ湪鍔犺浇澹伴煶銆?/returns>
        public bool IsLoadingSound(int serialId)
        {
            return m_SoundsBeingLoaded.Contains(serialId);
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
            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (m_SoundHelper == null)
            {
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

            if (errorCode.HasValue)
            {
                if (m_PlaySoundFailureEventHandler != null)
                {
                    PlaySoundFailureEventArgs playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(serialId, soundAssetName, soundGroupName, playSoundParams, errorCode.Value, errorMessage, userData);
                    m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                    ReferencePool.Release(playSoundFailureEventArgs);

                    if (playSoundParams.Referenced)
                    {
                        ReferencePool.Release(playSoundParams);
                    }

                    return serialId;
                }

                throw new GameFrameworkException(errorMessage);
            }

            m_SoundsBeingLoaded.Add(serialId);
            m_ResourceManager.LoadAsset(soundAssetName, null, priority, m_LoadAssetCallbacks, PlaySoundInfo.Create(serialId, soundGroup, playSoundParams, userData));
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
            if (IsLoadingSound(serialId))
            {
                m_SoundsToReleaseOnLoad.Add(serialId);
                m_SoundsBeingLoaded.Remove(serialId);
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
            foreach (int serialId in m_SoundsBeingLoaded)
            {
                m_SoundsToReleaseOnLoad.Add(serialId);
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

        private void LoadAssetSuccessCallback(string soundAssetName, object soundAsset, float duration, object userData)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)userData;
            if (playSoundInfo == null)
            {
                throw new GameFrameworkException("Play sound info is invalid.");
            }

            if (m_SoundsToReleaseOnLoad.Contains(playSoundInfo.SerialId))
            {
                m_SoundsToReleaseOnLoad.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.PlaySoundParams.Referenced)
                {
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);
                }

                ReferencePool.Release(playSoundInfo);
                m_SoundHelper.ReleaseSoundAsset(soundAsset);
                return;
            }

            m_SoundsBeingLoaded.Remove(playSoundInfo.SerialId);

            PlaySoundErrorCode? errorCode = null;
            ISoundAgent soundAgent = playSoundInfo.SoundGroup.PlaySound(playSoundInfo.SerialId, soundAsset, playSoundInfo.PlaySoundParams, out errorCode);
            if (soundAgent != null)
            {
                if (m_PlaySoundSuccessEventHandler != null)
                {
                    PlaySoundSuccessEventArgs playSoundSuccessEventArgs = PlaySoundSuccessEventArgs.Create(playSoundInfo.SerialId, soundAssetName, soundAgent, duration, playSoundInfo.UserData);
                    m_PlaySoundSuccessEventHandler(this, playSoundSuccessEventArgs);
                    ReferencePool.Release(playSoundSuccessEventArgs);
                }

                if (playSoundInfo.PlaySoundParams.Referenced)
                {
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);
                }

                ReferencePool.Release(playSoundInfo);
                return;
            }

            m_SoundsToReleaseOnLoad.Remove(playSoundInfo.SerialId);
            m_SoundHelper.ReleaseSoundAsset(soundAsset);
            string errorMessage = Utility.Text.Format("Sound group '{0}' play sound '{1}' failure.", playSoundInfo.SoundGroup.Name, soundAssetName);
            if (m_PlaySoundFailureEventHandler != null)
            {
                PlaySoundFailureEventArgs playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name, playSoundInfo.PlaySoundParams, errorCode.Value, errorMessage, playSoundInfo.UserData);
                m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                ReferencePool.Release(playSoundFailureEventArgs);

                if (playSoundInfo.PlaySoundParams.Referenced)
                {
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);
                }

                ReferencePool.Release(playSoundInfo);
                return;
            }

            if (playSoundInfo.PlaySoundParams.Referenced)
            {
                ReferencePool.Release(playSoundInfo.PlaySoundParams);
            }

            ReferencePool.Release(playSoundInfo);
            throw new GameFrameworkException(errorMessage);
        }

        private void LoadAssetFailureCallback(string soundAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)userData;
            if (playSoundInfo == null)
            {
                throw new GameFrameworkException("Play sound info is invalid.");
            }

            if (m_SoundsToReleaseOnLoad.Contains(playSoundInfo.SerialId))
            {
                m_SoundsToReleaseOnLoad.Remove(playSoundInfo.SerialId);
                if (playSoundInfo.PlaySoundParams.Referenced)
                {
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);
                }

                return;
            }

            m_SoundsBeingLoaded.Remove(playSoundInfo.SerialId);
            string appendErrorMessage = Utility.Text.Format("Load sound failure, asset name '{0}', status '{1}', error message '{2}'.", soundAssetName, status, errorMessage);
            if (m_PlaySoundFailureEventHandler != null)
            {
                PlaySoundFailureEventArgs playSoundFailureEventArgs = PlaySoundFailureEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name, playSoundInfo.PlaySoundParams, PlaySoundErrorCode.LoadAssetFailure, appendErrorMessage, playSoundInfo.UserData);
                m_PlaySoundFailureEventHandler(this, playSoundFailureEventArgs);
                ReferencePool.Release(playSoundFailureEventArgs);

                if (playSoundInfo.PlaySoundParams.Referenced)
                {
                    ReferencePool.Release(playSoundInfo.PlaySoundParams);
                }

                return;
            }

            throw new GameFrameworkException(appendErrorMessage);
        }

        private void LoadAssetUpdateCallback(string soundAssetName, float progress, object userData)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)userData;
            if (playSoundInfo == null)
            {
                throw new GameFrameworkException("Play sound info is invalid.");
            }

            if (m_PlaySoundUpdateEventHandler != null)
            {
                PlaySoundUpdateEventArgs playSoundUpdateEventArgs = PlaySoundUpdateEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name, playSoundInfo.PlaySoundParams, progress, playSoundInfo.UserData);
                m_PlaySoundUpdateEventHandler(this, playSoundUpdateEventArgs);
                ReferencePool.Release(playSoundUpdateEventArgs);
            }
        }

        private void LoadAssetDependencyAssetCallback(string soundAssetName, string dependencyAssetName, int loadedCount, int totalCount, object userData)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)userData;
            if (playSoundInfo == null)
            {
                throw new GameFrameworkException("Play sound info is invalid.");
            }

            if (m_PlaySoundDependencyAssetEventHandler != null)
            {
                PlaySoundDependencyAssetEventArgs playSoundDependencyAssetEventArgs = PlaySoundDependencyAssetEventArgs.Create(playSoundInfo.SerialId, soundAssetName, playSoundInfo.SoundGroup.Name, playSoundInfo.PlaySoundParams, dependencyAssetName, loadedCount, totalCount, playSoundInfo.UserData);
                m_PlaySoundDependencyAssetEventHandler(this, playSoundDependencyAssetEventArgs);
                ReferencePool.Release(playSoundDependencyAssetEventArgs);
            }
        }
    }
}
