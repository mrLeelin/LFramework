using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class EditorResourceProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private EditorResourceComponent _editorResourceComponent;
        
        internal override void Draw()
        {
           GetComponent(ref _editorResourceComponent);
           EditorGUILayout.LabelField("Load Waiting Asset Count", _editorResourceComponent.LoadWaitingAssetCount.ToString());
        }
    }
}