
/**

*********************************************************************
Author:              LFramework.Editor
CreateTime:          21:46:13

*********************************************************************
**/

using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ConfigProfiled : ProfiledBase
    {
        internal override bool CanDraw => true;


        private ConfigComponent _configComponent;
        
        
        internal override void Draw()
        {
            GetComponent(ref _configComponent);
            EditorGUILayout.LabelField("Config Count", _configComponent.Count.ToString());
            EditorGUILayout.LabelField("Cached Bytes Size", _configComponent.CachedBytesSize.ToString());
        }
    }
}