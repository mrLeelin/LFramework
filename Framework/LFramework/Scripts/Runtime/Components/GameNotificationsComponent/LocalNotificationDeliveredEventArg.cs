#if NOTIFICATION_SUPPORT
using GameFramework;


namespace LFramework.Runtime
{
    public class LocalNotificationDeliveredEventArg : GameEventArgs<LocalNotificationDeliveredEventArg>
    {

        public static LocalNotificationDeliveredEventArg Create(PendingNotification notification)
        {
            var arg = ReferencePool.Acquire<LocalNotificationDeliveredEventArg>();
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
#endif
