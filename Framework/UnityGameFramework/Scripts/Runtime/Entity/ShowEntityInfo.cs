//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;

namespace UnityGameFramework.Runtime
{
    public sealed class ShowEntityInfo : IReference
    {
        private Type m_EntityLogicType;
        private object m_UserData;
        private int m_Version;
        private bool m_Released;
        private int m_DispatchDepth;
        private bool m_ReleaseRequested;
        internal int Version => m_Version;

        internal void SetDispatching(bool dispatching)
        {
            if (dispatching) m_DispatchDepth++;
            else if (--m_DispatchDepth == 0 && m_ReleaseRequested) Release(m_Version);
        }

        internal void Release(int version)
        {
            if (m_Released || m_Version != version) return;
            if (m_DispatchDepth > 0)
            {
                m_ReleaseRequested = true;
                return;
            }
            m_Released = true;
            ReferencePool.Release(this);
        }

        public ShowEntityInfo()
        {
            m_EntityLogicType = null;
            m_UserData = null;
        }

        public Type EntityLogicType
        {
            get
            {
                return m_EntityLogicType;
            }
        }

        public object UserData
        {
            get
            {
                return m_UserData;
            }
        }

        public static ShowEntityInfo Create(Type entityLogicType, object userData)
        {
            ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo.m_Version++;
            showEntityInfo.m_Released = false;
            showEntityInfo.m_DispatchDepth = 0;
            showEntityInfo.m_ReleaseRequested = false;
            showEntityInfo.m_EntityLogicType = entityLogicType;
            showEntityInfo.m_UserData = userData;
            return showEntityInfo;
        }

        public void Clear()
        {
            m_EntityLogicType = null;
            m_UserData = null;
        }
    }
}
