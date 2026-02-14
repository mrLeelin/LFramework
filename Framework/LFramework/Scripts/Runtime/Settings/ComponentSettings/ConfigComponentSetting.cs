using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "ConfigComponentSetting",
        menuName = "LFramework/Settings/ConfigComponentSetting")]
    public class ConfigComponentSetting : ComponentSetting
    {
        [SerializeField] private bool m_EnableLoadConfigUpdateEvent = false;

        [SerializeField] private bool m_EnableLoadConfigDependencyAssetEvent = false;

        [SerializeField] private string m_ConfigHelperTypeName = "UnityGameFramework.Runtime.DefaultConfigHelper";

        [SerializeField] private ConfigHelperBase m_CustomConfigHelper = null;

        [SerializeField] private int m_CachedBytesSize = 0;
    }
}

