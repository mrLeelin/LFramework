using System;
using System.Linq;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal abstract class ProfiledBase
    {
        internal virtual string Title { get; } = string.Empty;
        internal virtual string SubTitle { get; } = string.Empty;
        internal abstract bool CanDraw { get; }

        internal abstract void Draw();

        protected void GetComponent<T>(ref T instance) where T : GameFrameworkComponent
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (instance != null)
            {
                return;
            }

            instance = LFrameworkAspect.Instance.Get<T>();
        }

        protected void DrawMetricCards(params ProfiledMetric[] metrics)
        {
            ProfiledPanelWidgets.DrawMetricCards(metrics);
        }

        protected void DrawSection(string title, string description, Action drawContent)
        {
            ProfiledPanelWidgets.DrawSection(title, description, drawContent);
        }

        protected void DrawKeyValueRow(string label, string value)
        {
            ProfiledPanelWidgets.DrawKeyValueRow(label, value);
        }
    }

    internal readonly struct ProfiledMetric
    {
        internal ProfiledMetric(string label, string value, string detail = null)
        {
            Label = label;
            Value = string.IsNullOrWhiteSpace(value) ? "N/A" : value;
            Detail = detail ?? string.Empty;
        }

        internal string Label { get; }
        internal string Value { get; }
        internal string Detail { get; }
    }

    internal static class ProfiledTextFormatter
    {
        internal static string JoinOrFallback(string[] values, string fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            string[] filtered = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            return filtered.Length == 0
                ? fallback
                : string.Join(", ", filtered);
        }
    }

    internal static class ProfiledPanelWidgets
    {
        private static readonly Color AccentColor = new Color(0.18f, 0.67f, 0.94f);
        private static readonly Color CardLight = new Color(0.96f, 0.97f, 0.99f, 1f);
        private static readonly Color CardDark = new Color(0.20f, 0.22f, 0.25f, 1f);
        private static readonly Color MutedTextLight = new Color(0.35f, 0.39f, 0.44f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.71f, 0.76f);

        private static GUIStyle _cardLabelStyle;
        private static GUIStyle _cardValueStyle;
        private static GUIStyle _cardDetailStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _sectionDetailStyle;
        private static bool _stylesInitialized;

        internal static void DrawMetricCards(params ProfiledMetric[] metrics)
        {
            EnsureStyles();

            if (metrics == null || metrics.Length == 0)
            {
                return;
            }

            float availableWidth = Mathf.Max(360f, EditorGUIUtility.currentViewWidth - 74f);
            float cardWidth = (availableWidth - 8f) * 0.5f;

            for (int index = 0; index < metrics.Length; index += 2)
            {
                EditorGUILayout.BeginHorizontal();
                DrawMetricCard(metrics[index], cardWidth);
                GUILayout.Space(8f);
                if (index + 1 < metrics.Length)
                {
                    DrawMetricCard(metrics[index + 1], cardWidth);
                }
                else
                {
                    GUILayout.Space(cardWidth);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }

        internal static void DrawSection(string title, string description, Action drawContent)
        {
            EnsureStyles();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, _sectionHeaderStyle);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    EditorGUILayout.LabelField(description, _sectionDetailStyle);
                    GUILayout.Space(6f);
                }

                drawContent?.Invoke();
            }
        }

        internal static void DrawKeyValueRow(string label, string value)
        {
            EditorGUILayout.LabelField(label, string.IsNullOrWhiteSpace(value) ? "N/A" : value);
        }

        private static void DrawMetricCard(ProfiledMetric metric, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, 78f, GUILayout.Width(width));
            DrawCard(rect);

            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 16f), metric.Label, _cardLabelStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 28f, rect.width - 28f, 22f), metric.Value, _cardValueStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 52f, rect.width - 28f, 16f), metric.Detail, _cardDetailStyle);
        }

        private static void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _cardLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };

            _cardDetailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _cardDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _sectionDetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
            _sectionDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;
        }

        private static void DrawCard(Rect rect)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? CardDark : CardLight);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.45f));
        }
    }
}
