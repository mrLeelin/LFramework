


using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "EntityComponentSetting",
        menuName = "LFramework/Settings/EntityComponentSetting")]
    public sealed class EntityComponentSetting : ComponentSetting
    {
        [SerializeField]
        private bool m_EnableShowEntityUpdateEvent = false;

        [SerializeField]
        private bool m_EnableShowEntityDependencyAssetEvent = false;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_EntityHelperTypeName = "UnityGameFramework.Runtime.DefaultEntityHelper";

        [SerializeField]
        private EntityHelperBase m_CustomEntityHelper = null;

        [SerializeField]
        private string m_EntityGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultEntityGroupHelper";

        [SerializeField]
        private EntityGroupHelperBase m_CustomEntityGroupHelper = null;

        [SerializeField]
        private EntityComponent.EntityGroup[] m_EntityGroups = null;
        
    }
}