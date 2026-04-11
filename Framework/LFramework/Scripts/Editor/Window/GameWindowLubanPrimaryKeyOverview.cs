using System.Collections.Generic;
using System.IO;
using Luban.Editor;
using Luban.Editor.PrimaryKey;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    internal sealed class GameWindowLubanPrimaryKeyOverview
    {
        private Vector2 _scrollPosition;
        private LubanPrimaryKeyGenerateConfig _config;

        [OnInspectorGUI]
        private void DrawPage()
        {
            EnsureConfig();

            string assetPath = _config != null ? AssetDatabase.GetAssetPath(_config) : string.Empty;
            int totalRuleCount = _config?.Rules?.Count ?? 0;
            int enabledRuleCount = 0;
            string outputDirectory = string.IsNullOrWhiteSpace(_config?.OutputDir) ? "Unassigned" : _config.OutputDir;

            if (_config?.Rules != null)
            {
                foreach (var rule in _config.Rules)
                {
                    if (rule != null && rule.Enable)
                    {
                        enabledRuleCount++;
                    }
                }
            }

            GameWindowChrome.BeginPage(ref _scrollPosition);
            GameWindowChrome.DrawHeader(
                "主键映射",
                "在这里维护 Luban 表的公共输出配置和表级主键映射规则，可直接生成表名 + SerialID 常量类。",
                new GameWindowBadge("Asset", string.IsNullOrEmpty(assetPath) ? "Auto Create" : Path.GetFileNameWithoutExtension(assetPath)),
                new GameWindowBadge("Enabled", enabledRuleCount.ToString()));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);
            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard("Rules", totalRuleCount.ToString(), "配置资产里的全部主键映射规则数量。", GameWindowChrome.AccentColor),
                new GameWindowStatCard("Enabled", enabledRuleCount.ToString(), "当前会实际参与生成的规则数量。", GameWindowChrome.SuccessColor),
                new GameWindowStatCard("Output", Path.GetFileName(outputDirectory), outputDirectory, GameWindowChrome.WarningColor));

            GameWindowChrome.DrawSectionHeader("Shared Settings", "命名空间和输出目录是整份配置共享的公共设置。");
            GameWindowChrome.BeginContentCard();
            DrawSharedSettings();
            GameWindowChrome.EndContentCard();

            GameWindowChrome.DrawSectionHeader("Rules", "列表里维护表名、主键字段和多列注释字段；表名与主键字段只能通过右侧选择。");
            GameWindowChrome.BeginContentCard();
            DrawToolbar();
            GUILayout.Space(8f);
            DrawRules();
            GameWindowChrome.EndContentCard();
            GameWindowChrome.EndPage();
        }

        internal void Dispose()
        {
            _config = null;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("选中配置资源", GUILayout.Height(28f)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }

            if (GUILayout.Button("打开输出目录", GUILayout.Height(28f)))
            {
                string directory = GetPrimaryOutputDirectory();
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                    EditorUtility.RevealInFinder(Path.GetFullPath(directory));
                }
            }

            if (GUILayout.Button("直接生成主键类", GUILayout.Height(28f)))
            {
                RunPrimaryKeyGeneration();
            }

            if (GUILayout.Button("新增规则", GUILayout.Height(28f)))
            {
                Undo.RecordObject(_config, "Add Luban Primary Key Rule");
                _config.Rules ??= new List<LubanPrimaryKeyGenerateRule>();
                _config.Rules.Add(new LubanPrimaryKeyGenerateRule());
                EditorUtility.SetDirty(_config);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSharedSettings()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("主键映射配置暂时不可用。", MessageType.Warning);
                return;
            }

            SerializedObject serializedObject = new(_config);
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LubanPrimaryKeyGenerateConfig.Namespace)));
            DrawOutputDirectoryField(serializedObject.FindProperty(nameof(LubanPrimaryKeyGenerateConfig.OutputDir)));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOutputDirectoryField(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Output Dir");

            string currentValue = property.stringValue ?? string.Empty;
            string updatedValue = EditorGUILayout.TextField(currentValue);
            if (!string.Equals(updatedValue, currentValue, System.StringComparison.Ordinal))
            {
                property.stringValue = updatedValue;
            }

            if (GUILayout.Button("...", GUILayout.Width(32f)))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string initialDirectory = string.IsNullOrWhiteSpace(currentValue)
                    ? projectRoot
                    : Path.GetFullPath(currentValue);
                string selectedDirectory = EditorUtility.OpenFolderPanel("Select Output Directory", initialDirectory, string.Empty);
                if (!string.IsNullOrWhiteSpace(selectedDirectory))
                {
                    property.stringValue = ToProjectRelativePath(projectRoot, selectedDirectory);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRules()
        {
            if (_config == null)
            {
                return;
            }

            if (_config.Rules == null || _config.Rules.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有主键映射规则，点击上面的“新增规则”创建。", MessageType.Info);
                return;
            }

            for (int index = 0; index < _config.Rules.Count; index++)
            {
                LubanPrimaryKeyGenerateRule rule = _config.Rules[index] ??= new LubanPrimaryKeyGenerateRule();
                rule.CommentFields ??= new List<string>();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                rule.Enable = EditorGUILayout.ToggleLeft($"规则 {index + 1}", rule.Enable, GUILayout.Width(80f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(56f)))
                {
                    Undo.RecordObject(_config, "Remove Luban Primary Key Rule");
                    _config.Rules.RemoveAt(index);
                    EditorUtility.SetDirty(_config);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawTableNameField(rule);
                DrawPrimaryKeyField(rule);
                DrawCommentFields(rule);

                string outputClassName = string.IsNullOrWhiteSpace(rule.TableName)
                    ? "Auto"
                    : LubanPrimaryKeyClassGenerator.ResolveOutputClassName(rule);
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.TextField("Output Class", outputClassName);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void DrawTableNameField(LubanPrimaryKeyGenerateRule rule)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Table Name");
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField(rule.TableName ?? string.Empty);
            }

            if (GUILayout.Button("选表", GUILayout.Width(56f)))
            {
                string selectedWorkbookPath = SelectWorkbookPath();
                if (!string.IsNullOrWhiteSpace(selectedWorkbookPath))
                {
                    Undo.RecordObject(_config, "Select Luban Workbook");
                    rule.TableName = LubanPrimaryKeyWorkbookReader.GetTableNameFromWorkbookPath(selectedWorkbookPath);
                    rule.PrimaryKeyField = string.Empty;
                    rule.CommentFields?.Clear();
                    EditorUtility.SetDirty(_config);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrimaryKeyField(LubanPrimaryKeyGenerateRule rule)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Primary Key Field");
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField(rule.PrimaryKeyField ?? string.Empty);
            }

            if (GUILayout.Button("选列", GUILayout.Width(56f)))
            {
                ShowHeaderSelectionMenu(rule, value =>
                {
                    rule.PrimaryKeyField = value;
                    EditorUtility.SetDirty(_config);
                });
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCommentFields(LubanPrimaryKeyGenerateRule rule)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Comment Fields");
            string summary = rule.CommentFields == null || rule.CommentFields.Count == 0
                ? "未选择"
                : string.Join(", ", rule.CommentFields);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField(summary);
            }

            if (GUILayout.Button("选择列", GUILayout.Width(56f)))
            {
                ShowCommentFieldToggleMenu(rule);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowHeaderSelectionMenu(LubanPrimaryKeyGenerateRule rule, System.Action<string> setter)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.TableName))
            {
                EditorUtility.DisplayDialog("Select Column", "请先通过“选表”设置 TableName。", "OK");
                return;
            }

            IReadOnlyList<string> headers = ResolveHeaderNames(rule.TableName);
            if (headers == null || headers.Count == 0)
            {
                EditorUtility.DisplayDialog("Select Column", "未找到可用表头，请检查 TableName 和 Luban 数据目录。", "OK");
                return;
            }

            GenericMenu menu = new();
            foreach (string header in headers)
            {
                string headerName = header;
                menu.AddItem(new GUIContent(headerName), false, () =>
                {
                    Undo.RecordObject(_config, "Select Luban Column");
                    setter(headerName);
                });
            }

            menu.ShowAsContext();
        }

        private void ShowCommentFieldToggleMenu(LubanPrimaryKeyGenerateRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.TableName))
            {
                EditorUtility.DisplayDialog("Select Comment Columns", "请先通过“选表”设置 TableName。", "OK");
                return;
            }

            IReadOnlyList<string> headers = ResolveHeaderNames(rule.TableName);
            if (headers == null || headers.Count == 0)
            {
                EditorUtility.DisplayDialog("Select Comment Columns", "未找到可用表头，请检查 TableName 和 Luban 数据目录。", "OK");
                return;
            }

            rule.CommentFields ??= new List<string>();
            GenericMenu menu = new();
            foreach (string header in headers)
            {
                string headerName = header;
                bool isSelected = rule.CommentFields.Contains(headerName);
                menu.AddItem(new GUIContent(headerName), isSelected, () =>
                {
                    Undo.RecordObject(_config, "Toggle Luban Comment Field");
                    if (rule.CommentFields.Contains(headerName))
                    {
                        rule.CommentFields.Remove(headerName);
                    }
                    else
                    {
                        rule.CommentFields.Add(headerName);
                    }
                    EditorUtility.SetDirty(_config);
                });
            }

            menu.ShowAsContext();
        }

        private void RunPrimaryKeyGeneration()
        {
            try
            {
                LubanExportConfig exportConfig = LubanExportConfig.GetOrCreate();
                LubanPrimaryKeyClassGenerator.GenerateAll(exportConfig, _config);
                EditorUtility.DisplayDialog("Primary Key Mapping", "已按当前启用规则生成主键类。", "OK");
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Primary Key Mapping", exception.Message, "OK");
            }
        }

        private string SelectWorkbookPath()
        {
            string initialDirectory = GetWorkbookBrowseDirectory();
            string selectedWorkbookPath = EditorUtility.OpenFilePanel("Select Luban Workbook", initialDirectory, "xlsx");
            return string.IsNullOrWhiteSpace(selectedWorkbookPath) ? null : selectedWorkbookPath;
        }

        private IReadOnlyList<string> ResolveHeaderNames(string tableName)
        {
            try
            {
                LubanExportConfig exportConfig = LubanExportConfig.GetOrCreate();
                string workbookPath = LubanPrimaryKeyWorkbookReader.ResolveWorkbookPath(exportConfig, tableName);
                return LubanPrimaryKeyWorkbookReader.ReadHeaderNames(workbookPath);
            }
            catch
            {
                return null;
            }
        }

        private string GetWorkbookBrowseDirectory()
        {
            try
            {
                LubanExportConfig exportConfig = LubanExportConfig.GetOrCreate();
                string dataRoot = LubanPrimaryKeyWorkbookReader.ResolveDataRoot(exportConfig);
                if (Directory.Exists(dataRoot))
                {
                    return dataRoot;
                }
            }
            catch
            {
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private string GetPrimaryOutputDirectory()
        {
            if (_config == null || string.IsNullOrWhiteSpace(_config.OutputDir))
            {
                return null;
            }

            return _config.OutputDir;
        }

        private void EnsureConfig()
        {
            _config ??= LubanPrimaryKeyGenerateConfigRegister.GetOrCreate();
        }

        private static string ToProjectRelativePath(string projectRoot, string selectedDirectory)
        {
            string normalizedProjectRoot = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedSelectedDirectory = Path.GetFullPath(selectedDirectory);

            if (normalizedSelectedDirectory.StartsWith(normalizedProjectRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = normalizedSelectedDirectory.Substring(normalizedProjectRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.IsNullOrEmpty(relativePath) ? "." : relativePath.Replace('\\', '/');
            }

            return normalizedSelectedDirectory.Replace('\\', '/');
        }
    }
}
