
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
        public string bindTypeName;
    }
}