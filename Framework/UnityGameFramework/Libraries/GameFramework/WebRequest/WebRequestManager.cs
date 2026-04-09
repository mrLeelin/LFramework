//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.WebRequest
{
    /// <summary>
    /// Web 璇锋眰绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class WebRequestManager : GameFrameworkModule, IWebRequestManager
    {
        private readonly TaskPool<WebRequestTask> m_TaskPool;
        private float m_Timeout;
        private EventHandler<WebRequestStartEventArgs> m_WebRequestStartEventHandler;
        private EventHandler<WebRequestSuccessEventArgs> m_WebRequestSuccessEventHandler;
        private EventHandler<WebRequestFailureEventArgs> m_WebRequestFailureEventHandler;

        /// <summary>
        /// 鍒濆鍖?Web 璇锋眰绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public WebRequestManager()
        {
            m_TaskPool = new TaskPool<WebRequestTask>();
            m_Timeout = 30f;
            m_WebRequestStartEventHandler = null;
            m_WebRequestSuccessEventHandler = null;
            m_WebRequestFailureEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇 Web 璇锋眰浠ｇ悊鎬绘暟閲忋€?
        /// </summary>
        public int TotalAgentCount
        {
            get
            {
                return m_TaskPool.TotalAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇鍙敤 Web 璇锋眰浠ｇ悊鏁伴噺銆?
        /// </summary>
        public int FreeAgentCount
        {
            get
            {
                return m_TaskPool.FreeAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇宸ヤ綔涓?Web 璇锋眰浠ｇ悊鏁伴噺銆?
        /// </summary>
        public int WorkingAgentCount
        {
            get
            {
                return m_TaskPool.WorkingAgentCount;
            }
        }

        /// <summary>
        /// 鑾峰彇绛夊緟 Web 璇锋眰鏁伴噺銆?
        /// </summary>
        public int WaitingTaskCount
        {
            get
            {
                return m_TaskPool.WaitingTaskCount;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃?Web 璇锋眰瓒呮椂鏃堕暱锛屼互绉掍负鍗曚綅銆?
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
        /// Web 璇锋眰寮€濮嬩簨浠躲€?
        /// </summary>
        public event EventHandler<WebRequestStartEventArgs> WebRequestStart
        {
            add
            {
                m_WebRequestStartEventHandler += value;
            }
            remove
            {
                m_WebRequestStartEventHandler -= value;
            }
        }

        /// <summary>
        /// Web 璇锋眰鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<WebRequestSuccessEventArgs> WebRequestSuccess
        {
            add
            {
                m_WebRequestSuccessEventHandler += value;
            }
            remove
            {
                m_WebRequestSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// Web 璇锋眰澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<WebRequestFailureEventArgs> WebRequestFailure
        {
            add
            {
                m_WebRequestFailureEventHandler += value;
            }
            remove
            {
                m_WebRequestFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// Web 璇锋眰绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TaskPool.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞?Web 璇锋眰绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
            m_TaskPool.Shutdown();
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠ｇ悊杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="webRequestAgentHelper">瑕佸鍔犵殑 Web 璇锋眰浠ｇ悊杈呭姪鍣ㄣ€?/param>
        public void AddWebRequestAgentHelper(IWebRequestAgentHelper webRequestAgentHelper)
        {
            WebRequestAgent agent = new WebRequestAgent(webRequestAgentHelper);
            agent.WebRequestAgentStart += OnWebRequestAgentStart;
            agent.WebRequestAgentSuccess += OnWebRequestAgentSuccess;
            agent.WebRequestAgentFailure += OnWebRequestAgentFailure;

            m_TaskPool.AddAgent(agent);
        }

        /// <summary>
        /// 鏍规嵁 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙疯幏鍙?Web 璇锋眰浠诲姟鐨勪俊鎭€?
        /// </summary>
        /// <param name="serialId">瑕佽幏鍙栦俊鎭殑 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/param>
        /// <returns>Web 璇锋眰浠诲姟鐨勪俊鎭€?/returns>
        public TaskInfo GetWebRequestInfo(int serialId)
        {
            return m_TaskPool.GetTaskInfo(serialId);
        }

        /// <summary>
        /// 鏍规嵁 Web 璇锋眰浠诲姟鐨勬爣绛捐幏鍙?Web 璇锋眰浠诲姟鐨勪俊鎭€?
        /// </summary>
        /// <param name="tag">瑕佽幏鍙栦俊鎭殑 Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>Web 璇锋眰浠诲姟鐨勪俊鎭€?/returns>
        public TaskInfo[] GetWebRequestInfos(string tag)
        {
            return m_TaskPool.GetTaskInfos(tag);
        }

        /// <summary>
        /// 鏍规嵁 Web 璇锋眰浠诲姟鐨勬爣绛捐幏鍙?Web 璇锋眰浠诲姟鐨勪俊鎭€?
        /// </summary>
        /// <param name="tag">瑕佽幏鍙栦俊鎭殑 Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="results">Web 璇锋眰浠诲姟鐨勪俊鎭€?/param>
        public void GetAllWebRequestInfos(string tag, List<TaskInfo> results)
        {
            m_TaskPool.GetTaskInfos(tag, results);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈?Web 璇锋眰浠诲姟鐨勪俊鎭€?
        /// </summary>
        /// <returns>鎵€鏈?Web 璇锋眰浠诲姟鐨勪俊鎭€?/returns>
        public TaskInfo[] GetAllWebRequestInfos()
        {
            return m_TaskPool.GetAllTaskInfos();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈?Web 璇锋眰浠诲姟鐨勪俊鎭€?
        /// </summary>
        /// <param name="results">鎵€鏈?Web 璇锋眰浠诲姟鐨勪俊鎭€?/param>
        public void GetAllWebRequestInfos(List<TaskInfo> results)
        {
            m_TaskPool.GetAllTaskInfos(results);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri)
        {
            return AddWebRequest(webRequestUri, null, null, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData)
        {
            return AddWebRequest(webRequestUri, postData, null, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, string tag)
        {
            return AddWebRequest(webRequestUri, null, tag, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, int priority)
        {
            return AddWebRequest(webRequestUri, null, null, priority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, string tag)
        {
            return AddWebRequest(webRequestUri, postData, tag, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, int priority)
        {
            return AddWebRequest(webRequestUri, postData, null, priority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, string tag, int priority)
        {
            return AddWebRequest(webRequestUri, null, tag, priority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, string tag, object userData)
        {
            return AddWebRequest(webRequestUri, null, tag, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, priority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, int priority)
        {
            return AddWebRequest(webRequestUri, postData, tag, priority, null);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, object userData)
        {
            return AddWebRequest(webRequestUri, postData, tag, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, priority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, string tag, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, tag, priority, userData);
        }

        /// <summary>
        /// 澧炲姞 Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="webRequestUri">Web 璇锋眰鍦板潃銆?/param>
        /// <param name="postData">瑕佸彂閫佺殑鏁版嵁娴併€?/param>
        /// <param name="tag">Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <param name="priority">Web 璇锋眰浠诲姟鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏂板 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/returns>
        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(webRequestUri))
            {
                throw new GameFrameworkException("Web request uri is invalid.");
            }

            if (TotalAgentCount <= 0)
            {
                throw new GameFrameworkException("You must add web request agent first.");
            }

            WebRequestTask webRequestTask = WebRequestTask.Create(webRequestUri, postData, tag, priority, m_Timeout, userData);
            m_TaskPool.AddTask(webRequestTask);
            return webRequestTask.SerialId;
        }

        /// <summary>
        /// 鏍规嵁 Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙风Щ闄?Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="serialId">瑕佺Щ闄?Web 璇锋眰浠诲姟鐨勫簭鍒楃紪鍙枫€?/param>
        /// <returns>鏄惁绉婚櫎 Web 璇锋眰浠诲姟鎴愬姛銆?/returns>
        public bool RemoveWebRequest(int serialId)
        {
            return m_TaskPool.RemoveTask(serialId);
        }

        /// <summary>
        /// 鏍规嵁 Web 璇锋眰浠诲姟鐨勬爣绛剧Щ闄?Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <param name="tag">瑕佺Щ闄?Web 璇锋眰浠诲姟鐨勬爣绛俱€?/param>
        /// <returns>绉婚櫎 Web 璇锋眰浠诲姟鐨勬暟閲忋€?/returns>
        public int RemoveWebRequests(string tag)
        {
            return m_TaskPool.RemoveTasks(tag);
        }

        /// <summary>
        /// 绉婚櫎鎵€鏈?Web 璇锋眰浠诲姟銆?
        /// </summary>
        /// <returns>绉婚櫎 Web 璇锋眰浠诲姟鐨勬暟閲忋€?/returns>
        public int RemoveAllWebRequests()
        {
            return m_TaskPool.RemoveAllTasks();
        }

        private void OnWebRequestAgentStart(WebRequestAgent sender)
        {
            if (m_WebRequestStartEventHandler != null)
            {
                WebRequestStartEventArgs webRequestStartEventArgs = WebRequestStartEventArgs.Create(sender.Task.SerialId, sender.Task.WebRequestUri, sender.Task.UserData);
                m_WebRequestStartEventHandler(this, webRequestStartEventArgs);
                ReferencePool.Release(webRequestStartEventArgs);
            }
        }

        private void OnWebRequestAgentSuccess(WebRequestAgent sender, byte[] webResponseBytes)
        {
            if (m_WebRequestSuccessEventHandler != null)
            {
                WebRequestSuccessEventArgs webRequestSuccessEventArgs = WebRequestSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.WebRequestUri, webResponseBytes, sender.Task.UserData);
                m_WebRequestSuccessEventHandler(this, webRequestSuccessEventArgs);
                ReferencePool.Release(webRequestSuccessEventArgs);
            }
        }

        private void OnWebRequestAgentFailure(WebRequestAgent sender, string errorMessage)
        {
            if (m_WebRequestFailureEventHandler != null)
            {
                WebRequestFailureEventArgs webRequestFailureEventArgs = WebRequestFailureEventArgs.Create(sender.Task.SerialId, sender.Task.WebRequestUri, errorMessage, sender.Task.UserData);
                m_WebRequestFailureEventHandler(this, webRequestFailureEventArgs);
                ReferencePool.Release(webRequestFailureEventArgs);
            }
        }
    }
}
