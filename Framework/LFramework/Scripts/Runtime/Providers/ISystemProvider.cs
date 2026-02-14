using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    /// <summary>
    /// All system provider base
    /// </summary>
    public interface ISystemProvider : IReference
    {

        /// <summary>
        /// 初始化
        /// </summary>
        void AwakeComponent();

        /// <summary>
        /// 注册事件
        /// </summary>
        void SubscribeEvent();
        /// <summary>
        /// 启动
        /// </summary>
        void SetUp();
        
        /// <summary>
        /// 注销事件
        /// </summary>
        void UnSubscribeEvent();
        /// <summary>
        /// 轮询
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        void UpdateComponent(float elapseSeconds, float realElapseSeconds){}
        
    }
}