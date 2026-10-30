using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 使用 Unity 的构建选项枚举编辑运行时配置中的整数位掩码。
    /// </summary>
    [CustomPropertyDrawer(typeof(PlayerBuildSetting.BuildOptionsConfig))]
    public sealed class PlayerBuildOptionsConfigDrawer : PropertyDrawer
    {
        private const string OptionsFieldName = "buildOptions";
        private const string HelpText =
            "按平台和构建模式使用对应的 Options。Development 等选项由你自行勾选，不会自动补齐。\n" +
            "LZ4 与 LZ4HC 互斥；Android 导出选项由打包类型控制。";

        /// <summary>
        /// 在现有 IMGUI 设置页面中绘制多选枚举，保留混合值和撤销支持。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            bool previousMixedValue = EditorGUI.showMixedValue;
            try
            {
                SerializedProperty options = property.FindPropertyRelative(OptionsFieldName);
                Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.showMixedValue = options.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                var value = (BuildOptions)EditorGUI.EnumFlagsField(fieldRect,
                    new GUIContent(label.text, HelpText), (BuildOptions)options.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    options.intValue = (int)value;
                }

                EditorGUI.showMixedValue = false;
                Rect helpRect = new Rect(position.x, fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width, position.height - fieldRect.height - EditorGUIUtility.standardVerticalSpacing);
                EditorGUI.HelpBox(helpRect, HelpText, MessageType.Info);
            }
            finally
            {
                EditorGUI.showMixedValue = previousMixedValue;
                EditorGUI.EndProperty();
            }
        }

        /// <summary>
        /// 为换行说明预留空间。
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float width = Mathf.Max(180f, EditorGUIUtility.currentViewWidth - 60f - EditorGUI.indentLevel * 15f);
            float helpHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(HelpText), width);
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + helpHeight;
        }

        /// <summary>
        /// 在 UI Toolkit Inspector 中提供同一枚举控件，并同步序列化值和混合状态。
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            SerializedProperty options = property.FindPropertyRelative(OptionsFieldName);
            var field = new EnumFlagsField(property.displayName, (BuildOptions)options.intValue)
            {
                tooltip = HelpText,
                showMixedValue = options.hasMultipleDifferentValues
            };
            field.RegisterValueChangedCallback(change =>
            {
                options.serializedObject.Update();
                options.intValue = (int)(BuildOptions)change.newValue;
                options.serializedObject.ApplyModifiedProperties();
            });
            field.TrackPropertyValue(options, updated =>
            {
                field.showMixedValue = updated.hasMultipleDifferentValues;
                field.SetValueWithoutNotify((BuildOptions)updated.intValue);
            });
            root.Add(field);
            root.Add(new HelpBox(HelpText, HelpBoxMessageType.Info));
            return root;
        }
    }
}
