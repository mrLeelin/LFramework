

using System;
using LFramework.Runtime.Settings;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class ComponentHelper
    {

        internal static GameFrameworkComponent CreateComponent(Type type, ComponentSetting componentSetting)
        {
            if (componentSetting != null)
            {
                return componentSetting.CovertToComponent();
            }

            var instance = Activator.CreateInstance(type);
            if (instance is not GameFrameworkComponent gameFrameworkComponent)
            {
                Log.Fatal($"Type '{type.FullName}' is not GameFrameworkComponent . but you already create it.");
                return null;
            }
            return gameFrameworkComponent;
        }
        
    }
}