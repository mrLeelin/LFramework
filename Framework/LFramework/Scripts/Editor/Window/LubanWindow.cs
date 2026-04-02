using System.IO;
using Luban.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    internal sealed class GameWindowLubanOverview
    {
        private Vector2 _scrollPosition;
        private LubanExportConfig _config;
        private PropertyTree _propertyTree;

        [OnInspectorGUI]
        private void DrawPage()
        {
            EnsurePropertyTree();

            string assetPath = _config != null ? AssetDatabase.GetAssetPath(_config) : string.Empty;
            int schemaCount = _config?.config?.schema_files?.Count ?? 0;
            int targetCount = _config?.config?.targets?.Count ?? 0;
            int customArgCount = _config?.custom_args?.Count ?? 0;

            GameWindowChrome.BeginPage(ref _scrollPosition);
            GameWindowChrome.DrawHeader(
                "Luban",
                "直接在 GameWindow 内维护 Luban 导出配置、命令预览和一键导出。",
                new GameWindowBadge("Asset", string.IsNullOrEmpty(assetPath) ? "Auto Create" : Path.GetFileNameWithoutExtension(assetPath)),
                new GameWindowBadge("Target", string.IsNullOrWhiteSpace(_config?.target) ? "Unassigned" : _config.target));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);
            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard("Schema", schemaCount.ToString(), "导出配置里的 schema 文件数量。", GameWindowChrome.AccentColor),
                new GameWindowStatCard("Targets", targetCount.ToString(), "导出目标数量。", GameWindowChrome.SuccessColor),
                new GameWindowStatCard("Custom Args", customArgCount.ToString(), "自定义 -x 扩展参数数量。", GameWindowChrome.WarningColor));

            GameWindowChrome.DrawSectionHeader("Configuration", "完整的 Luban 配置和执行操作都直接放在当前页签中。");
            GameWindowChrome.BeginContentCard();
            DrawToolbar();
            GUILayout.Space(8f);

            if (_propertyTree == null)
            {
                EditorGUILayout.HelpBox("Luban 配置暂时不可用。", MessageType.Warning);
            }
            else
            {
                _propertyTree.Draw(false);
            }

            GameWindowChrome.EndContentCard();
            GameWindowChrome.EndPage();
        }

        internal void Dispose()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
            _config = null;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("预览命令", GUILayout.Height(28f)))
            {
                _config?.PreviewCommand();
            }

            if (GUILayout.Button("执行导出", GUILayout.Height(28f)))
            {
                _config?.RunCommand();
            }

            if (GUILayout.Button("选中配置资源", GUILayout.Height(28f)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void EnsurePropertyTree()
        {
            var config = LubanExportConfig.GetOrCreate();
            if (_config == config && _propertyTree != null)
            {
                return;
            }

            _propertyTree?.Dispose();
            _config = config;
            _propertyTree = _config != null ? PropertyTree.Create(new SerializedObject(_config)) : null;
        }
    }
}
