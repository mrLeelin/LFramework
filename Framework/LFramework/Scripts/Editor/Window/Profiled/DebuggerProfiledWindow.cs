using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DebuggerProfiledWindow : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private DebuggerComponent _debuggerComponent;

        internal override void Draw()
        {
            GetComponent(ref _debuggerComponent);
            bool activeWindow = EditorGUILayout.Toggle("Active Window", _debuggerComponent.ActiveWindow);
            if (activeWindow != _debuggerComponent.ActiveWindow)
            {
                _debuggerComponent.ActiveWindow = activeWindow;
            }

            if (GUILayout.Button("Reset Layout"))
            {
                _debuggerComponent.ResetLayout();
            }
        }
    }
}