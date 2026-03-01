using GameFramework.Resource;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    /// <summary>
    /// 资源组件性能分析面板
    /// </summary>
    internal sealed class ResourceProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private ResourceComponent _resourceComponent;

        internal override void Draw()
        {
            GetComponent(ref _resourceComponent);

            EditorGUILayout.LabelField("Resource Mode", _resourceComponent.ResourceMode.ToString());
            EditorGUILayout.LabelField("Resource Helper", _resourceComponent.ResourceHelperTypeName ?? "N/A");
            EditorGUILayout.LabelField("Agent Helper", _resourceComponent.LoadResourceAgentHelperTypeName ?? "N/A");
            EditorGUILayout.LabelField("Agent Helper Count", _resourceComponent.LoadResourceAgentHelperCount.ToString());
            EditorGUILayout.LabelField("Min Unload Interval", $"{_resourceComponent.MinUnloadInterval:F1}s");
            EditorGUILayout.LabelField("Max Unload Interval", $"{_resourceComponent.MaxUnloadInterval:F1}s");

            if (_resourceComponent.ResourceMode == ResourceMode.YooAsset)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("YooAsset Settings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Package Name", _resourceComponent.YooAssetPackageName ?? "N/A");
                EditorGUILayout.LabelField("Play Mode", _resourceComponent.YooAssetPlayMode.ToString());
                EditorGUILayout.LabelField("Host Server URL", _resourceComponent.YooAssetHostServerUrl ?? "N/A");
                EditorGUILayout.LabelField("Fallback Server URL", _resourceComponent.YooAssetFallbackHostServerUrl ?? "N/A");
            }
        }
    }
}
