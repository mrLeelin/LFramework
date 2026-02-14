
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    
    
    [CreateAssetMenu(order = 1, fileName = "SceneComponentSetting",
        menuName = "LFramework/Settings/SceneComponentSetting")]
    public sealed class SceneComponentSetting : ComponentSetting
    {
        [SerializeField]
        private bool m_EnableLoadSceneUpdateEvent = true;

        [SerializeField]
        private bool m_EnableLoadSceneDependencyAssetEvent = true;
    }
}