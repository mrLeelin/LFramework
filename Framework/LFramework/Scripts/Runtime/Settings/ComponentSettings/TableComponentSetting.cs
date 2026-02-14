using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LFramework.Runtime.Settings
{
     
    [CreateAssetMenu(order = 1, fileName = "TableComponentSetting",
        menuName = "LFramework/Settings/TableComponentSetting")]
    public class TableComponentSetting : ComponentSetting
    {
        
        [SerializeField] private bool m_EnableLoadDataTableUpdateEvent = false;

        [SerializeField] private bool m_EnableLoadDataTableDependencyAssetEvent = false;

        [SerializeField] private string m_DataTableHelperTypeName = "LFramework.Runtime.DefaultTableHelper";

        [SerializeField] private TableHelperBase m_CustomDataTableHelper = null;

        [SerializeField] private int m_CachedBytesSize = 0;
        
    }
    
}

