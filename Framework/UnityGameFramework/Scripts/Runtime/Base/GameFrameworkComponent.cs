//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 游戏框架组件抽象类。
    /// </summary>
    public abstract class GameFrameworkComponent //: MonoBehaviour
    {
        /// <summary>
        /// Instance game object
        /// </summary>
        protected Transform Instance { get; private set; }
        
        public GameObject Parent { get; set; }
        
        
        public virtual void AwakeComponent(){}
        
        /// <summary>
        /// StartComponent
        /// </summary>
        public virtual void StartComponent()
        {
        }

        /// <summary>
        /// Set up component
        /// </summary>
        public virtual void SetUpComponent()
        {
        }

        /// <summary>
        /// 轮询
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        public virtual void UpdateComponent(float elapseSeconds, float realElapseSeconds){}
        
        
        public virtual void LateUpdate(){}
        /// <summary>
        /// ShutDown
        /// </summary>
        public virtual void ShutDown(){}
        

        protected void CreateInstance(string name = "")
        {
            Instance = new GameObject($"[{(string.IsNullOrEmpty(name) ? GetType().Name : name)}]").transform;
            Instance.SetParent(Parent.transform);
        }

        public virtual void RuntimeOnApplicationFocus(bool hasFocus)
        {
            
        }

        public virtual void RuntimeOnApplicationPause(bool pauseStatus)
        {
            
        }
    }
}