using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.UI;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Runtime
{

    public class WindowAnimationExitStartArg : GameEventArgs<WindowAnimationExitStartArg>
    {
        public WindowAnimationExitStartArg()
        {
            UIForm = null;
            UserData = null;
        }
        
        public IUIForm UIForm { get; private set; }
        
        public object UserData { get; private set; }
        
        public static WindowAnimationExitStartArg Create(IUIForm uiForm, object userData)
        {
            WindowAnimationExitStartArg openUIFormSuccessEventArgs =
                ReferencePool.Acquire<WindowAnimationExitStartArg>();
            openUIFormSuccessEventArgs.UIForm = uiForm;
            openUIFormSuccessEventArgs.UserData = userData;
            return openUIFormSuccessEventArgs;
        }
        
        public override void Clear()
        {
            UIForm = null;
            UserData = null;
        }
    }
    public class WindowAnimationEnterCompletedArg : GameEventArgs<WindowAnimationEnterCompletedArg>
    {
        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public WindowAnimationEnterCompletedArg()
        {
            UIForm = null;
            UserData = null;
        }

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public IUIForm UIForm { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="uiForm">加载成功的界面。</param>
        /// <param name="duration">加载持续时间。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static WindowAnimationEnterCompletedArg Create(IUIForm uiForm, object userData)
        {
            WindowAnimationEnterCompletedArg openUIFormSuccessEventArgs =
                ReferencePool.Acquire<WindowAnimationEnterCompletedArg>();
            openUIFormSuccessEventArgs.UIForm = uiForm;
            openUIFormSuccessEventArgs.UserData = userData;
            return openUIFormSuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            UIForm = null;
            UserData = null;
        }
    }
}