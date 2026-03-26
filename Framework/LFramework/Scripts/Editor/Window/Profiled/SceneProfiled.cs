using System.Linq;
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

            string[] loadedSceneNames = ConvertSceneNames(_sceneComponent.GetLoadedSceneAssetNames());
            string[] loadingSceneNames = ConvertSceneNames(_sceneComponent.GetLoadingSceneAssetNames());
            string[] unloadingSceneNames = ConvertSceneNames(_sceneComponent.GetUnloadingSceneAssetNames());

            DrawMetricCards(
                new ProfiledMetric("Loaded Scenes", loadedSceneNames.Length.ToString(), "Active scenes"),
                new ProfiledMetric("Loading Scenes", loadingSceneNames.Length.ToString(), "Pending load"),
                new ProfiledMetric("Unloading Scenes", unloadingSceneNames.Length.ToString(), "Pending unload"),
                new ProfiledMetric(
                    "Main Camera",
                    _sceneComponent.MainCamera != null ? _sceneComponent.MainCamera.name : "None",
                    _sceneComponent.MainCamera != null ? "Camera reference" : "No active camera"));

            DrawSection(
                "Scene Queues",
                "Readable view of the scene load and unload queues tracked by the runtime scene component.",
                () =>
                {
                    DrawKeyValueRow("Loaded", ProfiledTextFormatter.JoinOrFallback(loadedSceneNames, "<Empty>"));
                    DrawKeyValueRow("Loading", ProfiledTextFormatter.JoinOrFallback(loadingSceneNames, "<Empty>"));
                    DrawKeyValueRow("Unloading", ProfiledTextFormatter.JoinOrFallback(unloadingSceneNames, "<Empty>"));
                });
        }

        private static string[] ConvertSceneNames(string[] sceneAssetNames)
        {
            if (sceneAssetNames == null || sceneAssetNames.Length == 0)
            {
                return new string[0];
            }

            return sceneAssetNames
                .Select(SceneComponent.GetSceneName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
        }
    }
}
