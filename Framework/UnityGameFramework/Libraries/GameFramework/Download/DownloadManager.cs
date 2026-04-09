//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Download
{
    /// <summary>
    /// 涓嬭浇绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class DownloadManager : GameFrameworkModule, IDownloadManager
    {
        private const int OneMegaBytes = 1024 * 1024;

        private readonly TaskPool<DownloadTask> m_TaskPool;
        private readonly DownloadCounter m_DownloadCounter;
        private int m_FlushSize;
        private float m_Timeout;
        private EventHandler<DownloadStartEventArgs> m_DownloadStartEventHandler;
        private EventHandler<DownloadUpdateEventArgs> m_DownloadUpdateEventHandler;
        private EventHandler<DownloadSuccessEventArgs> m_DownloadSuccessEventHandler;
        private EventHandler<DownloadFailureEventArgs> m_DownloadFailureEventHandler;

        /// <summary>
        /// 鍒濆鍖栦笅杞界鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public DownloadManager()
        {
            m_TaskPool = new TaskPool<DownloadTask>();
            m_DownloadCounter = new DownloadCounter(1f, 10f);
            m_FlushSize = OneMegaBytes;
            m_Timeout = 30f;
            m_DownloadStartEventHandler = null;
            m_DownloadUpdateEventHandler = null;
            m_DownloadSuccessEventHandler = null;
            m_DownloadFailureEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return 5;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃笅杞芥槸鍚﹁鏆傚仠銆?
        /// </summary>
        public bool Paused
        {
            get
            {
                return m_TaskPool.Paused;
            }
            set
            {
                m_TaskPool.Paused = value;
            }
        }

        /// <summary>
        /// 鑾峰彇涓嬭浇浠ｇ悊鎬绘暟閲忋€?
        /// </summary>
        public int TotalAgentCount
        {
            get
            {
                return m_TaskPool.TotalAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇鍙敤涓嬭浇浠ｇ悊鏁伴噺銆?
        /// </summary>
        public int FreeAgentCount
        {
            get
            {
                return m_TaskPool.FreeAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇宸ヤ綔涓笅杞戒唬鐞嗘暟閲忋€?
        /// </summary>
        public int WorkingAgentCount
        {
            get
            {
                return m_TaskPool.WorkingAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇绛夊緟涓嬭浇浠诲姟鏁伴噺銆?
        /// </summary>
        public int WaitingTaskCount
        {
            get
            {
                return m_TaskPool.WaitingTaskCount;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃皢缂撳啿鍖哄啓鍏ョ鐩樼殑涓寸晫澶у皬銆?
        /// </summary>
        public int FlushSize
        {
            get
            {
                return m_FlushSize;
            }
            set
            {
                m_FlushSize = value;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃笅杞借秴鏃舵椂闀匡紝浠ョ涓哄崟浣嶃€?
        /// </summary>
        public float Timeout
        {
            get
            {
                return m_Timeout;
            }
            set
            {
                m_Timeout = value;
            }
        }

        /// <summary>
        /// 鑾峰彇褰撳墠涓嬭浇閫熷害銆?
        /// </summary>
        public float CurrentSpeed
        {
            get
            {
                return m_DownloadCounter.CurrentSpeed;
            }
        }

        /// <summary>
        /// 涓嬭浇寮€濮嬩簨浠躲€?
        /// </summary>
        public event EventHandler<DownloadStartEventArgs> DownloadStart
        {
            add
            {
                m_DownloadStartEventHandler += value;
            }
            remove
            {
                m_DownloadStartEventHandler -= value;
            }
        }

        /// <summary>
        /// 涓嬭浇鏇存柊浜嬩欢銆?
        /// </summary>
        public event EventHandler<DownloadUpdateEventArgs> DownloadUpdate
        {
            add
            {
                m_DownloadUpdateEventHandler += value;
            }
            remove
            {
                m_DownloadUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 涓嬭浇鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<DownloadSuccessEventArgs> DownloadSuccess
        {
            add
            {
                m_DownloadSuccessEventHandler += value;
            }
            remove
            {
                m_DownloadSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 涓嬭浇澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<DownloadFailureEventArgs> DownloadFailure
        {
            add
            {
                m_DownloadFailureEventHandler += value;
            }
            remove
            {
                m_DownloadFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 涓嬭浇绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TaskPool.Update(elapseSeconds, realElapseSeconds);
            m_DownloadCounter.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗕笅杞界鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            m_TaskPool.Shutdown();
            m_DownloadCounter.Shutdown();
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠ｇ悊杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="downloadAgentHelper">瑕佸鍔犵殑涓嬭浇浠ｇ悊杈呭姪鍣ㄣ€?/param>
        public void AddDownloadAgentHelper(IDownloadAgentHelper downloadAgentHelper)
        {
            DownloadAgent agent = new DownloadAgent(downloadAgentHelper);
            agent.DownloadAgentStart += OnDownloadAgentStart;
            agent.DownloadAgentUpdate += OnDownloadAgentUpdate;
            agent.DownloadAgentSuccess += OnDownloadAgentSuccess;
            agent.DownloadAgentFailure += OnDownloadAgentFailure;

            m_TaskPool.AddAgent(agent);
        }

        /// <summary>
        /// 鏍规嵁涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙疯幏鍙栦笅杞戒换鍔＄殑淇℃伅銆?
        /// </summary>
        /// <param name="serialId">瑕佽幏鍙栦俊鎭殑涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/param>
        /// <returns>涓嬭浇浠诲姟鐨勪俊鎭€?/returns>
        public TaskInfo GetDownloadInfo(int serialId)
        {
            return m_TaskPool.GetTaskInfo(serialId);
        }

        /// <summary>
        /// 鏍规嵁涓嬭浇浠诲姟鐨勬爣绛捐幏鍙栦笅杞戒换鍔＄殑淇℃伅銆?
        /// </summary>
        /// <param name="tag">瑕佽幏鍙栦俊鎭殑涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>涓嬭浇浠诲姟鐨勪俊鎭€?/returns>
        public TaskInfo[] GetDownloadInfos(string tag)
        {
            return m_TaskPool.GetTaskInfos(tag);
        }

        /// <summary>
        /// 鏍规嵁涓嬭浇浠诲姟鐨勬爣绛捐幏鍙栦笅杞戒换鍔＄殑淇℃伅銆?
        /// </summary>
        /// <param name="tag">瑕佽幏鍙栦俊鎭殑涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="results">涓嬭浇浠诲姟鐨勪俊鎭€?/param>
        public void GetDownloadInfos(string tag, List<TaskInfo> results)
        {
            m_TaskPool.GetTaskInfos(tag, results);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈変笅杞戒换鍔＄殑淇℃伅銆?
        /// </summary>
        /// <returns>鎵€鏈変笅杞戒换鍔＄殑淇℃伅銆?/returns>
        public TaskInfo[] GetAllDownloadInfos()
        {
            return m_TaskPool.GetAllTaskInfos();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈変笅杞戒换鍔＄殑淇℃伅銆?
        /// </summary>
        /// <param name="results">鎵€鏈変笅杞戒换鍔＄殑淇℃伅銆?/param>
        public void GetAllDownloadInfos(List<TaskInfo> results)
        {
            m_TaskPool.GetAllTaskInfos(results);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri)
        {
            return AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="tag">涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag)
        {
            return AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="priority">涓嬭浇浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, null);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="tag">涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">涓嬭浇浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority)
        {
            return AddDownload(downloadPath, downloadUri, tag, priority, null);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="tag">涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, object userData)
        {
            return AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="priority">涓嬭浇浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, userData);
        }

        /// <summary>
        /// 澧炲姞涓嬭浇浠诲姟銆?
        /// </summary>
        /// <param name="downloadPath">涓嬭浇鍚庡瓨鏀捐矾寰勩€?/param>
        /// <param name="downloadUri">鍘熷涓嬭浇鍦板潃銆?/param>
        /// <param name="tag">涓嬭浇浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">涓嬭浇浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(downloadPath))
            {
                throw new GameFrameworkException("Download path is invalid.");
            }

            if (string.IsNullOrEmpty(downloadUri))
            {
                throw new GameFrameworkException("Download uri is invalid.");
            }

            if (TotalAgentCount <= 0)
            {
                throw new GameFrameworkException("You must add download agent first.");
            }

            DownloadTask downloadTask = DownloadTask.Create(downloadPath, downloadUri, tag, priority, m_FlushSize, m_Timeout, userData);
            m_TaskPool.AddTask(downloadTask);
            return downloadTask.SerialId;
        }

        /// <summary>
        /// 鏍规嵁涓嬭浇浠诲姟鐨勫簭鍒楃紪鍙风Щ闄や笅杞戒换鍔°€?
        /// </summary>
        /// <param name="serialId">瑕佺Щ闄や笅杞戒换鍔＄殑搴忓垪缂栧彿銆?/param>
        /// <returns>鏄惁绉婚櫎涓嬭浇浠诲姟鎴愬姛銆?/returns>
        public bool RemoveDownload(int serialId)
        {
            return m_TaskPool.RemoveTask(serialId);
        }

        /// <summary>
        /// 鏍规嵁涓嬭浇浠诲姟鐨勬爣绛剧Щ闄や笅杞戒换鍔°€?
        /// </summary>
        /// <param name="tag">瑕佺Щ闄や笅杞戒换鍔＄殑鏍囩銆?/param>
        /// <returns>绉婚櫎涓嬭浇浠诲姟鐨勬暟閲忋€?/returns>
        public int RemoveDownloads(string tag)
        {
            return m_TaskPool.RemoveTasks(tag);
        }

        /// <summary>
        /// 绉婚櫎鎵€鏈変笅杞戒换鍔°€?
        /// </summary>
        /// <returns>绉婚櫎涓嬭浇浠诲姟鐨勬暟閲忋€?/returns>
        public int RemoveAllDownloads()
        {
            return m_TaskPool.RemoveAllTasks();
        }

        private void OnDownloadAgentStart(DownloadAgent sender)
        {
            if (m_DownloadStartEventHandler != null)
            {
                DownloadStartEventArgs downloadStartEventArgs = DownloadStartEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                m_DownloadStartEventHandler(this, downloadStartEventArgs);
                ReferencePool.Release(downloadStartEventArgs);
            }
        }

        private void OnDownloadAgentUpdate(DownloadAgent sender, int deltaLength)
        {
            m_DownloadCounter.RecordDeltaLength(deltaLength);
            if (m_DownloadUpdateEventHandler != null)
            {
                DownloadUpdateEventArgs downloadUpdateEventArgs = DownloadUpdateEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                m_DownloadUpdateEventHandler(this, downloadUpdateEventArgs);
                ReferencePool.Release(downloadUpdateEventArgs);
            }
        }

        private void OnDownloadAgentSuccess(DownloadAgent sender, long length)
        {
            if (m_DownloadSuccessEventHandler != null)
            {
                DownloadSuccessEventArgs downloadSuccessEventArgs = DownloadSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                m_DownloadSuccessEventHandler(this, downloadSuccessEventArgs);
                ReferencePool.Release(downloadSuccessEventArgs);
            }
        }

        private void OnDownloadAgentFailure(DownloadAgent sender, string errorMessage)
        {
            if (m_DownloadFailureEventHandler != null)
            {
                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, errorMessage, sender.Task.UserData);
                m_DownloadFailureEventHandler(this, downloadFailureEventArgs);
                ReferencePool.Release(downloadFailureEventArgs);
            }
        }
    }
}
