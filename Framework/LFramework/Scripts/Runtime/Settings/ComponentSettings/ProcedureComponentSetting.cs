


using UnityEngine;

namespace LFramework.Runtime.Settings
{
    
     
    [CreateAssetMenu(order = 1, fileName = "ProcedureComponentSetting",
        menuName = "LFramework/Settings/ProcedureComponentSetting")]
    public sealed class ProcedureComponentSetting : ComponentSetting
    {
        
        [SerializeField]
        private string[] m_AvailableProcedureTypeNames = null;
        
        
        [SerializeField]
        private string m_EntranceProcedureTypeName = null;

        [SerializeField]
        private string m_EntranceHotfixProcedureTypeName = null;
    }
}