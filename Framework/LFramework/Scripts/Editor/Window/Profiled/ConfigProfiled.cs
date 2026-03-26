
/**

*********************************************************************
Author:              LFramework.Editor
CreateTime:          21:46:13

*********************************************************************
**/

using UnityEditor;
using UnityGameFramework.Runtime;
using LFramework.Editor;

namespace LFramework.Editor.Window
{
    internal sealed class ConfigProfiled : ProfiledBase
    {
        internal override bool CanDraw => true;


        private ConfigComponent _configComponent;
        
        
        internal override void Draw()
        {
            GetComponent(ref _configComponent);
            if (_configComponent == null)
            {
                EditorGUILayout.HelpBox("ConfigComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Config Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Config Count", _configComponent.Count.ToString());
            EditorGUILayout.LabelField("Cached Bytes Size", _configComponent.CachedBytesSize.ToString());
            EditorGUILayout.EndVertical();
        }
    }
}
