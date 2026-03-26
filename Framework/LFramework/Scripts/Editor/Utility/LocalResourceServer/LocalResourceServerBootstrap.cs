using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    [InitializeOnLoad]
    internal static class LocalResourceServerBootstrap
    {
        static LocalResourceServerBootstrap()
        {
            EditorApplication.delayCall += RestoreServerIfNeeded;
        }

        private static void RestoreServerIfNeeded()
        {
            var controller = new LocalResourceServerController();
            if (!controller.ShouldRestoreAfterReload || controller.IsRunning)
            {
                return;
            }

            if (!controller.TryStart(out string errorMessage))
            {
                Debug.LogWarning($"Failed to restore Local Resource Server after editor reload: {errorMessage}");
            }
        }
    }
}
