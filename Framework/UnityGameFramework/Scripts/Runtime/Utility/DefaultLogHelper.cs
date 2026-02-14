//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using GameFramework;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : GameFrameworkLog.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(GameFrameworkLogLevel level, object message)
        {
            var messageTime = AppendCurrentTime(message.ToString(), "#FEFF00");
            switch (level)
            {
                case GameFrameworkLogLevel.Debug:
                    Debug.Log(messageTime);
                    break;

                case GameFrameworkLogLevel.Info:
                    Debug.Log(messageTime);
                    break;

                case GameFrameworkLogLevel.Warning:
                    Debug.LogWarning(messageTime);
                    break;

                case GameFrameworkLogLevel.Error:
                    Debug.LogError(messageTime);
                    break;
                
                default:
                    throw new GameFrameworkException(messageTime);
            }
        }

        /// <summary>
        /// 在前面插入时间
        /// </summary>
        /// <param name="message"></param>
        private string AppendCurrentTime(string message)
        {
            return $"[{DateTime.Now:HH:mm:ss}] {message}";
        }
        /// <summary>
        /// 时间加上颜色
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private string AppendCurrentTime(string message, string color)
        {
            
#if !UNITY_EDITOR
            return AppendCurrentTime(message);
#endif
            return $"<color={color}>[{DateTime.Now:HH:mm:ss}] </color> {message}";
        }
        
        private string MessageChangeColor(string message, string color)
        {
#if !UNITY_EDITOR
            return message;
#endif
            return $"<color={color}>{message}</color>";
        }
    }
}
