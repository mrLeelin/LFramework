using System;
using System.IO;
using System.Linq;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor
{
    internal readonly struct GameWindowBadge
    {
        internal readonly string Label;
        internal readonly string Value;

        internal GameWindowBadge(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    internal readonly struct GameWindowStatCard
    {
        internal readonly string Label;
        internal readonly string Value;
        internal readonly string Hint;
        internal readonly Color AccentColor;

        internal GameWindowStatCard(string label, string value, string hint, Color accentColor)
        {
            Label = label;
            Value = value;
            Hint = hint;
            AccentColor = accentColor;
        }
    }

    internal static class GameWindowChrome
    {
        internal static readonly Color AccentColor = new Color(0.18f, 0.67f, 0.94f);
        internal static readonly Color SuccessColor = new Color(0.22f, 0.72f, 0.38f);
        internal static readonly Color WarningColor = new Color(0.92f, 0.64f, 0.18f);
        internal static readonly Color ErrorColor = new Color(0.86f, 0.29f, 0.25f);

        private static readonly Color CardBgLight = new Color(0.94f, 0.95f, 0.97f, 0.92f);
        private static readonly Color CardBgDark = new Color(0.20f, 0.22f, 0.25f, 0.92f);
        private static readonly Color BorderLight = new Color(0.55f, 0.58f, 0.62f, 0.28f);
        private static readonly Color BorderDark = new Color(0.48f, 0.52f, 0.58f, 0.35f);
        private static readonly Color MutedTextLight = new Color(0.37f, 0.40f, 0.45f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.70f, 0.74f);

        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _badgeStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _sectionSubtitleStyle;
        private static GUIStyle _cardTitleStyle;
        private static GUIStyle _cardValueStyle;
        private static GUIStyle _cardHintStyle;
        private static GUIStyle _bannerTitleStyle;
        private static GUIStyle _bannerBodyStyle;
        private static bool _stylesInitialized;


        internal static float GetDefaultWidth() => 520;
        
        internal static void BeginPage(ref Vector2 scrollPosition)
        {
            EnsureStyles();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Space(12f);
        }

        internal static void EndPage()
        {
            GUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
        }

        internal static void DrawHeader(string title, string subtitle, params GameWindowBadge[] badges)
        {
            EnsureStyles();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, _titleStyle, GUILayout.Height(32f));
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, _subtitleStyle);
            }

            if (badges != null && badges.Length > 0)
            {
                GUILayout.Space(6f);
                EditorGUILayout.BeginHorizontal();
                foreach (var badge in badges)
                {
                    if (string.IsNullOrEmpty(badge.Label) && string.IsNullOrEmpty(badge.Value))
                    {
                        continue;
                    }

                    DrawBadge(badge.Label, badge.Value);
                    GUILayout.Space(8f);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        internal static void DrawSeparator()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            var rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            rect.width -= 16f;
            EditorGUI.DrawRect(rect, GetBorderColor());
            EditorGUILayout.EndHorizontal();
        }

        internal static void DrawSectionHeader(string title, string subtitle = null)
        {
            EnsureStyles();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, _sectionSubtitleStyle);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        internal static void DrawCompactHeader(string title, string subtitle = null)
        {
            EnsureStyles();
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, _sectionSubtitleStyle);
            }
        }

        internal static void DrawStateBanner(string title, string message, MessageType messageType)
        {
            EnsureStyles();

            var accentColor = messageType switch
            {
                MessageType.Error => ErrorColor,
                MessageType.Warning => WarningColor,
                _ => SuccessColor,
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            var rect = GUILayoutUtility.GetRect(0f, 72f, GUILayout.ExpandWidth(true));
            rect.width -= 16f;
            DrawCardBackground(rect, accentColor);

            var titleRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 18f);
            var messageRect = new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, 28f);

            _bannerTitleStyle.normal.textColor = accentColor;
            _bannerBodyStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.20f);

            GUI.Label(titleRect, title, _bannerTitleStyle);
            GUI.Label(messageRect, message, _bannerBodyStyle);

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        internal static void DrawStatCards(params GameWindowStatCard[] cards)
        {
            EnsureStyles();
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            int columns = GetColumns(cards.Length);
            float availableWidth = GetDefaultWidth();
            float cardWidth = (availableWidth - (columns - 1) * 8f) / columns;

            for (int i = 0; i < cards.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16f);

                for (int j = 0; j < columns && i + j < cards.Length; j++)
                {
                    if (j > 0)
                    {
                        GUILayout.Space(8f);
                    }

                    DrawStatCard(cards[i + j], cardWidth);
                }

                GUILayout.Space(16f);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }

        internal static void BeginContentCard()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(6f);
        }

        internal static void EndContentCard()
        {
            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        internal static string GetDisplayName(string rawName, params string[] suffixes)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return "Untitled";
            }

            if (suffixes != null)
            {
                foreach (var suffix in suffixes)
                {
                    if (string.IsNullOrEmpty(suffix))
                    {
                        continue;
                    }

                    if (rawName.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        rawName = rawName.Substring(0, rawName.Length - suffix.Length);
                        break;
                    }
                }
            }

            return ObjectNames.NicifyVariableName(rawName);
        }

        internal static string GetShortTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return "Unassigned";
            }

            int splitIndex = typeName.LastIndexOf('.');
            return splitIndex >= 0 && splitIndex < typeName.Length - 1
                ? typeName.Substring(splitIndex + 1)
                : typeName;
        }

        internal static string GetAssetDirectory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return "Assets";
            }

            return Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "Assets";
        }

        private static void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft,
            };
            _titleStyle.normal.textColor = AccentColor;

            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
            };
            _subtitleStyle.normal.textColor = GetMutedTextColor();

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };
            _badgeStyle.normal.textColor = GetMutedTextColor();

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
            };
            _sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.15f, 0.15f, 0.15f);

            _sectionSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = true,
            };
            _sectionSubtitleStyle.normal.textColor = GetMutedTextColor();

            _cardTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
            };
            _cardTitleStyle.normal.textColor = GetMutedTextColor();

            _cardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true,
                clipping = TextClipping.Clip,
            };

            _cardHintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = true,
                clipping = TextClipping.Clip,
            };
            _cardHintStyle.normal.textColor = GetMutedTextColor();

            _bannerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _bannerBodyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
        }

        private static void DrawBadge(string label, string value)
        {
            var content = new GUIContent($"  {label}: {value}  ");
            var size = _badgeStyle.CalcSize(content);
            var rect = GUILayoutUtility.GetRect(size.x + 10f, 20f, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.30f, 0.34f, 0.9f)
                : new Color(0.86f, 0.89f, 0.93f, 0.95f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), AccentColor);
            GUI.Label(rect, content, _badgeStyle);
        }

        private static void DrawStatCard(GameWindowStatCard card, float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 82f, GUILayout.Width(width));
            DrawCardBackground(rect, card.AccentColor);

            var titleRect = new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 16f);
            var valueRect = new Rect(rect.x + 14f, rect.y + 28f, rect.width - 28f, 24f);
            var hintRect = new Rect(rect.x + 14f, rect.y + 54f, rect.width - 28f, 20f);

            GUI.Label(titleRect, card.Label, _cardTitleStyle);
            GUI.Label(valueRect, card.Value, _cardValueStyle);
            GUI.Label(hintRect, card.Hint, _cardHintStyle);
        }

        private static void DrawCardBackground(Rect rect, Color accentColor)
        {
            var backgroundColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor);
        }

        private static Color GetBorderColor()
        {
            return EditorGUIUtility.isProSkin ? BorderDark : BorderLight;
        }

        private static Color GetMutedTextColor()
        {
            return EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;
        }

        private static int GetColumns(int cardCount)
        {
            if (EditorGUIUtility.currentViewWidth < 540f)
            {
                return 1;
            }

            if (EditorGUIUtility.currentViewWidth < 860f)
            {
                return Mathf.Min(cardCount, 2);
            }

            return Mathf.Min(cardCount, 3);
        }
    }

    internal sealed class GameWindowFrameworkSettingOverview
    {
        private Vector2 _scrollPosition;

        [OnInspectorGUI]
        private void DrawPage()
        {
            int assetCount = AssetUtilities.GetAllAssetsOfType<ComponentSetting>().Count();
            int typeCount = Type.GetRuntimeOrEditorTypes(typeof(ComponentSetting))
                .Count(type => !type.IsAbstract && !type.IsInterface);

            GameWindowChrome.BeginPage(ref _scrollPosition);
            GameWindowChrome.DrawHeader(
                "Framework Setting",
                "Hosts all ComponentSetting assets with a shared visual shell while keeping the original custom editor logic.",
                new GameWindowBadge("Mode", EditorApplication.isPlaying ? "Play Mode" : "Edit Mode"),
                new GameWindowBadge("Scope", "Component Settings"));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);
            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard("Assets", assetCount.ToString(), "Total ComponentSetting assets in the project.", GameWindowChrome.AccentColor),
                new GameWindowStatCard("Types", typeCount.ToString(), "Runtime and editor setting types this window can host.", GameWindowChrome.SuccessColor),
                new GameWindowStatCard("Host", "Unified Shell", "Child pages still render through the original custom editor code.", GameWindowChrome.WarningColor));
            GameWindowChrome.DrawSectionHeader("Usage", "Select any concrete setting page from the left tree. The shell adds title, context, and spacing without changing the underlying behavior.");
            GameWindowChrome.BeginContentCard();
            GUILayout.Label(
                "- Settings are still saved directly back to the ScriptableObject asset.\n" +
                "- Runtime-related fields remain controlled by the existing inspector code.\n" +
                "- Dense pages can be refined later without reworking the menu host again.",
                EditorStyles.wordWrappedLabel);
            GameWindowChrome.EndContentCard();
            GameWindowChrome.EndPage();
        }
    }

    internal sealed class GameWindowFrameworkProfiledOverview
    {
        private readonly Func<int> _panelCountProvider;
        private Vector2 _scrollPosition;

        internal GameWindowFrameworkProfiledOverview(Func<int> panelCountProvider)
        {
            _panelCountProvider = panelCountProvider;
        }

        [OnInspectorGUI]
        private void DrawPage()
        {
            int panelCount = _panelCountProvider?.Invoke() ?? 0;

            GameWindowChrome.BeginPage(ref _scrollPosition);
            GameWindowChrome.DrawHeader(
                "Framework Profiled",
                "Collects runtime monitoring pages behind one host so each panel gets consistent scroll, state, and error handling.",
                new GameWindowBadge("Mode", EditorApplication.isPlaying ? "Live Runtime" : "Runtime Only"),
                new GameWindowBadge("Refresh", "Realtime"));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);
            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard("Panels", panelCount.ToString(), "Auto-discovered profiled pages available in the menu.", GameWindowChrome.AccentColor),
                new GameWindowStatCard("State", EditorApplication.isPlaying ? "Live" : "Standby", "Most panels are meaningful only while the game is running.", GameWindowChrome.SuccessColor),
                new GameWindowStatCard("Shell", "Unified Host", "Long pages gain consistent scroll and runtime state treatment.", GameWindowChrome.WarningColor));

            if (!EditorApplication.isPlaying)
            {
                GameWindowChrome.DrawSectionHeader("Current State");
                GameWindowChrome.DrawStateBanner("Runtime Only", "Enter Play Mode before opening live profiled pages to avoid empty or partial output.", MessageType.Warning);
            }
            else
            {
                GameWindowChrome.DrawSectionHeader("Current State", "Select a concrete profiled page from the left tree to inspect runtime data.");
                GameWindowChrome.BeginContentCard();
                GUILayout.Label(
                    "- The host is responsible for title, scroll, runtime state, and draw protection.\n" +
                    "- Dense pages can be refined in later waves without reworking the host again.\n" +
                    "- This first pass focuses on readability and complete presentation, not behavior changes.",
                    EditorStyles.wordWrappedLabel);
                GameWindowChrome.EndContentCard();
            }

            GameWindowChrome.EndPage();
        }
    }
}
