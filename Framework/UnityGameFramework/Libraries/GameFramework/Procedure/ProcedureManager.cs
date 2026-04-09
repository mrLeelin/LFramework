//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Fsm;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Procedure
{
    /// <summary>
    /// 娴佺▼绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class ProcedureManager : GameFrameworkModule, IProcedureManager
    {
        private IFsmManager m_FsmManager;
        private IFsm<IProcedureManager> m_ProcedureFsm;

        /// <summary>
        /// 鍒濆鍖栨祦绋嬬鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public ProcedureManager()
        {
            m_FsmManager = null;
            m_ProcedureFsm = null;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get { return -2; }
        }

        /// <summary>
        /// 鑾峰彇褰撳墠娴佺▼銆?
        /// </summary>
        public ProcedureBase CurrentProcedure
        {
            get
            {
                if (m_ProcedureFsm == null)
                {
                    throw new GameFrameworkException("You must initialize procedure first.");
                }

                return (ProcedureBase)m_ProcedureFsm.CurrentState;
            }
        }

        /// <summary>
        /// 鑾峰彇褰撳墠娴佺▼鎸佺画鏃堕棿銆?
        /// </summary>
        public float CurrentProcedureTime
        {
            get
            {
                if (m_ProcedureFsm == null)
                {
                    throw new GameFrameworkException("You must initialize procedure first.");
                }

                return m_ProcedureFsm.CurrentStateTime;
            }
        }

        /// <summary>
        /// 娴佺▼绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘祦绋嬬鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            if (m_FsmManager != null)
            {
                if (m_ProcedureFsm != null)
                {
                    m_FsmManager.DestroyFsm(m_ProcedureFsm);
                    m_ProcedureFsm = null;
                }

                m_FsmManager = null;
            }
        }

        /// <summary>
        /// 鍒濆鍖栨祦绋嬬鐞嗗櫒銆?
        /// </summary>
        /// <param name="fsmManager">鏈夐檺鐘舵€佹満绠＄悊鍣ㄣ€?/param>
        /// <param name="procedures">娴佺▼绠＄悊鍣ㄥ寘鍚殑娴佺▼銆?/param>
        public void Initialize(IFsmManager fsmManager, params ProcedureBase[] procedures)
        {
            if (fsmManager == null)
            {
                throw new GameFrameworkException("FSM manager is invalid.");
            }

            m_FsmManager = fsmManager;
            m_ProcedureFsm = m_FsmManager.CreateFsm(this, procedures);
        }

        public void AddProcedure(params ProcedureBase[] procedures)
        {
            m_ProcedureFsm.AddState(procedures);
        }

        /// <summary>
        /// 寮€濮嬫祦绋嬨€?
        /// </summary>
        /// <typeparam name="T">瑕佸紑濮嬬殑娴佺▼绫诲瀷銆?/typeparam>
        public void StartProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            m_ProcedureFsm.Start<T>();
        }


        /// <summary>
        /// 寮€濮嬫祦绋嬨€?
        /// </summary>
        /// <param name="procedureType">瑕佸紑濮嬬殑娴佺▼绫诲瀷銆?/param>
        public void StartProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            m_ProcedureFsm.Start(procedureType);
        }

        /// <summary>
        /// 鏄惁瀛樺湪娴佺▼銆?
        /// </summary>
        /// <typeparam name="T">瑕佹鏌ョ殑娴佺▼绫诲瀷銆?/typeparam>
        /// <returns>鏄惁瀛樺湪娴佺▼銆?/returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return m_ProcedureFsm.HasState<T>();
        }

        /// <summary>
        /// 鏄惁瀛樺湪娴佺▼銆?
        /// </summary>
        /// <param name="procedureType">瑕佹鏌ョ殑娴佺▼绫诲瀷銆?/param>
        /// <returns>鏄惁瀛樺湪娴佺▼銆?/returns>
        public bool HasProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return m_ProcedureFsm.HasState(procedureType);
        }

        /// <summary>
        /// 鑾峰彇娴佺▼銆?
        /// </summary>
        /// <typeparam name="T">瑕佽幏鍙栫殑娴佺▼绫诲瀷銆?/typeparam>
        /// <returns>瑕佽幏鍙栫殑娴佺▼銆?/returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return m_ProcedureFsm.GetState<T>();
        }

        /// <summary>
        /// 鑾峰彇娴佺▼銆?
        /// </summary>
        /// <param name="procedureType">瑕佽幏鍙栫殑娴佺▼绫诲瀷銆?/param>
        /// <returns>瑕佽幏鍙栫殑娴佺▼銆?/returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return (ProcedureBase)m_ProcedureFsm.GetState(procedureType);
        }

        public void ForceChangedProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            m_ProcedureFsm.ChangeState<T>();
        }

        public void ForceChangedProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            m_ProcedureFsm.ChangeState(procedureType);
        }
    }
}