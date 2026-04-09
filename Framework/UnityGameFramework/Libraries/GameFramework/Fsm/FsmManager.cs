//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Fsm
{
    /// <summary>
    /// 鏈夐檺鐘舵€佹満绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class FsmManager : GameFrameworkModule, IFsmManager
    {
        private readonly Dictionary<TypeNamePair, FsmBase> m_Fsms;
        private readonly List<FsmBase> m_TempFsms;

        /// <summary>
        /// 鍒濆鍖栨湁闄愮姸鎬佹満绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public FsmManager()
        {
            m_Fsms = new Dictionary<TypeNamePair, FsmBase>();
            m_TempFsms = new List<FsmBase>();
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 鑾峰彇鏈夐檺鐘舵€佹満鏁伴噺銆?
        /// </summary>
        public int Count
        {
            get
            {
                return m_Fsms.Count;
            }
        }

        /// <summary>
        /// 鏈夐檺鐘舵€佹満绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_TempFsms.Clear();
            if (m_Fsms.Count <= 0)
            {
                return;
            }

            foreach (KeyValuePair<TypeNamePair, FsmBase> fsm in m_Fsms)
            {
                m_TempFsms.Add(fsm.Value);
            }

            foreach (FsmBase fsm in m_TempFsms)
            {
                if (fsm.IsDestroyed)
                {
                    continue;
                }

                fsm.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘湁闄愮姸鎬佹満绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<TypeNamePair, FsmBase> fsm in m_Fsms)
            {
                fsm.Value.Shutdown();
            }

            m_Fsms.Clear();
            m_TempFsms.Clear();
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <returns>鏄惁瀛樺湪鏈夐檺鐘舵€佹満銆?/returns>
        public bool HasFsm<T>() where T : class
        {
            return InternalHasFsm(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <returns>鏄惁瀛樺湪鏈夐檺鐘舵€佹満銆?/returns>
        public bool HasFsm(Type ownerType)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalHasFsm(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>鏄惁瀛樺湪鏈夐檺鐘舵€佹満銆?/returns>
        public bool HasFsm<T>(string name) where T : class
        {
            return InternalHasFsm(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>鏄惁瀛樺湪鏈夐檺鐘舵€佹満銆?/returns>
        public bool HasFsm(Type ownerType, string name)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalHasFsm(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 鑾峰彇鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <returns>瑕佽幏鍙栫殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> GetFsm<T>() where T : class
        {
            return (IFsm<T>)InternalGetFsm(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 鑾峰彇鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <returns>瑕佽幏鍙栫殑鏈夐檺鐘舵€佹満銆?/returns>
        public FsmBase GetFsm(Type ownerType)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalGetFsm(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 鑾峰彇鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>瑕佽幏鍙栫殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            return (IFsm<T>)InternalGetFsm(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 鑾峰彇鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>瑕佽幏鍙栫殑鏈夐檺鐘舵€佹満銆?/returns>
        public FsmBase GetFsm(Type ownerType, string name)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalGetFsm(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <returns>鎵€鏈夋湁闄愮姸鎬佹満銆?/returns>
        public FsmBase[] GetAllFsms()
        {
            int index = 0;
            FsmBase[] results = new FsmBase[m_Fsms.Count];
            foreach (KeyValuePair<TypeNamePair, FsmBase> fsm in m_Fsms)
            {
                results[index++] = fsm.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="results">鎵€鏈夋湁闄愮姸鎬佹満銆?/param>
        public void GetAllFsms(List<FsmBase> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeNamePair, FsmBase> fsm in m_Fsms)
            {
                results.Add(fsm.Value);
            }
        }

        /// <summary>
        /// 鍒涘缓鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="owner">鏈夐檺鐘舵€佹満鎸佹湁鑰呫€?/param>
        /// <param name="states">鏈夐檺鐘舵€佹満鐘舵€侀泦鍚堛€?/param>
        /// <returns>瑕佸垱寤虹殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }

        /// <summary>
        /// 鍒涘缓鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <param name="owner">鏈夐檺鐘舵€佹満鎸佹湁鑰呫€?/param>
        /// <param name="states">鏈夐檺鐘舵€佹満鐘舵€侀泦鍚堛€?/param>
        /// <returns>瑕佸垱寤虹殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
        {
            TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasFsm<T>(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));
            }

            Fsm<T> fsm = Fsm<T>.Create(name, owner, states);
            m_Fsms.Add(typeNamePair, fsm);
            return fsm;
        }

        /// <summary>
        /// 鍒涘缓鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="owner">鏈夐檺鐘舵€佹満鎸佹湁鑰呫€?/param>
        /// <param name="states">鏈夐檺鐘舵€佹満鐘舵€侀泦鍚堛€?/param>
        /// <returns>瑕佸垱寤虹殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> CreateFsm<T>(T owner, List<FsmState<T>> states) where T : class
        {
            return CreateFsm(string.Empty, owner, states);
        }

        /// <summary>
        /// 鍒涘缓鏈夐檺鐘舵€佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="name">鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <param name="owner">鏈夐檺鐘舵€佹満鎸佹湁鑰呫€?/param>
        /// <param name="states">鏈夐檺鐘舵€佹満鐘舵€侀泦鍚堛€?/param>
        /// <returns>瑕佸垱寤虹殑鏈夐檺鐘舵€佹満銆?/returns>
        public IFsm<T> CreateFsm<T>(string name, T owner, List<FsmState<T>> states) where T : class
        {
            TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasFsm<T>(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));
            }

            Fsm<T> fsm = Fsm<T>.Create(name, owner, states);
            m_Fsms.Add(typeNamePair, fsm);
            return fsm;
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm<T>() where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm(Type ownerType)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalDestroyFsm(new TypeNamePair(ownerType));
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="name">瑕侀攢姣佺殑鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm<T>(string name) where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="ownerType">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/param>
        /// <param name="name">瑕侀攢姣佺殑鏈夐檺鐘舵€佹満鍚嶇О銆?/param>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm(Type ownerType, string name)
        {
            if (ownerType == null)
            {
                throw new GameFrameworkException("Owner type is invalid.");
            }

            return InternalDestroyFsm(new TypeNamePair(ownerType, name));
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <typeparam name="T">鏈夐檺鐘舵€佹満鎸佹湁鑰呯被鍨嬨€?/typeparam>
        /// <param name="fsm">瑕侀攢姣佺殑鏈夐檺鐘舵€佹満銆?/param>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
        {
            if (fsm == null)
            {
                throw new GameFrameworkException("FSM is invalid.");
            }

            return InternalDestroyFsm(new TypeNamePair(typeof(T), fsm.Name));
        }

        /// <summary>
        /// 閿€姣佹湁闄愮姸鎬佹満銆?
        /// </summary>
        /// <param name="fsm">瑕侀攢姣佺殑鏈夐檺鐘舵€佹満銆?/param>
        /// <returns>鏄惁閿€姣佹湁闄愮姸鎬佹満鎴愬姛銆?/returns>
        public bool DestroyFsm(FsmBase fsm)
        {
            if (fsm == null)
            {
                throw new GameFrameworkException("FSM is invalid.");
            }

            return InternalDestroyFsm(new TypeNamePair(fsm.OwnerType, fsm.Name));
        }

        private bool InternalHasFsm(TypeNamePair typeNamePair)
        {
            return m_Fsms.ContainsKey(typeNamePair);
        }

        private FsmBase InternalGetFsm(TypeNamePair typeNamePair)
        {
            FsmBase fsm = null;
            if (m_Fsms.TryGetValue(typeNamePair, out fsm))
            {
                return fsm;
            }

            return null;
        }

        private bool InternalDestroyFsm(TypeNamePair typeNamePair)
        {
            FsmBase fsm = null;
            if (m_Fsms.TryGetValue(typeNamePair, out fsm))
            {
                fsm.Shutdown();
                return m_Fsms.Remove(typeNamePair);
            }

            return false;
        }
    }
}
