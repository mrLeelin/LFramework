using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    public sealed class SettingMergeResult
    {
        public bool RequiresManualReview => FieldChanges.Any(change => change.action == SettingFieldChangeAction.ManualReview);
        public List<SettingFieldChange> FieldChanges { get; } = new();
    }

    /// <summary>
    /// V1 Setting 合并器：简单字段逐项比较，列表整体比较，高风险字段只提示人工处理。
    /// </summary>
    public static class SettingMergeUtility
    {
        public static SettingMergeResult Merge(ScriptableObject baseOld, ScriptableObject baseNew, ScriptableObject local)
        {
            if (baseNew == null) throw new ArgumentNullException(nameof(baseNew));
            if (local == null) throw new ArgumentNullException(nameof(local));
            if (baseOld == null)
            {
                throw new ArgumentNullException(nameof(baseOld));
            }

            var result = new SettingMergeResult();
            foreach (FieldInfo field in GetSerializableFields(baseNew.GetType()))
            {
                object oldValue = field.GetValue(baseOld);
                object newValue = field.GetValue(baseNew);
                object localValue = field.GetValue(local);

                bool templateChanged = !DeepEquals(oldValue, newValue);
                bool localChanged = !DeepEquals(oldValue, localValue);
                if (!templateChanged)
                {
                    continue;
                }

                if (!localChanged)
                {
                    field.SetValue(local, DeepClone(newValue));
                    result.FieldChanges.Add(new SettingFieldChange
                    {
                        fieldName = field.Name,
                        action = SettingFieldChangeAction.UpdatedFromTemplate
                    });
                    continue;
                }

                if (IsHighRiskField(field))
                {
                    result.FieldChanges.Add(new SettingFieldChange
                    {
                        fieldName = field.Name,
                        action = SettingFieldChangeAction.ManualReview
                    });
                    continue;
                }

                result.FieldChanges.Add(new SettingFieldChange
                {
                    fieldName = field.Name,
                    action = SettingFieldChangeAction.PreservedLocalOverride
                });
            }

            return result;
        }

        private static bool IsHighRiskField(FieldInfo field)
        {
            if (field.Name == nameof(ComponentSetting.bindTypeName))
            {
                return true;
            }

            return field.Name.IndexOf("HelperTypeName", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            while (type != null && type != typeof(ScriptableObject) && type != typeof(UnityEngine.Object))
            {
                foreach (FieldInfo field in type.GetFields(flags))
                {
                    if (field.IsStatic || field.IsNotSerialized)
                    {
                        continue;
                    }

                    bool isSerializableField = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                    if (isSerializableField)
                    {
                        yield return field;
                    }
                }

                type = type.BaseType;
            }
        }

        private static bool DeepEquals(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            Type type = left.GetType();
            if (type != right.GetType())
            {
                return false;
            }

            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                return Equals(left, right);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return Equals(left, right);
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                var leftList = (IList)left;
                var rightList = (IList)right;
                if (leftList.Count != rightList.Count)
                {
                    return false;
                }

                for (int i = 0; i < leftList.Count; i++)
                {
                    if (!DeepEquals(leftList[i], rightList[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            foreach (FieldInfo field in GetSerializableFields(type))
            {
                if (!DeepEquals(field.GetValue(left), field.GetValue(right)))
                {
                    return false;
                }
            }

            return true;
        }

        private static object DeepClone(object value)
        {
            if (value == null)
            {
                return null;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                return value;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return value;
            }

            if (type.IsArray)
            {
                Array source = (Array)value;
                Array clone = (Array)Activator.CreateInstance(type, source.Length);
                for (int i = 0; i < source.Length; i++)
                {
                    clone.SetValue(DeepClone(source.GetValue(i)), i);
                }

                return clone;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList source = (IList)value;
                IList clone = (IList)Activator.CreateInstance(type);
                foreach (object item in source)
                {
                    clone.Add(DeepClone(item));
                }

                return clone;
            }

            object instance = Activator.CreateInstance(type);
            foreach (FieldInfo field in GetSerializableFields(type))
            {
                field.SetValue(instance, DeepClone(field.GetValue(value)));
            }

            return instance;
        }
    }
}
