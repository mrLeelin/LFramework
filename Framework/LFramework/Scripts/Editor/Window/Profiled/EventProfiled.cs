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
            EditorGUILayout.LabelField("Event Handler Count", _eventComponent.EventHandlerCount.ToString());
            EditorGUILayout.LabelField("Event Count", _eventComponent.EventCount.ToString());
        }
    }
}