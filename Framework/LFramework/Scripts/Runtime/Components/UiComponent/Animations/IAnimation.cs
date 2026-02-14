using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{

    /// <summary>
    ///     动画状态
    /// </summary>
    public enum WindowAnimationState
    {
        None,
        /// <summary>
        ///     打开
        /// </summary>
        Opening,
        /// <summary>
        ///     关闭
        /// </summary>
        Closing,
    }
  

    /// <summary>
    /// 播放完毕
    /// </summary>
    public delegate void PlayCompleted();

    /// <summary>
    /// 结束播放完毕
    /// </summary>
    public delegate void EndCompleted();
    
    /// <summary>
    /// Ui Animation
    /// </summary>
    public interface IAnimation
    {
        
        IAnimation PlayAnimation(string nodeName,PlayCompleted playCompleted);

    }

}
