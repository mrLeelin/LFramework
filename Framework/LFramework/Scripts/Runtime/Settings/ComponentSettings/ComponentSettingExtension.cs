using System;
using System.Reflection;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    public static class ComponentSettingExtension
    {
        internal static GameFrameworkComponent CovertToComponent(this ComponentSetting componentSetting)
        {
            if (string.IsNullOrEmpty(componentSetting.bindTypeName))
            {
                Log.Fatal($"The setting  '{componentSetting.GetType().FullName}' is not bind type name.");
                return null;
            }

            var componentType = Utility.Assembly.GetType(componentSetting.bindTypeName);
            if (componentType == null)
            {
                Log.Fatal(
                    $"The setting '{componentSetting.GetType().FullName}' bindTypeName '{componentSetting.bindTypeName}' is not a type.");
                return null;
            }

            if (componentType.IsAbstract ||
                componentType.IsInterface ||
                componentType.IsAssignableFrom(typeof(GameFrameworkComponent))
               )
            {
                Log.Fatal(
                    $"The setting '{componentSetting.GetType().FullName}' bindTypeName '{componentSetting.bindTypeName}' is unqualified.");
                return null;
            }

            var component = Activator.CreateInstance(componentType) as GameFrameworkComponent;
            if (component == null)
            {
                Log.Fatal(
                    $"The setting '{componentSetting.GetType().FullName}' bindTypeName '{componentSetting.bindTypeName}' is unqualified.");
                return null;
            }

            SetComponentValueFromSetting(componentSetting, componentType, component);
            return component;
        }

        private static void SetComponentValueFromSetting(ComponentSetting componentSetting, Type componentType,
            object componentInstance)
        {
            var fields = componentSetting.GetType().GetRuntimeFields();
            if (fields == null)
            {
                Log.Fatal($"The setting '{componentSetting.GetType().FullName}' fields is null. ");
                return;
            }

            foreach (var field in fields)
            {
                var componentFiled = componentType.GetField(field.Name,BindingFlags.Instance | BindingFlags.NonPublic);
                if (componentFiled == null)
                {
#if UNITY_EDITOR
                    if (field.Name.Equals("bindTypeName"))
                    {
                        continue;
                    }
                    Log.Error($"Type '{componentType.FullName}'  Field '{field.Name}'  is not in setting or component .");
#endif
                    continue;
                }

                if (componentFiled.FieldType != field.FieldType)
                {
                    continue;
                }

                componentFiled.SetValue(componentInstance, field.GetValue(componentSetting));
            }
        }
    }
}