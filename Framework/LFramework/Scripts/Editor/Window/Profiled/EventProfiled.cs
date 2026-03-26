using LFramework.Editor;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class EventProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private EventComponent _eventComponent;

        internal override void Draw()
        {
            GetComponent(ref _eventComponent);
            if (_eventComponent == null)
            {
                EditorGUILayout.HelpBox("EventComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Event Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Event Handler Count", _eventComponent.EventHandlerCount.ToString());
            EditorGUILayout.LabelField("Event Count", _eventComponent.EventCount.ToString());
            EditorGUILayout.EndVertical();
        }
    }
}
