using GameFramework.Resource;
using LFramework.Editor;
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
            if (_resourceComponent == null)
            {
                EditorGUILayout.HelpBox("ResourceComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Resource Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Resource Mode", _resourceComponent.ResourceMode.ToString());
            EditorGUILayout.LabelField("Resource Helper", _resourceComponent.ResourceHelperTypeName ?? "N/A");
            EditorGUILayout.LabelField("Min Unload Interval", $"{_resourceComponent.MinUnloadInterval:F1}s");
            EditorGUILayout.LabelField("Max Unload Interval", $"{_resourceComponent.MaxUnloadInterval:F1}s");
            EditorGUILayout.EndVertical();

            if (_resourceComponent.ResourceMode == ResourceMode.YooAsset)
            {
                EditorGUILayout.Space();
                GameWindowChrome.DrawCompactHeader("YooAsset Settings");
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Package Name", _resourceComponent.YooAssetPackageName ?? "N/A");
                EditorGUILayout.LabelField("Play Mode", _resourceComponent.YooAssetsPlayModel.ToString());
                EditorGUILayout.EndVertical();
            }
        }
    }
}
