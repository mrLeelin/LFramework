using System;
using System.Reflection;
using GameFramework;
using UnityEngine;
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
                !typeof(GameFrameworkComponent).IsAssignableFrom(componentType)
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

            var settingType = componentSetting.GetType();
            foreach (var field in fields)
            {
                // 基类字段是设置元数据，不参与运行时组件参数映射。
                if (field.DeclaringType == typeof(ComponentSetting))
                {
                    continue;
                }

                // 静态字段不属于实例配置，不参与运行时组件参数映射。
                if (field.IsStatic)
                {
                    continue;
                }

                // 仅由 Helper 通过设置资产直接读取的字段，显式忽略组件参数映射与字段缺失诊断。
                if (field.GetCustomAttribute<ComponentSettingMappingIgnoreAttribute>(true) != null)
                {
                    continue;
                }

                var componentFiled = FindComponentField(componentType, field.Name);
                if (componentFiled == null)
                {
#if UNITY_EDITOR
                    // 诊断使用 UnityEngine.Debug 输出，避免项目未定义 ENABLE_LOG 等日志宏时调用被整体编译掉，
                    // 导致真实的映射错误在不同工程配置下静默消失；该分支仅编辑器编译。
                    Debug.LogError(
                        $"[ComponentSetting] Field '{settingType.FullName}.{field.Name}' is not mapped: component '{componentType.FullName}' has no field with that name. Add [ComponentSettingMappingIgnore] to the setting field if this is intentional.");
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

        /// <summary>
        /// 在组件类型及其基类中查找同名字段，包含 public 字段和基类私有字段。
        /// </summary>
        private static FieldInfo FindComponentField(Type componentType, string fieldName)
        {
            for (var type = componentType; type != null && type != typeof(object); type = type.BaseType)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }
    }
}
