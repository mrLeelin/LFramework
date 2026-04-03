using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// Dedicated dashboard page for managing the local resource server.
    /// </summary>
    public class GameWindowLocalResourceServer
    {
        private static readonly Color AccentColor = new Color(0.18f, 0.67f, 0.94f);
        private static readonly Color CardBgLight = new Color(0.94f, 0.95f, 0.97f, 0.92f);
        private static readonly Color CardBgDark = new Color(0.20f, 0.22f, 0.25f, 0.92f);
        private static readonly Color SuccessColor = new Color(0.22f, 0.72f, 0.38f);
        private static readonly Color ErrorColor = new Color(0.86f, 0.29f, 0.25f);
        private static readonly Color WarningColor = new Color(0.92f, 0.64f, 0.18f);
        private static readonly Color MutedTextLight = new Color(0.37f, 0.40f, 0.45f);
        private static readonly Color MutedTextDark = new Color(0.67f, 0.70f, 0.74f);

        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardValueStyle;
        private GUIStyle _cardSubtitleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _messageDetailStyle;
        private bool _stylesInitialized;

        private LocalResourceServerController _controller;
        private int _port;
        private string _message;
        private MessageType _messageType = MessageType.Info;
        private Vector2 _scrollPosition;

        [OnInspectorGUI]
        private void DrawPage()
        {
            EnsureController();
            EnsureStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Space(12f);

            DrawHeader();
            GUILayout.Space(16f);
            DrawSeparator();
            GUILayout.Space(12f);

            DrawStatusSection();
            GUILayout.Space(14f);

            DrawConfigurationSection();
            GUILayout.Space(14f);

            DrawActionSection();

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Space(14f);
                DrawMessageCard();
            }

            GUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
        }

        private void EnsureController()
        {
            if (_controller != null)
            {
                return;
            }

            _controller = new LocalResourceServerController();
            _port = _controller.Port;
        }

        private void EnsureStyles()
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
            _subtitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
            };

            _cardTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
            };
            _cardTitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _cardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true,
                clipping = TextClipping.Overflow,
            };

            _cardSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = true,
            };
            _cardSubtitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fixedHeight = 30,
                padding = new RectOffset(12, 12, 5, 5),
            };

            _messageStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };

            _messageDetailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Local Resource Server", _titleStyle, GUILayout.Height(32f));
            EditorGUILayout.LabelField(
                "Manage local ServerData hosting for hot-update validation, endpoint checks, and local package testing.",
                _subtitleStyle);
            GUILayout.Space(6f);
            DrawHeaderBadge("Root", "ServerData");
            EditorGUILayout.EndVertical();

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderBadge(string label, string value)
        {
            GUIContent content = new GUIContent($"  {label}: {value}  ");
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };
            badgeStyle.normal.textColor = EditorGUIUtility.isProSkin ? MutedTextDark : MutedTextLight;

            Vector2 size = badgeStyle.CalcSize(content);
            Rect rect = GUILayoutUtility.GetRect(size.x + 10f, 20f, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.30f, 0.34f, 0.9f)
                : new Color(0.86f, 0.89f, 0.93f, 0.95f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), AccentColor);
            GUI.Label(rect, content, badgeStyle);
        }

        private void DrawStatusSection()
        {
            DrawSectionHeader("Overview");

            float availableWidth = GameWindowChrome.GetDefaultWidth();
            float cardWidth = (availableWidth - 8f) * 0.5f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            DrawInfoCard(
                "Service Status",
                _controller.IsRunning ? "Running" : "Stopped",
                _controller.IsRunning ? "Serving requests from the local ServerData folder." : "Ready to host ServerData on demand.",
                _controller.IsRunning ? SuccessColor : WarningColor,
                cardWidth,
                84f);
            GUILayout.Space(8f);
            DrawInfoCard(
                "Endpoint",
                _controller.BaseUrl,
                "Loopback URL used for local package and manifest testing.",
                AccentColor,
                cardWidth,
                84f);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfoCard(string title, string value, string subtitle, Color accentColor, float width, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width));
            DrawCardBackground(rect, accentColor);

            Rect titleRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 16f);
            Rect valueRect = new Rect(rect.x + 14f, rect.y + 30f, rect.width - 28f, 24f);
            Rect subtitleRect = new Rect(rect.x + 14f, rect.y + 56f, rect.width - 28f, 20f);

            GUI.Label(titleRect, title, _cardTitleStyle);
            GUI.Label(valueRect, value, _cardValueStyle);
            GUI.Label(subtitleRect, subtitle, _cardSubtitleStyle);
        }

        private void DrawConfigurationSection()
        {
            DrawSectionHeader("Configuration");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            Rect rect = GUILayoutUtility.GetRect(0f, 118f, GUILayout.ExpandWidth(true));
            rect.width -= 16f;
            DrawCardBackground(rect, AccentColor);

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 98f;

            Rect contentRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f);
            Rect portRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 18f);
            Rect runningRect = new Rect(contentRect.x, contentRect.y + 28f, contentRect.width, 18f);
            Rect pathRect = new Rect(contentRect.x, contentRect.y + 56f, contentRect.width, 18f);
            Rect noteRect = new Rect(contentRect.x, contentRect.y + 82f, contentRect.width, 20f);

            using (new EditorGUI.DisabledGroupScope(_controller.IsRunning))
            {
                int updatedPort = EditorGUI.IntField(portRect, "Port", _port);
                if (updatedPort != _port)
                {
                    _port = updatedPort;
                    _controller.Port = _port;
                }
            }

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUI.Toggle(runningRect, "Is Running", _controller.IsRunning);
                EditorGUI.TextField(pathRect, "ServerData", _controller.RootDirectory ?? string.Empty);
            }

            GUI.Label(noteRect, "The port field is locked while the server is running to keep the displayed endpoint stable.", _cardSubtitleStyle);
            EditorGUIUtility.labelWidth = previousLabelWidth;

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionSection()
        {
            DrawSectionHeader("Actions");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            if (DrawActionButton("Start", "d_PlayButton", AccentColor, !_controller.IsRunning))
            {
                TryStartLocalResourceServer();
            }

            GUILayout.Space(8f);

            if (DrawActionButton("Stop", "d_PreMatQuad", WarningColor, _controller.IsRunning))
            {
                _controller.Stop();
                SetMessage("Local resource server stopped.", MessageType.Info);
            }

            GUILayout.Space(8f);

            if (DrawActionButton("Open ServerData Folder", "d_FolderOpened Icon", new Color(0.30f, 0.60f, 0.88f), true))
            {
                _controller.EnsureServerDataDirectory();
                EditorUtility.RevealInFinder(_controller.RootDirectory);
                SetMessage("Opened the ServerData folder in the system file explorer.", MessageType.Info);
            }

            GUILayout.Space(8f);

            if (DrawActionButton("Copy URL", "Clipboard", new Color(0.35f, 0.58f, 0.82f), true))
            {
                EditorGUIUtility.systemCopyBuffer = _controller.BaseUrl;
                SetMessage($"Copied {_controller.BaseUrl} to the clipboard.", MessageType.Info);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawActionButton(string label, string iconName, Color backgroundColor, bool enabled)
        {
            Texture2D icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            GUIContent content = icon != null ? new GUIContent($" {label}", icon) : new GUIContent(label);

            Color previousBackgroundColor = GUI.backgroundColor;
            bool previousEnabled = GUI.enabled;

            GUI.backgroundColor = backgroundColor;
            GUI.enabled = enabled;
            bool clicked = GUILayout.Button(content, _buttonStyle, GUILayout.MinWidth(140f));

            GUI.backgroundColor = previousBackgroundColor;
            GUI.enabled = previousEnabled;

            return clicked;
        }

        private void DrawMessageCard()
        {
            Color accentColor = GetMessageColor(_messageType);
            string title = _messageType == MessageType.Error ? "Action Failed" : "Action Result";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            Rect rect = GUILayoutUtility.GetRect(0f, 68f, GUILayout.ExpandWidth(true));
            rect.width -= 16f;
            DrawCardBackground(rect, accentColor);

            Rect titleRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 18f);
            Rect messageRect = new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 24f);

            _messageStyle.normal.textColor = accentColor;
            _messageDetailStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.16f, 0.18f, 0.20f);

            GUI.Label(titleRect, title, _messageStyle);
            GUI.Label(messageRect, _message, _messageDetailStyle);

            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCardBackground(Rect rect, Color accentColor)
        {
            Color backgroundColor = EditorGUIUtility.isProSkin ? CardBgDark : CardBgLight;
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor);
        }

        private void DrawSeparator()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            Rect rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            rect.width -= 16f;
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.48f, 0.52f, 0.58f, 0.35f)
                : new Color(0.55f, 0.58f, 0.62f, 0.28f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            _sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.15f, 0.15f, 0.15f);
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void TryStartLocalResourceServer()
        {
            _controller.EnsureServerDataDirectory();
            _controller.Port = _port;

            if (_controller.TryStart(out string errorMessage))
            {
                SetMessage($"Local resource server running at {_controller.BaseUrl}.", MessageType.Info);
                return;
            }

            SetMessage(
                string.IsNullOrEmpty(errorMessage) ? "Failed to start local resource server." : errorMessage,
                MessageType.Error);
        }

        private void SetMessage(string message, MessageType messageType)
        {
            _message = message;
            _messageType = messageType;
        }

        private static Color GetMessageColor(MessageType messageType)
        {
            return messageType switch
            {
                MessageType.Error => ErrorColor,
                MessageType.Warning => WarningColor,
                _ => SuccessColor,
            };
        }
    }
}
