using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime
{
    [CreateAssetMenu(order = 1, fileName = "SpriteCollectionComponentSetting",
        menuName = "LFramework/Settings/SpriteCollectionComponentSetting")]
    public class SpriteCollectionComponentSetting : ComponentSetting
    {
        /// <summary>
        /// 检查是否可以释放间隔
        /// </summary>
        [SerializeField] private float m_CheckCanReleaseInterval = 30f;
        
        /// <summary>
        /// 对象池自动释放时间间隔
        /// </summary>
        [SerializeField] private float m_AutoReleaseInterval = 60f;
        
    }
}

