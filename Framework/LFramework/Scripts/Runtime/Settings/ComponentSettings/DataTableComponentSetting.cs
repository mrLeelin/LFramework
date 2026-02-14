

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "DataTableComponentSetting",
        menuName = "LFramework/Settings/DataTableComponentSetting")]
    public sealed class DataTableComponentSetting : ComponentSetting
    {
        [SerializeField] private string tableFullName = "";
        
        [SerializeField]
        private bool m_EnableLoadDataTableUpdateEvent = false;

        [SerializeField]
        private bool m_EnableLoadDataTableDependencyAssetEvent = false;

        [SerializeField]
        private string m_DataTableHelperTypeName = "UnityGameFramework.Runtime.DefaultDataTableHelper";

        [SerializeField]
        private DataTableHelperBase m_CustomDataTableHelper = null;

        [SerializeField]
        private int m_CachedBytesSize = 0;
    }
}