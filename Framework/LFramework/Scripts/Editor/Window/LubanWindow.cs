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

        /// <summary>
        /// 框架内置 Luban 模板的源目录（Unity 资产路径）。
        /// </summary>
        private const string TemplateSrcDir = "Assets/Framework/Framework/LFramework/Assets/Template/Luban";

        /// <summary>
        /// 模板文件名列表。
        /// </summary>
        private static readonly string[] TemplateFileNames = { "table.sbn", "tables.sbn" };

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
            DrawPrimaryAction();
            GUILayout.Space(4f);
            DrawSecondaryToolbar();
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

        private void DrawSecondaryToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("预览命令", GUILayout.Height(28f)))
            {
                _config?.PreviewCommand();
            }

            if (GUILayout.Button("选中配置资源", GUILayout.Height(28f)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }

            if (GUILayout.Button("Copy Cs_Bin 模板文件", GUILayout.Height(28f)))
            {
                CopyCsBinTemplates();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrimaryAction()
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = GameWindowChrome.SuccessColor;

            if (GUILayout.Button("执行导出", GUILayout.Height(36f), GUILayout.ExpandWidth(true)))
            {
                _config?.RunCommand();
            }

            GUI.backgroundColor = previousBg;
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

        /// <summary>
        /// 将内置 cs-bin 模板文件复制到 Luban DLL 同级的 Templates/cs-bin/ 目录。
        /// </summary>
        private void CopyCsBinTemplates()
        {
            // 1. 读取 luban_dll 路径
            string lubanDll = _config?.luban_dll;
            if (string.IsNullOrEmpty(lubanDll))
            {
                EditorUtility.DisplayDialog("错误", "请先配置 luban dll 路径", "确定");
                return;
            }

            // 2. 计算目标目录
            string targetDir = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(lubanDll), "..", "Templates", "cs-bin"));

            // 3. 检查所有源文件是否存在
            var missingFiles = new System.Collections.Generic.List<string>();
            foreach (string fileName in TemplateFileNames)
            {
                string srcPath = Path.Combine(TemplateSrcDir, fileName);
                if (!File.Exists(srcPath))
                {
                    missingFiles.Add(fileName);
                }
            }

            if (missingFiles.Count > 0)
            {
                EditorUtility.DisplayDialog("错误",
                    $"模板源文件缺失:\n{string.Join("\n", missingFiles)}",
                    "确定");
                return;
            }

            // 4. 创建目标目录（幂等）
            Directory.CreateDirectory(targetDir);

            // 5. 复制文件
            try
            {
                foreach (string fileName in TemplateFileNames)
                {
                    string src = Path.Combine(TemplateSrcDir, fileName);
                    string dst = Path.Combine(targetDir, fileName);
                    File.Copy(src, dst, true);
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误",
                    $"复制模板文件时发生异常:\n{ex.Message}",
                    "确定");
                return;
            }

            // 6. 成功提示
            EditorUtility.DisplayDialog("成功",
                $"模板文件已复制到:\n{targetDir}",
                "确定");
        }
    }
}
