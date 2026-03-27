#if NOTIFICATION_SUPPORT
using System;
using System.Text;
using Unity.Notifications;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Represents a notification that will be delivered for this application.
    /// </summary>
    public class GameNotification
    {
        private  Notification _internalNotification;
        private GameNotificationsComponent _component;

        /// <summary>
        /// Gets the internal notification object used by the mobile notifications system.
        /// </summary>
        public ref Notification InternalNotification => ref _internalNotification;

        /// <summary>
        /// Gets or sets a unique identifier for this notification.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If null, will be generated automatically once the notification is delivered, and then
        /// can be retrieved afterwards.
        /// </para>
        /// <para>On some platforms, this might be converted to a string identifier internally.</para>
        /// </remarks>
        /// <value>A unique integer identifier for this notification, or null (on some platforms) if not explicitly set.</value>
        public int? Id
        {
            get => _internalNotification.Identifier;
            set => _internalNotification.Identifier = value;
        }

        /// <summary>
        /// Gets or sets the notification's title.
        /// </summary>
        /// <value>The title message for the notification.</value>
        public string Title
        {
            get => _internalNotification.Title;
            set => _internalNotification.Title = value;
        }

        /// <summary>
        /// Gets or sets the body text of the notification.
        /// </summary>
        /// <value>The body message for the notification.</value>
        public string Body
        {
            get => _internalNotification.Text;
            set => _internalNotification.Text = value;
        }

        /// <summary>
        /// Gets or sets optional arbitrary data for the notification.
        /// </summary>
        public string Data
        {
            get => _internalNotification.Data;
            set => _internalNotification.Data = value;
        }

        /// <summary>
        /// Gets or sets the badge number for this notification. No badge number will be shown if null.
        /// </summary>
        /// <value>The number displayed on the app badge.</value>
        public int BadgeNumber
        {
            get => _internalNotification.Badge;
            set => _internalNotification.Badge = value;
        }

        /// <summary>
        /// Schedule this notification to be delivered at the specified time.
        /// </summary>
        /// <param name="deliveryTime"></param>
        public PendingNotification ScheduleNotification(DateTime deliveryTime)
        {
            return _component.ScheduleNotification(this, deliveryTime);
        }

        /// <summary>
        /// Instantiate a new instance of <see cref="iOSGameNotification"/>.
        /// </summary>
        internal GameNotification(GameNotificationsComponent component)
        {
            _internalNotification = new Notification
            {
                ShowInForeground = true // Deliver in foreground by default
            };
            this._component = component;
        }

        /// <summary>
        /// Instantiate a new instance of <see cref="iOSGameNotification"/> from a delivered notification.
        /// </summary>
        /// <param name="notification">The delivered notification.</param>
        /// <param name="component"></param>
        internal GameNotification(Notification notification, GameNotificationsComponent component)
        {
            this._internalNotification = notification;
            this._component = component;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Title: {Title}");
            stringBuilder.AppendLine($"Body: {Body}");
            stringBuilder.AppendLine($"Data: {Data}");
            stringBuilder.AppendLine($"Badge: {BadgeNumber}");
            stringBuilder.AppendLine($"Id: {Id}");
            return stringBuilder.ToString();
            
        }
    }
}
#endif