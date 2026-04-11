using System;
using System.Collections.Generic;
using System.IO;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Window
{
    public class GameWindow : OdinMenuEditorWindow
    {
        public Action<OdinMenuTree> BuildMenuTreeAction;

        private static List<IGameWindowExtend> _gameWindowExtends;

        private GameWindowHome _gameWindowHome;
        private GameWindowLocalResourceServer _gameWindowLocalResourceServer;
        private GameWindowFrameworkSettingOverview _gameWindowFrameworkSettingOverview;
        private GameWindowFrameworkProfiledOverview _gameWindowFrameworkProfiledOverview;
        private GameWindowLubanOverview _gameWindowLubanOverview;
        private GameWindowLubanPrimaryKeyOverview _gameWindowLubanPrimaryKeyOverview;
        private List<ProfiledBase> _allProfiled;
        private UnityEditor.Editor _cachedSettingEditor;
        private UnityEngine.Object _cachedSettingTarget;
        private Vector2 _frameworkSettingScrollPosition;
        private Vector2 _frameworkProfiledScrollPosition;
        private const string SettingEditorFocusControlName = "LFramework.GameWindow.SettingEditorHost";

        [MenuItem("LFramework/GameSetting")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _gameWindowHome = new GameWindowHome();
            _gameWindowLocalResourceServer = new GameWindowLocalResourceServer();

            if (_allProfiled == null)
            {
                _allProfiled = new List<ProfiledBase>();
                var profiledBaseTypes = Type.GetRuntimeOrEditorTypes(typeof(ProfiledBase));
                foreach (var type in profiledBaseTypes)
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    try
                    {
                        if (Activator.CreateInstance(type) is ProfiledBase instance)
                        {
                            _allProfiled.Add(instance);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"Failed to create ProfiledBase instance: {type.Name}, Error: {exception.Message}");
                    }
                }
            }

            _gameWindowFrameworkSettingOverview ??= new GameWindowFrameworkSettingOverview();
            _gameWindowFrameworkProfiledOverview ??=
                new GameWindowFrameworkProfiledOverview(() => _allProfiled?.Count ?? 0);
            _gameWindowLubanOverview ??= new GameWindowLubanOverview();
            _gameWindowLubanPrimaryKeyOverview ??= new GameWindowLubanPrimaryKeyOverview();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_cachedSettingEditor != null)
            {
                DestroyImmediate(_cachedSettingEditor);
                _cachedSettingEditor = null;
            }

            _cachedSettingTarget = null;
            _gameWindowLubanOverview?.Dispose();
            _gameWindowLubanPrimaryKeyOverview?.Dispose();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect: true,OdinMenuStyle.TreeViewStyle)
            {
                { "Home", _gameWindowHome, EditorIcons.House },

              
                { "框架设置", _gameWindowFrameworkSettingOverview, EditorIcons.SettingsCog },
                { "运行时预览", _gameWindowFrameworkProfiledOverview, EditorIcons.Car },
                
#if LUBAN_SUPPORT
                  { "Luban", _gameWindowLubanOverview, EditorIcons.SettingsCog },
                  { "Luban/主键映射", _gameWindowLubanPrimaryKeyOverview, EditorIcons.SettingsCog },
#endif
                
                { "本地Cdn测试服务", _gameWindowLocalResourceServer, EditorIcons.SettingsCog },
            };

            AddAllAssetsAtType<ComponentSetting>(tree, "框架设置")
                .AddIcons(EditorIcons.SettingsCog);

            if (_allProfiled != null)
            {
                foreach (var profiled in _allProfiled)
                {
                    if (profiled == null)
                    {
                        continue;
                    }

                    var name = profiled.GetType().Name;
                    if (name.EndsWith("Profiled", StringComparison.Ordinal))
                    {
                        name = name.Substring(0, name.Length - "Profiled".Length);
                    }

                    tree.AddObjectAtPath("运行时预览/" + name, profiled);
                }
            }

            AddAllAssetsAtType<ProjectSettingSelector>(tree, "Game Setting/Project Setting Selector")
                .AddIcons(EditorIcons.SettingsCog);

            AddAllAssetsAtType<BaseSetting>(tree, "Game Setting/GameSettings")
                .AddIcons(EditorIcons.SettingsCog);

            tree.Add("构建", null, EditorIcons.Airplane);
            tree.AddObjectAtPath("构建/Build Resources", new BuildResourcesData()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("构建/Build App", new BuildPackageWindow()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("构建/Upload Version Files", new BuildVersionWindow()).AddIcon(EditorIcons.Car);
            tree.AddObjectAtPath("通用/OpenFolder", new OpenFolderInspector()).AddIcon(EditorIcons.ShoppingCart);
            AddAllExtendItems("扩展", tree);
            return tree;
        }

        protected override void DrawEditors()
        {
            var selected = MenuTree?.Selection?.SelectedValue;
            if (selected is ProfiledBase profiledBase)
            {
                DrawProfiledPage(profiledBase);
                return;
            }

            if (GameWindowSettingPageSupport.TryCreate(selected as UnityEngine.Object, out var settingPageModel))
            {
                DrawSettingAssetPage(settingPageModel);
                return;
            }

            base.DrawEditors();
        }

        private void DrawSettingAssetPage(GameWindowSettingPageModel pageModel)
        {
            EnsureSettingEditor(pageModel.Target);

            string assetPath = AssetDatabase.GetAssetPath(pageModel.Target);

            GameWindowChrome.BeginPage(ref _frameworkSettingScrollPosition);
            GameWindowChrome.DrawHeader(
                pageModel.Title,
                pageModel.Subtitle,
                new[]
                {
                    new GameWindowBadge("Asset", Path.GetFileNameWithoutExtension(assetPath)),
                    new GameWindowBadge("Mode", EditorApplication.isPlaying ? "Live Preview" : "Asset Edit")
                },
                badgeIndex => GameWindowFrameworkSettingActions.HandleAssetBadgeClick(pageModel.Target, badgeIndex));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);
            DrawSettingStatCards(pageModel.Target, assetPath);
            if (pageModel.Target is ProjectSettingSelector projectSettingSelector)
            {
                DrawProjectSettingSelectorActions(projectSettingSelector);
            }

            GameWindowChrome.DrawSectionHeader("Configuration", "The original inspector content is rendered below inside the unified shell.");
            DrawEmbeddedSettingInspector();
            GameWindowChrome.EndPage();
        }

        private void DrawProjectSettingSelectorActions(ProjectSettingSelector selector)
        {
            GameWindowChrome.DrawSectionHeader(
                "Project Setting Selector Actions",
                "Collect all project-owned settings and validate the selector references with one click.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            if (GUILayout.Button("Collect All Settings", GUILayout.Height(28f)))
            {
                GameWindowProjectSettingSelectorActions.CollectAllSettings(selector);
                EditorUtility.DisplayDialog(
                    "Collect All Settings",
                    GameWindowProjectSettingSelectorActions.BuildCollectionSummary(selector),
                    "OK");
                Repaint();
            }

            if (GUILayout.Button("Validate Settings", GUILayout.Height(28f)))
            {
                SettingValidationReport report = GameWindowProjectSettingSelectorActions.ValidateAllSettings(selector);
                EditorUtility.DisplayDialog(
                    "Validate Settings",
                    GameWindowProjectSettingSelectorActions.BuildValidationSummary(report),
                    "OK");
                Repaint();
            }

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawEmbeddedSettingInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.BeginVertical();

            Rect hostRect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.Height(0f));
            HandleEmbeddedInspectorFocus(hostRect);

            if (_cachedSettingEditor != null)
            {
                GUI.SetNextControlName(SettingEditorFocusControlName);
                _cachedSettingEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Missing CustomEditor for the selected setting asset.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void HandleEmbeddedInspectorFocus(Rect hostRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown)
            {
                return;
            }

            Rect expandedRect = hostRect;
            expandedRect.height = Mathf.Max(position.height, 1f);

            if (!expandedRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            EditorGUI.FocusTextInControl(SettingEditorFocusControlName);
            Repaint();
        }

        private void DrawSettingStatCards(UnityEngine.Object settingTarget, string assetPath)
        {
            if (settingTarget is ComponentSetting componentSetting)
            {
                string bindTypeDisplay = GameWindowChrome.GetShortTypeName(componentSetting.bindTypeName);
                GameWindowChrome.DrawStatCards(
                    new GameWindowStatCard(
                        "Bind Type",
                        bindTypeDisplay,
                        string.IsNullOrEmpty(componentSetting.bindTypeName) ? "No runtime component type assigned yet." : componentSetting.bindTypeName,
                        GameWindowChrome.AccentColor),
                    new GameWindowStatCard(
                        "Inspector",
                        _cachedSettingEditor != null ? _cachedSettingEditor.GetType().Name : "Missing Editor",
                        "Keeps the existing custom inspector fields and behavior.",
                        GameWindowChrome.SuccessColor),
                    new GameWindowStatCard(
                        "Location",
                        string.IsNullOrEmpty(assetPath) ? "Unknown" : Path.GetFileName(assetPath),
                        GameWindowChrome.GetAssetDirectory(assetPath),
                        GameWindowChrome.WarningColor));
                return;
            }

            if (settingTarget is ProjectSettingSelector projectSettingSelector)
            {
                GameWindowChrome.DrawStatCards(
                    new GameWindowStatCard(
                        "Base Settings",
                        projectSettingSelector.GetAllSettings().Count.ToString(),
                        "Collected project-owned base settings referenced by this selector.",
                        GameWindowChrome.AccentColor),
                    new GameWindowStatCard(
                        "Component Settings",
                        projectSettingSelector.GetAllComponentSettings().Count.ToString(),
                        "Collected project-owned component settings referenced by this selector.",
                        GameWindowChrome.SuccessColor),
                    new GameWindowStatCard(
                        "Selector Asset",
                        string.IsNullOrEmpty(assetPath) ? "Unknown" : Path.GetFileName(assetPath),
                        GameWindowChrome.GetAssetDirectory(assetPath),
                        GameWindowChrome.WarningColor));
                return;
            }

            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard(
                    "Type",
                    settingTarget.GetType().Name,
                    "The concrete ScriptableObject type currently selected in GameWindow.",
                    GameWindowChrome.AccentColor),
                new GameWindowStatCard(
                    "Inspector",
                    _cachedSettingEditor != null ? _cachedSettingEditor.GetType().Name : "Missing Editor",
                    "Keeps the existing custom inspector fields and behavior.",
                    GameWindowChrome.SuccessColor),
                new GameWindowStatCard(
                    "Location",
                    string.IsNullOrEmpty(assetPath) ? "Unknown" : Path.GetFileName(assetPath),
                    GameWindowChrome.GetAssetDirectory(assetPath),
                    GameWindowChrome.WarningColor));
        }

        private void DrawProfiledPage(ProfiledBase profiledBase)
        {
            string title = string.IsNullOrEmpty(profiledBase.Title)
                ? GameWindowChrome.GetDisplayName(profiledBase.GetType().Name, "Profiled")
                : profiledBase.Title;
            string subtitle = string.IsNullOrEmpty(profiledBase.SubTitle)
                ? "Realtime runtime diagnostics, summary metrics, and module state."
                : profiledBase.SubTitle;

            GameWindowChrome.BeginPage(ref _frameworkProfiledScrollPosition);
            GameWindowChrome.DrawHeader(
                title,
                subtitle,
                new GameWindowBadge("Mode", EditorApplication.isPlaying ? "Live Runtime" : "Runtime Only"),
                new GameWindowBadge("Refresh", "Realtime"));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);

            if (!EditorApplication.isPlaying)
            {
                GameWindowChrome.DrawStateBanner("Runtime Only", "This profiled page is only available in Play Mode.", MessageType.Warning);
                GameWindowChrome.EndPage();
                return;
            }

            if (!profiledBase.CanDraw)
            {
                GameWindowChrome.DrawStateBanner("Not Available", "The runtime component required by this profiled page is unavailable.", MessageType.Info);
                GameWindowChrome.EndPage();
                return;
            }

            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard("Panel", title, "The currently selected diagnostic panel.", GameWindowChrome.AccentColor),
                new GameWindowStatCard("State", "Live", "The window keeps repainting while the game is running.", GameWindowChrome.SuccessColor),
                new GameWindowStatCard("Host", "Unified Shell", "Adds scroll, state banners, and draw-failure protection.", GameWindowChrome.WarningColor));
            GameWindowChrome.DrawSectionHeader("Live View", "The body below is still drawn by the original profiled page.");
            GameWindowChrome.BeginContentCard();
            try
            {
                profiledBase.Draw();
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(
                    $"Profiled page draw failed: {exception.GetType().Name}: {exception.Message}",
                    MessageType.Error);
            }

            GameWindowChrome.EndContentCard();
            GameWindowChrome.EndPage();
            Repaint();
        }

        private void EnsureSettingEditor(UnityEngine.Object settingTarget)
        {
            if (_cachedSettingTarget != settingTarget)
            {
                _cachedSettingTarget = settingTarget;
            }

            UnityEditor.Editor.CreateCachedEditor(settingTarget, null, ref _cachedSettingEditor);
        }

        private static void AddAllExtendItems(string baseFold, OdinMenuTree tree)
        {
            AppendExtends();
            if (_gameWindowExtends == null || _gameWindowExtends.Count == 0)
            {
                return;
            }

            foreach (var extend in _gameWindowExtends)
            {
                tree.AddMenuItemAtPath(baseFold, new OdinMenuItem(tree, extend.FoldName, extend));
                var items = extend.Handle(tree);
                if (items == null)
                {
                    continue;
                }

                foreach (var item in items)
                {
                    if (item == null || item.Value == null)
                    {
                        continue;
                    }

                    tree.AddMenuItemAtPath(baseFold + '/' + extend.FoldName, item);
                }
            }
        }

        private static void AppendExtends()
        {
            if (_gameWindowExtends != null)
            {
                return;
            }

            _gameWindowExtends = new List<IGameWindowExtend>();
            var allTypes = GameFramework.Utility.Assembly.GetTypes();
            foreach (var type in allTypes)
            {
                if (!type.InheritsFrom<IGameWindowExtend>())
                {
                    continue;
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                _gameWindowExtends.Add((IGameWindowExtend)Activator.CreateInstance(type));
            }
        }

        private static IEnumerable<OdinMenuItem> AddAllAssetsAtType<T>(OdinMenuTree tree, string menuPath)
            where T : ScriptableObject
        {
            var allSettings = GameWindowAssetLocator.GetPreferredAssetsAtType<T>();

            menuPath = menuPath ?? string.Empty;
            menuPath = menuPath.TrimStart('/');
            HashSet<OdinMenuItem> result = new HashSet<OdinMenuItem>();
            foreach (var setting in allSettings)
            {
                if (@setting == (UnityEngine.Object)null)
                {
                    continue;
                }

                var assetsPath = AssetDatabase.GetAssetPath(setting);
                string menuLeafName = GameWindowAssetLocator.GetMenuItemName(setting, allSettings);
                string path = menuPath.Trim('/') + "/" + menuLeafName;
                SplitMenuPath(path, out path, out string name);
                tree.AddMenuItemAtPath(result, path, new OdinMenuItem(tree, name, setting));
            }

            return result;
        }

        private static void SplitMenuPath(string menuPath, out string path, out string name)
        {
            menuPath = menuPath.Trim('/');
            int length = menuPath.LastIndexOf('/');
            if (length == -1)
            {
                path = string.Empty;
                name = menuPath;
            }
            else
            {
                path = menuPath.Substring(0, length);
                name = menuPath.Substring(length + 1);
            }
        }
        
    }
}
