

using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class SoundProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private SoundComponent _soundComponent;
        
        internal override void Draw()
        {
            GetComponent(ref _soundComponent);
            
            EditorGUILayout.LabelField("Sound Group Count", _soundComponent.SoundGroupCount.ToString());
        }
    }
}