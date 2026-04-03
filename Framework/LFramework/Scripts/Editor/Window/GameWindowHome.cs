using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// 游戏框架门面 - LFramework Home Dashboard
    /// </summary>
    public class GameWindowHome
    {
        // ── 颜色常量 ──
        private static readonly Color AccentColor = new Color(0.2f, 0.68f, 0.94f);       // 蓝色主色调
        private static readonly Color CardBgLight = new Color(0.92f, 0.92f, 0.92f, 0.6f);
        private static readonly Color CardBgDark = new Color(0.22f, 0.22f, 0.22f, 0.6f);
        private static readonly Color SeparatorColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        private static readonly Color GreenBadge = new Color(0.2f, 0.72f, 0.35f);
        private static readonly Color GrayBadge = new Color(0.5f, 0.5f, 0.5f);

        // ── 模块定义 ──
        private static readonly ModuleInfo[] CoreModules = new ModuleInfo[]
        {
            new("Procedure",     "流程管理",     "管理游戏流程状态切换",       "d_UnityEditor.Timeline.TimelineWindow"),
            new("UI",            "界面系统",     "UI 生命周期与层级管理",      "d_UnityEditor.SceneHierarchyWindow"),
            new("Entity",        "实体系统",     "实体对象池与生命周期",       "d_Prefab Icon"),
            new("Sound",         "音效系统",     "音效播放与音量控制",         "d_AudioSource Icon"),
            new("Scene",         "场景管理",     "场景异步加载与切换",         "d_SceneAsset Icon"),
            new("Resource",      "资源管理",     "YooAsset 资源加载与热更",    "d_FolderOpened Icon"),
            new("Event",         "事件系统",     "全局事件派发与监听",         "d_EventSystem Icon"),
            new("Config",        "配置系统",     "游戏配置读取与管理",         "d_TextAsset Icon"),
            new("DataTable",     "数据表",       "数据表加载与查询",           "d_GridLayoutGroup Icon"),
            new("Download",      "下载管理",     "文件下载与断点续传",         "d_CloudConnect"),
            new("WebRequest",    "网络请求",     "HTTP 请求管理",              "d_NetworkAnimator Icon"),
            new("Localization",  "本地化",       "多语言切换与文本管理",       "d_Font Icon"),
        };

        private static readonly ModuleInfo[] ToolModules = new ModuleInfo[]
        {
            new("LaunchPipeline", "启动管线",    "可配置的游戏启动流程",       "d_PlayButton"),
            new("Singleton",      "单例管理",    "全局单例基类",               "d_AssemblyDefinitionAsset Icon"),
            new("ObjectPool",     "对象池",      "通用对象池管理",             "d_Package Manager"),
            new("DataNode",       "数据节点",    "树形数据存储",               "d_TreeEditor.Distribution"),
            new("FileSystem",     "文件系统",    "虚拟文件系统",               "d_DefaultAsset Icon"),
            new("Setting",        "设置存储",    "玩家偏好持久化",             "d_Preset.Context"),
        };

        // ── 样式缓存 ──
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _versionStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardDescStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _footerStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _cardTitleRichStyle;
        private GUIStyle _infoRowLabelStyle;
        private GUIStyle _infoRowValueStyle;
        private GUIStyle _statValueStyle;
        private GUIStyle _statLabelStyle;
        private GUIStyle _packageNameStyle;
        private GUIStyle _packageCatStyle;
        private GUIStyle _defineSymbolLabelStyle;
        private GUIStyle _defineSymbolStatusStyle;
        private Vector2 _scrollPos;
        private bool _stylesInitialized;

        // ── 缓存统计数据 ──
        private int _csFileCount = -1;
        private int _componentSettingCount = -1;
        private int _baseSettingCount = -1;
        private int _settingSelectorCount = -1;
        private int _sceneCount = -1;
        private int _prefabCount = -1;
        private double _lastStatsRefreshTime;
        private const double StatsRefreshInterval = 10.0; // 每10秒刷新一次

        // ── 模块数据结构 ──
        private struct ModuleInfo
        {
            public string Name;
            public string Label;
            public string Desc;
            public string IconName;

            public ModuleInfo(string name, string label, string desc, string iconName)
            {
                Name = name;
                Label = label;
                Desc = desc;
                IconName = iconName;
            }
        }

        // ── 样式初始化 ──
        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
            _titleStyle.normal.textColor = AccentColor;

            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                margin = new RectOffset(0, 0, 4, 0),
            };
            _subtitleStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.7f, 0.7f, 0.7f)
                : new Color(0.35f, 0.35f, 0.35f);

            _versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };
            _versionStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.5f, 0.5f, 0.5f)
                : new Color(0.45f, 0.45f, 0.45f);

            _cardTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
            };

            _cardDescStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                margin = new RectOffset(0, 0, 2, 0),
            };
            _cardDescStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f)
                : new Color(0.4f, 0.4f, 0.4f);

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 8, 4),
            };

            _linkStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                richText = true,
            };
            _linkStyle.normal.textColor = AccentColor;
            _linkStyle.hover.textColor = new Color(0.3f, 0.78f, 1f);

            _footerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
            };

            _actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fixedHeight = 28,
                padding = new RectOffset(10, 10, 4, 4),
            };

            _cardTitleRichStyle = new GUIStyle(_cardTitleStyle) { richText = true };

            _infoRowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
            };
            _infoRowLabelStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.65f, 0.65f, 0.65f)
                : new Color(0.35f, 0.35f, 0.35f);

            _infoRowValueStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };

            _statValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
            };
            _statValueStyle.normal.textColor = AccentColor;

            _statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleLeft,
            };
            _statLabelStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.55f, 0.55f)
                : new Color(0.4f, 0.4f, 0.4f);

            _packageNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
            };

            _packageCatStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleLeft,
            };
            _packageCatStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.55f, 0.55f)
                : new Color(0.4f, 0.4f, 0.4f);

            _defineSymbolLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };

            _defineSymbolStatusStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
        }

        // ── Odin 绘制入口 ──
        [OnInspectorGUI]
        private void DrawHomePage()
        {
            EnsureStyles();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            GUILayout.Space(12);

            DrawHeader();
            GUILayout.Space(16);
            DrawSeparator();
            GUILayout.Space(12);

            DrawQuickActions();
            GUILayout.Space(16);

            DrawProjectStats();
            GUILayout.Space(12);

            DrawBuildInfo();
            GUILayout.Space(16);

            DrawModuleSection("核心模块", CoreModules);
            GUILayout.Space(12);
            DrawModuleSection("工具模块", ToolModules);
            GUILayout.Space(16);

            DrawKeyPackages();
            GUILayout.Space(12);

            DrawEnvironmentInfo();
            GUILayout.Space(12);

            DrawLinks();
            GUILayout.Space(16);

            DrawFooter();
            GUILayout.Space(12);

            EditorGUILayout.EndScrollView();
        }

        // ── Header ──
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("LFramework", _titleStyle, GUILayout.Height(34));
            EditorGUILayout.LabelField(
                "轻量级 Unity 游戏框架  —  基于 GameFramework 扩展，集成 YooAsset 资源管理与热更新",
                _subtitleStyle);

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            DrawVersionBadge("Unity", Application.unityVersion);
            GUILayout.Space(8);
            DrawVersionBadge("YooAsset", "2.3.18");
            GUILayout.Space(8);
            DrawVersionBadge("URP", "17.3.0");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVersionBadge(string label, string version)
        {
            var content = new GUIContent($"  {label} {version}  ");
            var size = _versionStyle.CalcSize(content);
            var rect = GUILayoutUtility.GetRect(size.x + 8, 20, GUILayout.ExpandWidth(false));

            // 背景
            var bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.28f, 0.28f, 0.8f)
                : new Color(0.85f, 0.85f, 0.85f, 0.8f);
            EditorGUI.DrawRect(rect, bgColor);

            // 左侧色条
            var accentRect = new Rect(rect.x, rect.y, 3, rect.height);
            EditorGUI.DrawRect(accentRect, AccentColor);

            GUI.Label(rect, content, _versionStyle);
        }

        // ── 快捷操作 ──
        private void DrawQuickActions()
        {
            DrawSectionHeader("快捷操作");

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            if (DrawActionButton("打开持久化目录", "d_FolderOpened Icon"))
                EditorUtility.RevealInFinder(Application.persistentDataPath);

            GUILayout.Space(8);

            if (DrawActionButton("打开 StreamingAssets", "d_FolderOpened Icon"))
                EditorUtility.RevealInFinder(Application.streamingAssetsPath);

            GUILayout.Space(8);

            if (DrawActionButton("清除 PlayerPrefs", "d_TreeEditor.Trash"))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清除所有 PlayerPrefs 吗？", "确定", "取消"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("[LFramework] PlayerPrefs 已清除");
                }
            }

            GUILayout.Space(8);

            if (DrawActionButton("GC Collect", "d_Profiler.Memory"))
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                Debug.Log("[LFramework] GC.Collect & UnloadUnusedAssets 完成");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            if (DrawActionButton("Yoo -> Addressables", "d_Refresh"))
                RunMigrationAction(
                    "这会重建工具生成的 Addressable 分组，并迁移对应资源条目。是否继续？",
                    ResourceConfigMigrationHelper.ConvertYooAssetsToAddressables);

            GUILayout.Space(8);

            if (DrawActionButton("Addressables -> Yoo", "d_Refresh"))
                RunMigrationAction(
                    "这会重建目标 YooAssets Package 的采集配置。是否继续？",
                    ResourceConfigMigrationHelper.ConvertAddressablesToYooAssets);

            GUILayout.FlexibleSpace();
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawActionButton(string label, string iconName)
        {
            var icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            var content = icon != null ? new GUIContent(" " + label, icon) : new GUIContent(label);

            return GUILayout.Button(content, _actionButtonStyle, GUILayout.ExpandWidth(false));
        }

        private void RunMigrationAction(
            string confirmationMessage,
            Func<ResourceComponentSetting, ResourceConfigMigrationHelper.ResourceConfigMigrationResult> action)
        {
            var setting = AssetUtilities.GetAllAssetsOfType<ResourceComponentSetting>().FirstOrDefault();
            if (setting == null)
            {
                EditorUtility.DisplayDialog("Resource Migration", "未找到 ResourceComponentSetting。", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Resource Migration", confirmationMessage, "继续", "取消"))
            {
                return;
            }

            var result = action(setting);
            var dialogTitle = result.Success ? "迁移成功" : "迁移失败";
            var dialogBody = $"{result.Summary}\nReport: {result.ReportPath}";
            EditorUtility.DisplayDialog(dialogTitle, dialogBody, "OK");
        }

        // ── 模块卡片网格 ──
        private void DrawModuleSection(string title, ModuleInfo[] modules)
        {
            DrawSectionHeader(title);

            GUILayout.Space(4);

            const int columns = 3;
            float availableWidth = GameWindowChrome.GetDefaultWidth();
            float cardWidth = (availableWidth - (columns - 1) * 8) / columns;

            for (int i = 0; i < modules.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);

                for (int j = 0; j < columns && i + j < modules.Length; j++)
                {
                    if (j > 0) GUILayout.Space(8);
                    DrawModuleCard(modules[i + j], cardWidth);
                }

                GUILayout.Space(16);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(6);
            }
        }

        private void DrawModuleCard(ModuleInfo module, float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 52, GUILayout.Width(width));

            // 卡片背景
            var bgColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, bgColor);

            // 左侧色条
            var barRect = new Rect(rect.x, rect.y, 3, rect.height);
            EditorGUI.DrawRect(barRect, AccentColor);

            // 图标
            var icon = EditorGUIUtility.IconContent(module.IconName)?.image as Texture2D;
            if (icon != null)
            {
                var iconRect = new Rect(rect.x + 10, rect.y + 10, 16, 16);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            // 标题
            float textX = rect.x + 32;
            var titleRect = new Rect(textX, rect.y + 6, rect.width - 40, 18);
            GUI.Label(titleRect, $"{module.Name}  <color=#888888>{module.Label}</color>",
                _cardTitleRichStyle);

            // 描述
            var descRect = new Rect(textX, rect.y + 26, rect.width - 40, 20);
            GUI.Label(descRect, module.Desc, _cardDescStyle);
        }

        // ── 环境信息 ──
        private void DrawEnvironmentInfo()
        {
            DrawSectionHeader("环境信息");

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            var rect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
            rect.width -= 16;
            var bgColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, bgColor);

            float y = rect.y + 8;
            float labelW = 120;
            float valueX = rect.x + labelW + 16;

            DrawInfoRow(rect.x + 12, y, labelW, "平台", Application.platform.ToString());
            y += 18;
            DrawInfoRow(rect.x + 12, y, labelW, "公司名", Application.companyName);
            y += 18;
            DrawInfoRow(rect.x + 12, y, labelW, "产品名", Application.productName);
            y += 18;
            DrawInfoRow(rect.x + 12, y, labelW, "数据路径", Application.persistentDataPath);

            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfoRow(float x, float y, float labelWidth, string label, string value)
        {
            var labelRect = new Rect(x, y, labelWidth, 16);
            var valueRect = new Rect(x + labelWidth + 8, y, 500, 16);

            GUI.Label(labelRect, label, _infoRowLabelStyle);
            GUI.Label(valueRect, value, _infoRowValueStyle);
        }

        // ── 链接 ──
        private void DrawLinks()
        {
            DrawSectionHeader("相关资源");

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            if (DrawLinkButton("YooAsset 文档"))
                Application.OpenURL("https://yooasset.com/");

            GUILayout.Space(16);

            if (DrawLinkButton("GameFramework"))
                Application.OpenURL("https://gameframework.cn/");

            GUILayout.Space(16);

            if (DrawLinkButton("Odin Inspector"))
                Application.OpenURL("https://odininspector.com/");

            GUILayout.FlexibleSpace();
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawLinkButton(string label)
        {
            var content = new GUIContent("→ " + label);
            var rect = GUILayoutUtility.GetRect(content, _linkStyle, GUILayout.ExpandWidth(false));

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (Event.current.type == EventType.Repaint)
            {
                _linkStyle.Draw(rect, content, rect.Contains(Event.current.mousePosition), false, false, false);
            }

            return Event.current.type == EventType.MouseDown
                   && rect.Contains(Event.current.mousePosition);
        }

        // ── 项目统计 ──
        private void RefreshStatsIfNeeded()
        {
            if (EditorApplication.timeSinceStartup - _lastStatsRefreshTime < StatsRefreshInterval
                && _csFileCount >= 0)
                return;

            _lastStatsRefreshTime = EditorApplication.timeSinceStartup;

            // C# 脚本数量
            var csGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            _csFileCount = csGuids.Length;

            // ComponentSetting 数量
            _componentSettingCount = AssetUtilities.GetAllAssetsOfType<ComponentSetting>().Count();

            // BaseSetting 数量
            _baseSettingCount = AssetUtilities.GetAllAssetsOfType<BaseSetting>().Count();

            // SettingSelector 数量
            _settingSelectorCount = AssetUtilities.GetAllAssetsOfType<SettingSelector>().Count();

            // 场景数量
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            _sceneCount = sceneGuids.Length;

            // Prefab 数量
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            _prefabCount = prefabGuids.Length;
        }

        private void DrawProjectStats()
        {
            RefreshStatsIfNeeded();

            DrawSectionHeader("项目概览");
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            DrawStatCard("C# 脚本", _csFileCount.ToString(), "d_cs Script Icon");
            GUILayout.Space(8);
            DrawStatCard("场景文件", _sceneCount.ToString(), "d_SceneAsset Icon");
            GUILayout.Space(8);
            DrawStatCard("预制体", _prefabCount.ToString(), "d_Prefab Icon");
            GUILayout.Space(8);
            DrawStatCard("框架配置", _componentSettingCount.ToString(), "d_SettingsIcon");
            GUILayout.Space(8);
            DrawStatCard("游戏配置", _baseSettingCount.ToString(), "d_ScriptableObject Icon");
            GUILayout.Space(8);
            DrawStatCard("配置选择器", _settingSelectorCount.ToString(), "d_FilterByType");

            GUILayout.FlexibleSpace();
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatCard(string label, string value, string iconName)
        {
            var rect = GUILayoutUtility.GetRect(90, 56, GUILayout.Width(90));
            var bgColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, bgColor);

            // 顶部色条
            var barRect = new Rect(rect.x, rect.y, rect.width, 2);
            EditorGUI.DrawRect(barRect, AccentColor);

            // 图标
            var icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            if (icon != null)
            {
                var iconRect = new Rect(rect.x + 8, rect.y + 8, 14, 14);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            // 数值
            var valueRect = new Rect(rect.x + 26, rect.y + 4, rect.width - 34, 22);
            GUI.Label(valueRect, value, _statValueStyle);

            // 标签
            var labelRect = new Rect(rect.x + 8, rect.y + 30, rect.width - 16, 18);
            GUI.Label(labelRect, label, _statLabelStyle);
        }

        // ── 构建与脚本信息 ──
        private void DrawBuildInfo()
        {
            DrawSectionHeader("构建信息");
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            var rect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
            rect.width -= 16;
            var bgColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, bgColor);

            float y = rect.y + 8;
            float col1X = rect.x + 12;
            float col2X = rect.x + rect.width * 0.5f;
            float colW = rect.width * 0.45f;

            // 左列
            DrawInfoRow(col1X, y, 100, "构建目标", EditorUserBuildSettings.activeBuildTarget.ToString());
            y += 18;
            DrawInfoRow(col1X, y, 100, "脚本后端", PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString());
            y += 18;
            DrawInfoRow(col1X, y, 100, "API 兼容级别", PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup).ToString());

            // 右列 - Scripting Define Symbols 状态
            y = rect.y + 8;
            bool hasHybridCLR = HasScriptingDefine("HybridCLR_SUPPORT");
            bool hasYooAsset = HasScriptingDefine("YOOASSET_SUPPORT");

            DrawDefineSymbolRow(col2X, y, "HybridCLR", hasHybridCLR);
            y += 18;
            DrawDefineSymbolRow(col2X, y, "YooAsset", hasYooAsset);
            y += 18;

            bool isDev = EditorUserBuildSettings.development;
            DrawDefineSymbolRow(col2X, y, "Development Build", isDev);

            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        private bool HasScriptingDefine(string define)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out var defines);
            return defines != null && defines.Contains(define);
        }

        private void DrawDefineSymbolRow(float x, float y, string label, bool enabled)
        {
            // 状态指示点
            var dotRect = new Rect(x, y + 3, 10, 10);
            var dotColor = enabled ? GreenBadge : GrayBadge;
            EditorGUI.DrawRect(dotRect, dotColor);

            // 标签
            var labelRect = new Rect(x + 16, y, 200, 16);
            GUI.Label(labelRect, label, _defineSymbolLabelStyle);

            // 状态文字
            _defineSymbolStatusStyle.normal.textColor = enabled ? GreenBadge : GrayBadge;
            var statusRect = new Rect(x + 160, y + 1, 60, 16);
            GUI.Label(statusRect, enabled ? "已启用" : "未启用", _defineSymbolStatusStyle);
        }

        // ── 关键依赖包 ──
        private static readonly string[][] KeyPackages = new string[][]
        {
            new[] { "com.tuyoogame.yooasset", "YooAsset", "资源管理" },
            new[] { "com.code-philosophy.hybridclr", "HybridCLR", "热更新" },
            new[] { "com.cysharp.unitask", "UniTask", "异步框架" },
            new[] { "com.unity.render-pipelines.universal", "URP", "渲染管线" },
            new[] { "com.unity.addressables", "Addressables", "可寻址资源" },
            new[] { "com.unity.inputsystem", "Input System", "输入系统" },
            new[] { "com.unity.mobile.notifications", "Mobile Notifications", "本地推送" },
            new[] { "com.unity.timeline", "Timeline", "时间线" },
        };

        private void DrawKeyPackages()
        {
            DrawSectionHeader("关键依赖");
            GUILayout.Space(4);

            const int columns = 4;
            float availableWidth = GameWindowChrome.GetDefaultWidth();
            float cardWidth = (availableWidth - (columns - 1) * 8) / columns;

            for (int i = 0; i < KeyPackages.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);

                for (int j = 0; j < columns && i + j < KeyPackages.Length; j++)
                {
                    if (j > 0) GUILayout.Space(8);
                    var pkg = KeyPackages[i + j];
                    DrawPackageCard(pkg[1], pkg[2], pkg[0], cardWidth);
                }

                GUILayout.Space(16);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(6);
            }
        }

        private void DrawPackageCard(string displayName, string category, string packageId, float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 38, GUILayout.Width(width));
            var bgColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, bgColor);

            // 左侧色条
            var barRect = new Rect(rect.x, rect.y, 3, rect.height);
            EditorGUI.DrawRect(barRect, new Color(0.55f, 0.36f, 0.86f)); // 紫色区分

            // 包名
            var nameRect = new Rect(rect.x + 10, rect.y + 4, rect.width - 18, 16);
            GUI.Label(nameRect, displayName, _packageNameStyle);

            // 分类标签
            var catRect = new Rect(rect.x + 10, rect.y + 20, rect.width - 18, 14);
            GUI.Label(catRect, category, _packageCatStyle);
        }

        // ── 分隔线 ──
        private void DrawSeparator()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            rect.width -= 16;
            EditorGUI.DrawRect(rect, SeparatorColor);
            EditorGUILayout.EndHorizontal();
        }

        // ── 通用 Section Header ──
        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            _sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? Color.white
                : new Color(0.15f, 0.15f, 0.15f);
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
        }

        // ── 页脚 ──
        private void DrawFooter()
        {
            DrawSeparator();
            GUILayout.Space(6);
            EditorGUILayout.LabelField("LFramework  ·  Built with GameFramework & YooAsset", _footerStyle);
            GUILayout.Space(4);
        }
    }
}
