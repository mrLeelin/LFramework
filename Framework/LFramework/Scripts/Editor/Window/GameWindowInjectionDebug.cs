using LFramework.Editor.Windows;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// Embeds the full Injection debug UI inside GameWindow.
    /// </summary>
    public sealed class GameWindowInjectionDebug
    {
        private Vector2 _scrollPosition;
        private InjectionDebugWindow _embeddedWindow;

        [OnInspectorGUI]
        private void DrawPage()
        {
            EnsureEmbeddedWindow();
            _embeddedWindow.UpdateEmbeddedSnapshot();

            GameWindowChrome.BeginPage(ref _scrollPosition);
            GameWindowChrome.DrawHeader(
                "Injection Debug",
                "Inspect LFramework services, generated injectors, scopes, validation, and performance without opening a second window.",
                new GameWindowBadge("Host", "GameWindow"),
                new GameWindowBadge("Mode", EditorApplication.isPlaying ? "Play Mode" : "Edit Mode"),
                new GameWindowBadge("Scan", "Manual"));
            GameWindowChrome.DrawSeparator();
            GUILayout.Space(12f);

            DrawOverviewCards();

            GameWindowChrome.DrawSectionHeader(
                "Debug Panel",
                "The full Injection debug surface is embedded below; the Rescan button is still explicit to avoid heavy reflection work during normal navigation.");
            GameWindowChrome.BeginContentCard();
            _embeddedWindow.DrawEmbedded();
            GameWindowChrome.EndContentCard();
            GameWindowChrome.EndPage();
        }

        private void DrawOverviewCards()
        {
            var issueColor = _embeddedWindow.ValidationErrorCount > 0
                ? GameWindowChrome.ErrorColor
                : _embeddedWindow.ValidationIssueCount > 0
                    ? GameWindowChrome.WarningColor
                    : GameWindowChrome.SuccessColor;

            GameWindowChrome.DrawStatCards(
                new GameWindowStatCard(
                    "Services",
                    _embeddedWindow.ServiceCount.ToString(),
                    "Registered services in the current root scope.",
                    GameWindowChrome.AccentColor),
                new GameWindowStatCard(
                    "Injectors",
                    _embeddedWindow.InjectorCount.ToString(),
                    "Generated or dynamic injector entries currently known.",
                    GameWindowChrome.SuccessColor),
                new GameWindowStatCard(
                    "Inject Points",
                    _embeddedWindow.InjectPointCount.ToString(),
                    "Updated only when the manual Rescan action is used.",
                    GameWindowChrome.WarningColor),
                new GameWindowStatCard(
                    "Scopes",
                    _embeddedWindow.ScopeCount.ToString(),
                    "Root and child service scopes visible to the debugger.",
                    GameWindowChrome.AccentColor),
                new GameWindowStatCard(
                    "Issues",
                    _embeddedWindow.ValidationIssueCount.ToString(),
                    "Validation warnings and errors from the embedded panel.",
                    issueColor));
        }

        private void EnsureEmbeddedWindow()
        {
            if (_embeddedWindow != null)
            {
                return;
            }

            _embeddedWindow = ScriptableObject.CreateInstance<InjectionDebugWindow>();
            _embeddedWindow.hideFlags = HideFlags.HideAndDontSave;
            _embeddedWindow.MarkEmbeddedHost();
            InjectionDebugWindow.CloseStandaloneWindows();
        }
    }
}
