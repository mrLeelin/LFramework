using LFramework.Editor;
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
            if (_sceneComponent == null)
            {
                EditorGUILayout.HelpBox("SceneComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Scene Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Loaded Scene Assets", GetSceneNameString(_sceneComponent.GetLoadedSceneAssetNames()));
            EditorGUILayout.LabelField("Loading Scene Assets", GetSceneNameString(_sceneComponent.GetLoadingSceneAssetNames()));
            EditorGUILayout.LabelField("Unloading Scene Assets", GetSceneNameString(_sceneComponent.GetUnloadingSceneAssetNames()));
            EditorGUILayout.ObjectField("Main Camera", _sceneComponent.MainCamera, typeof(Camera), true);
            EditorGUILayout.EndVertical();
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
