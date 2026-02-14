using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;


namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "DebuggerComponentSetting",
        menuName = "LFramework/Settings/DebuggerComponentSetting")]
    public sealed class DebuggerComponentSetting : ComponentSetting
    {
        [SerializeField] private GUISkin m_Skin = null;

        [SerializeField] private DebuggerActiveWindowType m_ActiveWindow = DebuggerActiveWindowType.AlwaysOpen;

        [SerializeField] private bool m_ShowFullWindow = false;

        [SerializeField] private DebuggerComponent.ConsoleWindow m_ConsoleWindow;
    }
    
}