using UnityEngine;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "EditorResourceComponentSetting",
        menuName = "LFramework/Settings/EditorResourceComponentSetting")]
    public sealed class EditorResourceComponentSetting : ComponentSetting
    {
        
        [SerializeField]
        private bool m_EnableCachedAssets = true;

        [SerializeField]
        private int m_LoadAssetCountPerFrame = 1;

        [SerializeField]
        private float m_MinLoadAssetRandomDelaySeconds = 0f;

        [SerializeField]
        private float m_MaxLoadAssetRandomDelaySeconds = 0f;

    }
}