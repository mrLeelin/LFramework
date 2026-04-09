//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using UnityEngine.Scripting;

namespace GameFramework.Event
{
    /// <summary>
    /// 浜嬩欢绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class EventManager : GameFrameworkModule, IEventManager
    {
        private readonly EventPool<GameEventArgs> m_EventPool;
        private bool _isShutDown;

        /// <summary>
        /// 鍒濆鍖栦簨浠剁鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public EventManager()
        {
            _isShutDown = false;
            m_EventPool = new EventPool<GameEventArgs>(EventPoolMode.AllowNoHandler | EventPoolMode.AllowMultiHandler);
        }

        /// <summary>
        /// 鑾峰彇浜嬩欢澶勭悊鍑芥暟鐨勬暟閲忋€?
        /// </summary>
        public int EventHandlerCount
        {
            get { return m_EventPool.EventHandlerCount; }
        }

        /// <summary>
        /// 鑾峰彇浜嬩欢鏁伴噺銆?
        /// </summary>
        public int EventCount
        {
            get { return m_EventPool.EventCount; }
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get { return 7; }
        }

        /// <summary>
        /// 浜嬩欢绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_EventPool.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗕簨浠剁鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            m_EventPool.Shutdown();
            _isShutDown = true;
        }

        /// <summary>
        /// 鑾峰彇浜嬩欢澶勭悊鍑芥暟鐨勬暟閲忋€?
        /// </summary>
        /// <param name="id">浜嬩欢绫诲瀷缂栧彿銆?/param>
        /// <returns>浜嬩欢澶勭悊鍑芥暟鐨勬暟閲忋€?/returns>
        public int Count(int id)
        {
            return m_EventPool.Count(id);
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄤ簨浠跺鐞嗗嚱鏁般€?
        /// </summary>
        /// <param name="id">浜嬩欢绫诲瀷缂栧彿銆?/param>
        /// <param name="handler">瑕佹鏌ョ殑浜嬩欢澶勭悊鍑芥暟銆?/param>
        /// <returns>鏄惁瀛樺湪浜嬩欢澶勭悊鍑芥暟銆?/returns>
        public bool Check(int id, EventHandler<GameEventArgs> handler)
        {
            return m_EventPool.Check(id, handler);
        }

        /// <summary>
        /// 璁㈤槄浜嬩欢澶勭悊鍑芥暟銆?
        /// </summary>
        /// <param name="id">浜嬩欢绫诲瀷缂栧彿銆?/param>
        /// <param name="handler">瑕佽闃呯殑浜嬩欢澶勭悊鍑芥暟銆?/param>
        public void Subscribe(int id, EventHandler<GameEventArgs> handler)
        {
            m_EventPool.Subscribe(id, handler);
        }

        /// <summary>
        /// 鍙栨秷璁㈤槄浜嬩欢澶勭悊鍑芥暟銆?
        /// </summary>
        /// <param name="id">浜嬩欢绫诲瀷缂栧彿銆?/param>
        /// <param name="handler">瑕佸彇娑堣闃呯殑浜嬩欢澶勭悊鍑芥暟銆?/param>
        public void Unsubscribe(int id, EventHandler<GameEventArgs> handler)
        {
            //褰撴灦鏋勫叧闂殑鏃跺€欎笉闇€瑕佽繘琛岀Щ闄や簡
            //绉诲姩鍏ㄩ儴娓呯┖浜?
            if (_isShutDown)
            {
                return;
            }
            m_EventPool.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 璁剧疆榛樿浜嬩欢澶勭悊鍑芥暟銆?
        /// </summary>
        /// <param name="handler">瑕佽缃殑榛樿浜嬩欢澶勭悊鍑芥暟銆?/param>
        public void SetDefaultHandler(EventHandler<GameEventArgs> handler)
        {
            m_EventPool.SetDefaultHandler(handler);
        }

        /// <summary>
        /// 鎶涘嚭浜嬩欢锛岃繖涓搷浣滄槸绾跨▼瀹夊叏鐨勶紝鍗充娇涓嶅湪涓荤嚎绋嬩腑鎶涘嚭锛屼篃鍙繚璇佸湪涓荤嚎绋嬩腑鍥炶皟浜嬩欢澶勭悊鍑芥暟锛屼絾浜嬩欢浼氬湪鎶涘嚭鍚庣殑涓嬩竴甯у垎鍙戙€?
        /// </summary>
        /// <param name="sender">浜嬩欢婧愩€?/param>
        /// <param name="e">浜嬩欢鍙傛暟銆?/param>
        public void Fire(object sender, GameEventArgs e)
        {
            m_EventPool.Fire(sender, e);
        }

        /// <summary>
        /// 鎶涘嚭浜嬩欢绔嬪嵆妯″紡锛岃繖涓搷浣滀笉鏄嚎绋嬪畨鍏ㄧ殑锛屼簨浠朵細绔嬪埢鍒嗗彂銆?
        /// </summary>
        /// <param name="sender">浜嬩欢婧愩€?/param>
        /// <param name="e">浜嬩欢鍙傛暟銆?/param>
        public void FireNow(object sender, GameEventArgs e)
        {
            m_EventPool.FireNow(sender, e);
        }
    }
}