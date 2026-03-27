#if NOTIFICATION_SUPPORT
using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    
    
    [CreateAssetMenu(order = 1, fileName = "GameNotificationsComponentSetting",
        menuName = "LFramework/Settings/GameNotificationsComponentSetting")]
    public class GameNotificationsComponentSetting : ComponentSetting
    {
       
        [SerializeField, Tooltip("The operating mode for the notifications manager.")]
        private GameNotificationsComponent.OperatingMode mode = GameNotificationsComponent.OperatingMode.QueueClearAndReschedule;

        [SerializeField, Tooltip(
             "Check to make the notifications manager automatically set badge numbers so that they increment.\n" +
             "Schedule notifications with no numbers manually set to make use of this feature.")]
        private bool autoBadging = true;
    }

}
#endif