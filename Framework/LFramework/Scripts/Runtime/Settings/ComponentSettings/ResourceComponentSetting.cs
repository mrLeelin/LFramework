
using GameFramework.Resource;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "ResourceComponentSetting",
        menuName = "LFramework/Settings/ResourceComponentSetting")]
    public class ResourceComponentSetting : ComponentSetting
    {
        
        [SerializeField]
        private ResourceMode m_ResourceMode = ResourceMode.Package;

        [SerializeField]
        private ReadWritePathType m_ReadWritePathType = ReadWritePathType.Unspecified;

        [SerializeField]
        private float m_MinUnloadUnusedAssetsInterval = 60f;

        [SerializeField]
        private float m_MaxUnloadUnusedAssetsInterval = 300f;

        [SerializeField]
        private float m_AssetAutoReleaseInterval = 60f;

        [SerializeField]
        private int m_AssetCapacity = 64;

        [SerializeField]
        private float m_AssetExpireTime = 60f;

        [SerializeField]
        private int m_AssetPriority = 0;

        [SerializeField]
        private float m_ResourceAutoReleaseInterval = 60f;

        [SerializeField]
        private int m_ResourceCapacity = 16;

        [SerializeField]
        private float m_ResourceExpireTime = 60f;

        [SerializeField]
        private int m_ResourcePriority = 0;

        [SerializeField]
        private string m_UpdatePrefixUri = null;

        [SerializeField]
        private int m_GenerateReadWriteVersionListLength = 1024 * 1024;
        
        [SerializeField]
        private int m_UpdateRetryCount = 3;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_ResourceHelperTypeName = "UnityGameFramework.Runtime.DefaultResourceHelper";

        [SerializeField]
        private ResourceHelperBase m_CustomResourceHelper = null;

        [SerializeField]
        private string m_LoadResourceAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultLoadResourceAgentHelper";

        [SerializeField]
        private LoadResourceAgentHelperBase m_CustomLoadResourceAgentHelper = null;

        [SerializeField]
        private int m_LoadResourceAgentHelperCount = 3;
        
    }
}