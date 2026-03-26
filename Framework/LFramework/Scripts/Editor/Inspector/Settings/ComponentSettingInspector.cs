using System.Collections.Generic;
using System.Linq;
using System.Text;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ComponentSetting))]
    public abstract class ComponentSettingInspector : GameFrameworkInspector
    {
        private static readonly Color BoundAccent = new Color(0.22f, 0.71f, 0.46f);
        private static readonly Color UnboundAccent = new Color(0.91f, 0.63f, 0.18f);
        private static readonly Color CardLight = new Color(0.96f, 0.97f, 0.99f, 1f);
        private static readonly Color CardDark = new Color(0.20f, 0.22f, 0.25f, 1f);
        private static readonly Color MutedTextLight = new Color(0.35f, 0.39f, 0.44f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.71f, 0.76f);

        private SerializedProperty _bindTypeName;
        private List<string> _allComponentNames;
        private GUIContent[] _componentOptions;
        private int _index;
        private GameFrameworkComponent _gameFrameworkComponent;

        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardValueStyle;
        private GUIStyle _cardDetailStyle;
        private GUIStyle _cardFooterStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _sectionDetailStyle;
        private bool _stylesInitialized;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _bindTypeName ??= serializedObject.FindProperty("bindTypeName");
            if (_allComponentNames == null || _componentOptions == null)
            {
                RefreshTypeNames();
            }

            EnsureStyles();

            var descriptor = ComponentSettingBindingDescriptorFactory.Create(
                _bindTypeName.stringValue,
                Mathf.Max(0, (_allComponentNames?.Count ?? 1) - 1));

            DrawBindingHero(descriptor);
            DrawBindingSelector(descriptor);

            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnEnable()
        {
            _bindTypeName = serializedObject.FindProperty("bindTypeName");
            RefreshTypeNames();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        protected T GetComponent<T>() where T : GameFrameworkComponent
        {
            if (!EditorApplication.isPlaying)
            {
                return null;
            }

            _gameFrameworkComponent ??= LFrameworkAspect.Instance.Get<T>();
            return _gameFrameworkComponent as T;
        }

        private void DrawBindingHero(ComponentSettingBindingDescriptor descriptor)
        {
            Color accentColor = descriptor.HasBinding ? BoundAccent : UnboundAccent;

            Rect rect = GUILayoutUtility.GetRect(0f, 94f, GUILayout.ExpandWidth(true));
            DrawCard(rect, accentColor);

            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 32f, 18f), "Component Binding", _cardTitleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 30f, rect.width - 140f, 24f), descriptor.BindingDisplayName, _cardValueStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 54f, rect.width - 32f, 18f), descriptor.DetailText, _cardDetailStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 74f, rect.width - 32f, 16f), descriptor.AvailabilityText, _cardFooterStyle);

            DrawStatusBadge(new Rect(rect.xMax - 96f, rect.y + 14f, 80f, 24f), descriptor.StatusLabel, accentColor);
            GUILayout.Space(6f);
        }

        private void DrawBindingSelector(ComponentSettingBindingDescriptor descriptor)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Binding Selector", _sectionHeaderStyle);
                EditorGUILayout.LabelField(
                    descriptor.HasBinding
                        ? "Switch the target runtime component or clear the binding if this asset should stay generic."
                        : "Pick a GameFrameworkComponent to make the host relationship explicit for this setting asset.",
                    _sectionDetailStyle);

                EditorGUI.BeginChangeCheck();
                int nextIndex = EditorGUILayout.Popup(new GUIContent("Runtime Component"), _index, _componentOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    _index = Mathf.Clamp(nextIndex, 0, _allComponentNames.Count - 1);
                    _bindTypeName.stringValue = _allComponentNames[_index];
                }
            }

            GUILayout.Space(6f);
        }

        private void RefreshTypeNames()
        {
            var runtimeTypeNames = Type.GetRuntimeTypeNames(typeof(GameFrameworkComponent))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            _allComponentNames = new List<string>(runtimeTypeNames.Count + 1)
            {
                string.Empty,
            };
            _allComponentNames.AddRange(runtimeTypeNames);

            _componentOptions = new GUIContent[_allComponentNames.Count];
            _componentOptions[0] = new GUIContent("Unassigned", "Clear the runtime component binding.");
            for (int index = 1; index < _allComponentNames.Count; index++)
            {
                string typeName = _allComponentNames[index];
                _componentOptions[index] = new GUIContent(
                    ComponentSettingBindingDescriptorFactory.GetDisplayName(typeName),
                    typeName);
            }

            _index = Mathf.Max(0, _allComponentNames.IndexOf(_bindTypeName != null ? _bindTypeName.stringValue : string.Empty));
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _cardTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardDetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
            _cardDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.21f);

            _cardFooterStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };
            _cardFooterStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4),
            };

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

        private void DrawCard(Rect rect, Color accentColor)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? CardDark : CardLight);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), accentColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f));
        }

        private void DrawStatusBadge(Rect rect, string text, Color accentColor)
        {
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f)
                : new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);

            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor);

            Color previousColor = _badgeStyle.normal.textColor;
            _badgeStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.21f);
            GUI.Label(rect, text, _badgeStyle);
            _badgeStyle.normal.textColor = previousColor;
        }
    }

    internal sealed class ComponentSettingBindingDescriptor
    {
        internal ComponentSettingBindingDescriptor(
            string statusLabel,
            string bindingDisplayName,
            string detailText,
            string availabilityText,
            bool hasBinding)
        {
            StatusLabel = statusLabel;
            BindingDisplayName = bindingDisplayName;
            DetailText = detailText;
            AvailabilityText = availabilityText;
            HasBinding = hasBinding;
        }

        internal string StatusLabel { get; }
        internal string BindingDisplayName { get; }
        internal string DetailText { get; }
        internal string AvailabilityText { get; }
        internal bool HasBinding { get; }
    }

    internal static class ComponentSettingBindingDescriptorFactory
    {
        internal static ComponentSettingBindingDescriptor Create(string bindTypeName, int availableComponentCount)
        {
            bool hasBinding = !string.IsNullOrWhiteSpace(bindTypeName);
            return new ComponentSettingBindingDescriptor(
                hasBinding ? "Bound" : "Unbound",
                hasBinding ? GetDisplayName(bindTypeName) : "No Component Bound",
                hasBinding
                    ? bindTypeName
                    : "Select a GameFrameworkComponent to define which runtime system this setting configures.",
                availableComponentCount > 0
                    ? $"{availableComponentCount} runtime component types are available for binding."
                    : "No runtime component types were discovered in the current editor domain.",
                hasBinding);
        }

        internal static string GetDisplayName(string bindTypeName)
        {
            if (string.IsNullOrWhiteSpace(bindTypeName))
            {
                return "Unassigned";
            }

            string shortTypeName = bindTypeName;
            int lastDotIndex = bindTypeName.LastIndexOf('.');
            if (lastDotIndex >= 0 && lastDotIndex < bindTypeName.Length - 1)
            {
                shortTypeName = bindTypeName.Substring(lastDotIndex + 1);
            }

            return Nicify(shortTypeName);
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
}
