using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LFramework.Editor.Builder;
using LFramework.Runtime;
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
        private GameWindowPageChrome _pageChrome;
        private List<ProfiledBase> _allProfiled;

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
            _pageChrome = new GameWindowPageChrome();

            if (_allProfiled != null)
            {
                return;
            }

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

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                { "Home", _gameWindowHome, EditorIcons.House },
                { "Local Resource Server", _gameWindowLocalResourceServer, EditorIcons.SettingsCog },
            };

            AddAllAssetsAtType<ComponentSetting>(tree, "Framework Setting")
                .AddIcons(EditorIcons.SettingsCog);

            tree.Add("Framework Profiled", null, EditorIcons.Car);
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

                    tree.AddObjectAtPath("Framework Profiled/" + name, profiled);
                }
            }

            AddAllAssetsAtType<SettingSelector>(tree, "Game Setting/Setting Selector")
                .AddIcons(EditorIcons.SettingsCog);

            AddAllAssetsAtType<BaseSetting>(tree, "Game Setting/GameSettings")
                .AddIcons(EditorIcons.SettingsCog);

            tree.Add("打包", null, EditorIcons.Airplane);
            tree.AddObjectAtPath("打包/打包资源", new BuildResourcesData()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/打包App", new BuildPackageWindow()).AddIcon(EditorIcons.SettingsCog);
            tree.AddObjectAtPath("打包/上传版本文件", new BuildVersionWindow()).AddIcon(EditorIcons.Car);
            tree.AddObjectAtPath("Utility/OpenFolder", new OpenFolderInspector()).AddIcon(EditorIcons.ShoppingCart);
            AddAllExtendItems("功能扩展", tree);

            BuildMenuTreeAction?.Invoke(tree);
            return tree;
        }

        protected override void DrawEditors()
        {
            var selected = MenuTree?.Selection?.SelectedValue;
            if (selected is ComponentSetting componentSetting)
            {
                DrawComponentSettingPage(componentSetting);
                return;
            }

            if (selected is ProfiledBase profiledBase)
            {
                DrawProfiledPage(profiledBase);
                return;
            }

            base.DrawEditors();
        }

        private void DrawComponentSettingPage(ComponentSetting componentSetting)
        {
            var descriptor = GameWindowPageDescriptorFactory.Create(componentSetting);
            if (descriptor == null || _pageChrome == null)
            {
                base.DrawEditors();
                return;
            }

            _pageChrome.DrawPage(descriptor, () =>
            {
                var bindingConfigured = !string.IsNullOrWhiteSpace(componentSetting.bindTypeName);
                _pageChrome.DrawBanner(
                    bindingConfigured ? "Component Binding" : "Binding Required",
                    bindingConfigured
                        ? $"This setting currently targets {componentSetting.bindTypeName}."
                        : "Select a GameFrameworkComponent in the inspector below to make the asset binding explicit.",
                    bindingConfigured ? MessageType.Info : MessageType.Warning);

                _pageChrome.DrawSurface("Inspector", DrawCurrentEditors);
            });
        }

        private void DrawProfiledPage(ProfiledBase profiledBase)
        {
            var descriptor = GameWindowPageDescriptorFactory.Create(profiledBase);
            if (descriptor == null || _pageChrome == null)
            {
                DrawLegacyProfiled(profiledBase);
                return;
            }

            _pageChrome.DrawPage(descriptor, () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    _pageChrome.DrawBanner(
                        "Runtime Only",
                        "This profiled panel is only available while the Unity editor is in Play Mode.",
                        MessageType.Info);
                    return;
                }

                if (!profiledBase.CanDraw)
                {
                    _pageChrome.DrawBanner(
                        "Not Available",
                        "The selected runtime monitor is currently unavailable. Check initialization state and dependencies.",
                        MessageType.Warning);
                    return;
                }

                _pageChrome.DrawSurface("Live Metrics", () =>
                {
                    profiledBase.Draw();
                });

                Repaint();
            });
        }

        private void DrawLegacyProfiled(ProfiledBase profiledBase)
        {
            GUILayout.BeginVertical();

            if (!EditorApplication.isPlaying)
            {
                SirenixEditorGUI.Title(
                    "Runtime Only",
                    "This profiled panel is only available while the Unity editor is in Play Mode.",
                    TextAlignment.Left,
                    true);
                GUILayout.EndVertical();
                return;
            }

            if (!profiledBase.CanDraw)
            {
                SirenixEditorGUI.Title(
                    "Not Available",
                    "The selected runtime monitor is currently unavailable.",
                    TextAlignment.Left,
                    true);
                GUILayout.EndVertical();
                return;
            }

            SirenixEditorGUI.Title(
                string.IsNullOrEmpty(profiledBase.Title) ? profiledBase.GetType().Name : profiledBase.Title,
                string.IsNullOrEmpty(profiledBase.SubTitle) ? profiledBase.GetType().GetNiceFullName() : profiledBase.SubTitle,
                TextAlignment.Left,
                true);

            GUILayout.Space(10f);
            profiledBase.Draw();
            GUILayout.EndVertical();
            Repaint();
        }

        private void DrawCurrentEditors()
        {
            var currentDrawingTargets = CurrentDrawingTargets;
            if (currentDrawingTargets == null || currentDrawingTargets.Count == 0)
            {
                return;
            }

            for (var index = 0; index < currentDrawingTargets.Count; index++)
            {
                DrawEditor(index);
                if (index < currentDrawingTargets.Count - 1)
                {
                    GUILayout.Space(10f);
                }
            }
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
            var allSettings = AssetUtilities.GetAllAssetsOfType<T>();

            menuPath = menuPath ?? string.Empty;
            menuPath = menuPath.TrimStart('/');

            HashSet<OdinMenuItem> result = new HashSet<OdinMenuItem>();
            foreach (var setting in allSettings)
            {
                if (setting == null)
                {
                    continue;
                }

                var assetsPath = AssetDatabase.GetAssetPath(setting);
                string withoutExtension = Path.GetFileNameWithoutExtension(assetsPath);
                string path = menuPath.Trim('/') + "/" + withoutExtension;

                SplitMenuPath(path, out path, out var name);
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

    internal sealed class GameWindowPageDescriptor
    {
        internal GameWindowPageDescriptor(
            string contextLabel,
            string title,
            string subtitle,
            Color accentColor,
            string primaryMetaLabel,
            string primaryMetaValue,
            string secondaryMetaLabel,
            string secondaryMetaValue)
        {
            ContextLabel = contextLabel;
            Title = title;
            Subtitle = subtitle;
            AccentColor = accentColor;
            PrimaryMetaLabel = primaryMetaLabel;
            PrimaryMetaValue = primaryMetaValue;
            SecondaryMetaLabel = secondaryMetaLabel;
            SecondaryMetaValue = secondaryMetaValue;
        }

        internal string ContextLabel { get; }
        internal string Title { get; }
        internal string Subtitle { get; }
        internal Color AccentColor { get; }
        internal string PrimaryMetaLabel { get; }
        internal string PrimaryMetaValue { get; }
        internal string SecondaryMetaLabel { get; }
        internal string SecondaryMetaValue { get; }
        internal string ScrollKey => $"{ContextLabel}/{Title}";
    }

    internal static class GameWindowPageDescriptorFactory
    {
        private static readonly Color FrameworkSettingAccent = new Color(0.18f, 0.67f, 0.94f);
        private static readonly Color FrameworkProfiledAccent = new Color(0.17f, 0.72f, 0.54f);

        internal static GameWindowPageDescriptor Create(object selected)
        {
            if (selected is ComponentSetting componentSetting)
            {
                return Create(componentSetting);
            }

            if (selected is ProfiledBase profiledBase)
            {
                return Create(profiledBase);
            }

            return null;
        }

        internal static GameWindowPageDescriptor Create(ComponentSetting componentSetting)
        {
            if (componentSetting == null)
            {
                return null;
            }

            return CreateForComponent(componentSetting.GetType().Name, componentSetting.bindTypeName);
        }

        internal static GameWindowPageDescriptor Create(ProfiledBase profiledBase)
        {
            if (profiledBase == null)
            {
                return null;
            }

            return CreateForProfiled(profiledBase.GetType().Name, profiledBase.Title, profiledBase.SubTitle);
        }

        internal static GameWindowPageDescriptor CreateForComponent(string componentTypeName, string bindTypeName)
        {
            if (string.IsNullOrWhiteSpace(componentTypeName))
            {
                return null;
            }

            var normalizedName = StripSuffix(componentTypeName, "ComponentSetting");
            normalizedName = StripSuffix(normalizedName, "Setting");
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                normalizedName = componentTypeName;
            }

            return new GameWindowPageDescriptor(
                "Framework Setting",
                Nicify(normalizedName),
                string.IsNullOrWhiteSpace(bindTypeName) ? componentTypeName : bindTypeName,
                FrameworkSettingAccent,
                "Page",
                "Inspector",
                "Binding",
                string.IsNullOrWhiteSpace(bindTypeName) ? "Unbound" : "Bound");
        }

        internal static GameWindowPageDescriptor CreateForProfiled(string profiledTypeName, string title, string subtitle)
        {
            if (string.IsNullOrWhiteSpace(profiledTypeName) && string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var resolvedTypeName = string.IsNullOrWhiteSpace(profiledTypeName) ? title : profiledTypeName;
            var normalizedName = StripSuffix(resolvedTypeName, "Profiled");
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                normalizedName = resolvedTypeName;
            }

            return new GameWindowPageDescriptor(
                "Framework Profiled",
                string.IsNullOrWhiteSpace(title) ? Nicify(normalizedName) : title,
                string.IsNullOrWhiteSpace(subtitle) ? resolvedTypeName : subtitle,
                FrameworkProfiledAccent,
                "Page",
                "Metrics",
                "Mode",
                "Runtime");
        }

        private static string StripSuffix(string value, string suffix)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(suffix))
            {
                return value;
            }

            return value.EndsWith(suffix, StringComparison.Ordinal)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }

        private static string Nicify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '_' || current == '-')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    continue;
                }

                bool hasPrevious = i > 0;
                bool hasNext = i < value.Length - 1;
                char previous = hasPrevious ? value[i - 1] : '\0';
                char next = hasNext ? value[i + 1] : '\0';

                bool insertSpace =
                    hasPrevious &&
                    current != ' ' &&
                    (
                        char.IsUpper(current) && char.IsLower(previous) ||
                        char.IsDigit(current) && char.IsLetter(previous) ||
                        char.IsLetter(current) && char.IsDigit(previous) ||
                        char.IsUpper(current) && char.IsUpper(previous) && hasNext && char.IsLower(next)
                    );

                if (insertSpace && builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString().Trim();
        }
    }

    internal sealed class GameWindowPageChrome
    {
        private const float HorizontalPadding = 16f;

        private static readonly Color HeroBackgroundLight = new Color(0.96f, 0.97f, 0.99f, 1f);
        private static readonly Color HeroBackgroundDark = new Color(0.19f, 0.21f, 0.24f, 1f);
        private static readonly Color BannerBackgroundLight = new Color(1f, 1f, 1f, 0.96f);
        private static readonly Color BannerBackgroundDark = new Color(0.22f, 0.24f, 0.27f, 0.98f);
        private static readonly Color DividerLight = new Color(0.72f, 0.77f, 0.84f, 0.45f);
        private static readonly Color DividerDark = new Color(0.38f, 0.44f, 0.52f, 0.45f);
        private static readonly Color MutedTextLight = new Color(0.35f, 0.39f, 0.44f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.71f, 0.76f);
        private static readonly Color SurfaceTintLight = new Color(0.20f, 0.24f, 0.30f, 0.05f);
        private static readonly Color SurfaceTintDark = new Color(1f, 1f, 1f, 0.04f);

        private readonly Dictionary<string, Vector2> _scrollStates = new Dictionary<string, Vector2>();

        private GUIStyle _contextStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _metaStyle;
        private GUIStyle _surfaceStyle;
        private GUIStyle _surfaceHeaderStyle;
        private GUIStyle _bannerTitleStyle;
        private GUIStyle _bannerMessageStyle;
        private bool _stylesInitialized;

        internal void DrawPage(GameWindowPageDescriptor descriptor, Action drawBody)
        {
            if (descriptor == null)
            {
                drawBody?.Invoke();
                return;
            }

            EnsureStyles();

            if (!_scrollStates.TryGetValue(descriptor.ScrollKey, out var scrollPosition))
            {
                scrollPosition = Vector2.zero;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Space(12f);

            DrawHero(descriptor);
            GUILayout.Space(14f);
            DrawDivider(descriptor.AccentColor);
            GUILayout.Space(12f);

            drawBody?.Invoke();

            GUILayout.Space(12f);
            EditorGUILayout.EndScrollView();

            _scrollStates[descriptor.ScrollKey] = scrollPosition;
        }

        internal void DrawBanner(string title, string message, MessageType messageType)
        {
            EnsureStyles();

            Color accentColor = ResolveMessageColor(messageType);
            float width = Mathf.Max(240f, EditorGUIUtility.currentViewWidth - (HorizontalPadding * 4f));
            float messageHeight = Mathf.Max(22f, _bannerMessageStyle.CalcHeight(new GUIContent(message ?? string.Empty), width));
            float height = Mathf.Max(74f, messageHeight + 40f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);

            Rect rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            rect.width -= HorizontalPadding;
            DrawPanel(rect, accentColor, BannerBackgroundLight, BannerBackgroundDark);

            Color previousTitleColor = _bannerTitleStyle.normal.textColor;
            Color previousMessageColor = _bannerMessageStyle.normal.textColor;
            _bannerTitleStyle.normal.textColor = accentColor;
            _bannerMessageStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.17f, 0.18f, 0.21f);

            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 18f), title, _bannerTitleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 34f, rect.width - 32f, rect.height - 44f), message, _bannerMessageStyle);

            _bannerTitleStyle.normal.textColor = previousTitleColor;
            _bannerMessageStyle.normal.textColor = previousMessageColor;

            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        internal void DrawSurface(string header, Action drawContent)
        {
            EnsureStyles();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);

            EditorGUILayout.BeginVertical(_surfaceStyle);
            Rect tintRect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.Height(0f));
            tintRect.height = 1f;
            EditorGUI.DrawRect(tintRect, EditorGUIUtility.isProSkin ? SurfaceTintDark : SurfaceTintLight);

            if (!string.IsNullOrEmpty(header))
            {
                EditorGUILayout.LabelField(header, _surfaceHeaderStyle);
                GUILayout.Space(8f);
            }

            drawContent?.Invoke();
            EditorGUILayout.EndVertical();

            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _contextStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                wordWrap = true,
                clipping = TextClipping.Overflow,
            };

            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
            _subtitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _metaStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 4, 4),
            };

            _surfaceStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 14, 14),
                margin = new RectOffset(0, 0, 0, 0),
            };

            _surfaceHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _bannerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _bannerMessageStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
        }

        private void DrawHero(GameWindowPageDescriptor descriptor)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);

            Rect rect = GUILayoutUtility.GetRect(0f, 122f, GUILayout.ExpandWidth(true));
            rect.width -= HorizontalPadding;
            DrawPanel(rect, descriptor.AccentColor, HeroBackgroundLight, HeroBackgroundDark);

            Color previousContextColor = _contextStyle.normal.textColor;
            Color previousTitleColor = _titleStyle.normal.textColor;
            _contextStyle.normal.textColor = descriptor.AccentColor;
            _titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.14f, 0.16f, 0.19f);

            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 16f), descriptor.ContextLabel.ToUpperInvariant(), _contextStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 30f, rect.width - 36f, 30f), descriptor.Title, _titleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 62f, rect.width - 36f, 34f), descriptor.Subtitle, _subtitleStyle);

            _contextStyle.normal.textColor = previousContextColor;
            _titleStyle.normal.textColor = previousTitleColor;

            float chipX = rect.x + 18f;
            float chipY = rect.y + rect.height - 28f;
            DrawMetaChip(ref chipX, chipY, descriptor.PrimaryMetaLabel, descriptor.PrimaryMetaValue, descriptor.AccentColor);
            DrawMetaChip(ref chipX, chipY, descriptor.SecondaryMetaLabel, descriptor.SecondaryMetaValue, descriptor.AccentColor);

            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMetaChip(ref float x, float y, string label, string value, Color accentColor)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            string text = string.IsNullOrEmpty(label) ? value : $"{label}: {value}";
            GUIContent content = new GUIContent(text);
            Vector2 size = _metaStyle.CalcSize(content);
            Rect rect = new Rect(x, y, size.x + 20f, 22f);

            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f)
                : new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);
            Color textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.21f);

            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor);

            Color previousTextColor = _metaStyle.normal.textColor;
            _metaStyle.normal.textColor = textColor;
            GUI.Label(rect, content, _metaStyle);
            _metaStyle.normal.textColor = previousTextColor;

            x += rect.width + 8f;
        }

        private void DrawDivider(Color accentColor)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);

            Rect rect = GUILayoutUtility.GetRect(0f, 2f, GUILayout.ExpandWidth(true));
            rect.width -= HorizontalPadding;
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? DividerDark : DividerLight);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, Mathf.Min(110f, rect.width), rect.height), accentColor);

            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPanel(Rect rect, Color accentColor, Color lightBackground, Color darkBackground)
        {
            Color backgroundColor = EditorGUIUtility.isProSkin ? darkBackground : lightBackground;
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), accentColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f));
        }

        private static Color ResolveMessageColor(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Warning:
                    return new Color(0.91f, 0.63f, 0.18f);
                case MessageType.Error:
                    return new Color(0.88f, 0.33f, 0.28f);
                default:
                    return new Color(0.24f, 0.60f, 0.88f);
            }
        }
    }
}
