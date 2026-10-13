//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed partial class UIComponent : GameFrameworkComponent
    {
        [Serializable]
        public sealed class UIGroup
        {
            [SerializeField]
            private string m_Name = null;

            [SerializeField]
            private int m_Depth = 0;

            [SerializeField, InspectorName("不使用对象池")]
            [Tooltip("每次打开创建新界面实例，关闭后直接释放，不缓存到 UI 实例对象池。")]
            private bool m_DisableInstancePool = false;

            public bool DisableInstancePool => m_DisableInstancePool;

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            public int Depth
            {
                get
                {
                    return m_Depth;
                }
            }
        }
    }
}
