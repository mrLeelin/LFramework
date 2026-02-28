using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using LFramework.Runtime.Settings;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// SettingSelector 自定义编辑器
    /// 提供快速操作和验证功能
    /// </summary>
    [CustomEditor(typeof(SettingSelector))]
    public class SettingSelectorEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var selector = target as SettingSelector;
            if (selector == null) return;

            GUILayout.Space(10);
            SirenixEditorGUI.Title("快速操作", "", TextAlignment.Left, true);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新所有 Setting", GUILayout.Height(30)))
            {
                RefreshAllSettings(selector);
            }

            if (GUILayout.Button("验证所有 Setting", GUILayout.Height(30)))
            {
                ValidateAllSettings(selector);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("应用所有 Setting", GUILayout.Height(30)))
            {
                ApplyAllSettings(selector);
            }
        }

        /// <summary>
        /// 刷新所有 Setting，扫描项目中的 BaseSetting 资产
        /// </summary>
        private void RefreshAllSettings(SettingSelector selector)
        {
            var allSettings = AssetUtilities.GetAllAssetsOfType<BaseSetting>();

            // 按类型分组
            var settingsByType = allSettings.GroupBy(s => s.GetType().Name);

            Debug.Log($"[SettingSelectorEditor] 找到 {settingsByType.Count()} 种 Setting 类型：");

            foreach (var group in settingsByType)
            {
                Debug.Log($"  - {group.Key}: {group.Count()} 个实例");
                foreach (var setting in group)
                {
                    Debug.Log($"    • {setting.name}");
                }
            }

            EditorUtility.DisplayDialog("刷新完成",
                $"找到 {settingsByType.Count()} 种 Setting 类型，共 {allSettings.Count()} 个实例。\n详情请查看 Console。",
                "确定");
        }

        /// <summary>
        /// 验证所有选择的 Setting
        /// </summary>
        private void ValidateAllSettings(SettingSelector selector)
        {
            var allSettings = selector.GetAllSettings();
            if (allSettings.Count == 0)
            {
                EditorUtility.DisplayDialog("验证结果", "没有选择任何 Setting！", "确定");
                return;
            }

            bool allValid = true;
            int validCount = 0;
            int invalidCount = 0;

            Debug.Log("[SettingSelectorEditor] 开始验证所有 Setting...");

            foreach (var setting in allSettings)
            {
                if (setting == null)
                {
                    Debug.LogWarning("[SettingSelectorEditor] 发现空的 Setting 引用");
                    invalidCount++;
                    allValid = false;
                    continue;
                }

                if (setting.Validate(out string errorMessage))
                {
                    Debug.Log($"[SettingSelectorEditor] ✓ {setting.GetType().Name}: {setting.name}");
                    validCount++;
                }
                else
                {
                    Debug.LogError($"[SettingSelectorEditor] ✗ {setting.GetType().Name}: {setting.name}\n  错误: {errorMessage}");
                    invalidCount++;
                    allValid = false;
                }
            }

            string message = $"验证完成！\n\n" +
                           $"✓ 通过: {validCount}\n" +
                           $"✗ 失败: {invalidCount}\n\n" +
                           (allValid ? "所有 Setting 验证通过！" : "部分 Setting 验证失败，请检查 Console 查看详情。");

            EditorUtility.DisplayDialog("验证结果", message, "确定");
        }

        /// <summary>
        /// 应用所有选择的 Setting
        /// </summary>
        private void ApplyAllSettings(SettingSelector selector)
        {
            var allSettings = selector.GetAllSettings();
            if (allSettings.Count == 0)
            {
                EditorUtility.DisplayDialog("应用结果", "没有选择任何 Setting！", "确定");
                return;
            }

            Debug.Log("[SettingSelectorEditor] 开始应用所有 Setting...");

            foreach (var setting in allSettings)
            {
                if (setting == null) continue;

                setting.Apply();
                Debug.Log($"[SettingSelectorEditor] 已应用: {setting.GetType().Name} - {setting.name}");
            }

            EditorUtility.DisplayDialog("应用完成",
                $"已应用 {allSettings.Count} 个 Setting。",
                "确定");
        }
    }
}
