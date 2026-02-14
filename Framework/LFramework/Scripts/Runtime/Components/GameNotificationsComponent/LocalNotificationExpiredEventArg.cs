using System.Collections;
using System.Collections.Generic;
using GameFramework;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Runtime
{
    public class LocalNotificationExpiredEventArg : GameEventArgs<LocalNotificationExpiredEventArg>
    {
        
        public static LocalNotificationExpiredEventArg Create(PendingNotification notification)
        {
            var arg = ReferencePool.Acquire<LocalNotificationExpiredEventArg>();
            arg.PendingNotification = notification;
            return arg;
        }
        
        public PendingNotification PendingNotification { get; private set; }
        
        public override void Clear()
        {
            PendingNotification = null;
        }
    }
}

