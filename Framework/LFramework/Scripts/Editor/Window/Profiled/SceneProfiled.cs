/**

*********************************************************************
Author:              LFramework.Editor
CreateTime:          9:48:34

*********************************************************************
**/

using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class SceneProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;


        private SceneComponent _sceneComponent;

        internal override void Draw()
        {
            GetComponent(ref _sceneComponent);
            EditorGUILayout.LabelField("Loaded Scene Asset Names",
                GetSceneNameString(_sceneComponent.GetLoadedSceneAssetNames()));
            EditorGUILayout.LabelField("Loading Scene Asset Names",
                GetSceneNameString(_sceneComponent.GetLoadingSceneAssetNames()));
            EditorGUILayout.LabelField("Unloading Scene Asset Names",
                GetSceneNameString(_sceneComponent.GetUnloadingSceneAssetNames()));
            EditorGUILayout.ObjectField("Main Camera", _sceneComponent.MainCamera, typeof(Camera), true);
        }

        private string GetSceneNameString(string[] sceneAssetNames)
        {
            if (sceneAssetNames == null || sceneAssetNames.Length <= 0)
            {
                return "<Empty>";
            }

            string sceneNameString = string.Empty;
            foreach (string sceneAssetName in sceneAssetNames)
            {
                if (!string.IsNullOrEmpty(sceneNameString))
                {
                    sceneNameString += ", ";
                }

                sceneNameString += SceneComponent.GetSceneName(sceneAssetName);
            }

            return sceneNameString;
        }
    }
}