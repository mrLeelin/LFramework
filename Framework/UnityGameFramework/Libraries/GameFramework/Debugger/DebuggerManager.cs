//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine.Scripting;

namespace GameFramework.Debugger
{
    /// <summary>
    /// 璋冭瘯鍣ㄧ鐞嗗櫒銆?
    /// </summary>
    [Preserve]
    internal sealed partial class DebuggerManager : GameFrameworkModule, IDebuggerManager
    {
        private readonly DebuggerWindowGroup m_DebuggerWindowRoot;
        private bool m_ActiveWindow;

        /// <summary>
        /// 鍒濆鍖栬皟璇曞櫒绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public DebuggerManager()
        {
            m_DebuggerWindowRoot = new DebuggerWindowGroup();
            m_ActiveWindow = false;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃皟璇曞櫒绐楀彛鏄惁婵€娲汇€?
        /// </summary>
        public bool ActiveWindow
        {
            get
            {
                return m_ActiveWindow;
            }
            set
            {
                m_ActiveWindow = value;
            }
        }

        /// <summary>
        /// 璋冭瘯鍣ㄧ獥鍙ｆ牴缁撶偣銆?
        /// </summary>
        public IDebuggerWindowGroup DebuggerWindowRoot
        {
            get
            {
                return m_DebuggerWindowRoot;
            }
        }

        /// <summary>
        /// 璋冭瘯鍣ㄧ鐞嗗櫒杞銆?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!m_ActiveWindow)
            {
                return;
            }

            m_DebuggerWindowRoot.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗚皟璇曞櫒绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
            m_ActiveWindow = false;
            m_DebuggerWindowRoot.Shutdown();
        }

        /// <summary>
        /// 娉ㄥ唽璋冭瘯鍣ㄧ獥鍙ｃ€?
        /// </summary>
        /// <param name="path">璋冭瘯鍣ㄧ獥鍙ｈ矾寰勩€?/param>
        /// <param name="debuggerWindow">瑕佹敞鍐岀殑璋冭瘯鍣ㄧ獥鍙ｃ€?/param>
        /// <param name="args">鍒濆鍖栬皟璇曞櫒绐楀彛鍙傛暟銆?/param>
        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new GameFrameworkException("Path is invalid.");
            }

            if (debuggerWindow == null)
            {
                throw new GameFrameworkException("Debugger window is invalid.");
            }

            m_DebuggerWindowRoot.RegisterDebuggerWindow(path, debuggerWindow);
            debuggerWindow.Initialize(args);
        }

        /// <summary>
        /// 瑙ｉ櫎娉ㄥ唽璋冭瘯鍣ㄧ獥鍙ｃ€?
        /// </summary>
        /// <param name="path">璋冭瘯鍣ㄧ獥鍙ｈ矾寰勩€?/param>
        /// <returns>鏄惁瑙ｉ櫎娉ㄥ唽璋冭瘯鍣ㄧ獥鍙ｆ垚鍔熴€?/returns>
        public bool UnregisterDebuggerWindow(string path)
        {
            return m_DebuggerWindowRoot.UnregisterDebuggerWindow(path);
        }

        /// <summary>
        /// 鑾峰彇璋冭瘯鍣ㄧ獥鍙ｃ€?
        /// </summary>
        /// <param name="path">璋冭瘯鍣ㄧ獥鍙ｈ矾寰勩€?/param>
        /// <returns>瑕佽幏鍙栫殑璋冭瘯鍣ㄧ獥鍙ｃ€?/returns>
        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return m_DebuggerWindowRoot.GetDebuggerWindow(path);
        }

        /// <summary>
        /// 閫変腑璋冭瘯鍣ㄧ獥鍙ｃ€?
        /// </summary>
        /// <param name="path">璋冭瘯鍣ㄧ獥鍙ｈ矾寰勩€?/param>
        /// <returns>鏄惁鎴愬姛閫変腑璋冭瘯鍣ㄧ獥鍙ｃ€?/returns>
        public bool SelectDebuggerWindow(string path)
        {
            return m_DebuggerWindowRoot.SelectDebuggerWindow(path);
        }
    }
}
