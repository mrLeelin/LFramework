
/**

*********************************************************************
Author:              LFramework.Runtime
CreateTime:          20:06:03

*********************************************************************
**/

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "UIComponentSetting",
        menuName = "LFramework/Settings/UIComponentSetting")]
    public sealed class UIComponentSetting : ComponentSetting
    {
        [SerializeField]
        private bool m_EnableOpenUIFormSuccessEvent = true;

        [SerializeField]
        private bool m_EnableOpenUIFormFailureEvent = true;

        [SerializeField]
        private bool m_EnableOpenUIFormUpdateEvent = false;

        [SerializeField]
        private bool m_EnableOpenUIFormDependencyAssetEvent = false;

        [SerializeField]
        private bool m_EnableCloseUIFormCompleteEvent = true;

        [SerializeField]
        private float m_InstanceAutoReleaseInterval = 60f;

        [SerializeField]
        private int m_InstanceCapacity = 16;

        [SerializeField]
        private float m_InstanceExpireTime = 60f;

        [SerializeField]
        private int m_InstancePriority = 0;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private Vector2 m_InstanceRootOffset = Vector2.zero;
        
        [SerializeField]
        private string m_UIFormHelperTypeName = "UnityGameFramework.Runtime.DefaultUIFormHelper";

        [SerializeField]
        private UIFormHelperBase m_CustomUIFormHelper = null;

        [SerializeField]
        private string m_UIGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultUIGroupHelper";

        [SerializeField]
        private UIGroupHelperBase m_CustomUIGroupHelper = null;

        [SerializeField]
        private UIComponent.UIGroup[] m_UIGroups = null;
        
      
    }
    
}