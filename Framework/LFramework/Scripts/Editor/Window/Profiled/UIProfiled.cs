

using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class UIProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private UIComponent _uiComponent;
        
        internal override void Draw()
        {
            GetComponent(ref _uiComponent);
            EditorGUILayout.LabelField("UI Group Count", _uiComponent.UIGroupCount.ToString());
        }
    }
}