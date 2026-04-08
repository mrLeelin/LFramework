
using System;
using System.Reflection;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime.Settings
{
    [System.Serializable]
    public abstract class ComponentSetting : ScriptableObject
    {
        [SerializeField] protected string settingId;
        public string bindTypeName;

        /// <summary>
        /// Setting 稳定标识
        /// </summary>
        public string SettingId => string.IsNullOrWhiteSpace(settingId) ? GetType().FullName : settingId;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下设置稳定标识
        /// </summary>
        public void EditorSetSettingId(string newSettingId)
        {
            settingId = newSettingId;
        }
#endif
    }
}
