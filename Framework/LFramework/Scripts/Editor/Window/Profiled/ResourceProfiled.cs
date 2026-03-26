using GameFramework.Resource;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ResourceProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private ResourceComponent _resourceComponent;

        internal override void Draw()
        {
            GetComponent(ref _resourceComponent);

            DrawMetricCards(
                new ProfiledMetric("Resource Mode", _resourceComponent.ResourceMode.ToString(), "Active backend"),
                new ProfiledMetric("Resource Helper", _resourceComponent.ResourceHelperTypeName ?? "N/A", "Runtime bridge"),
                new ProfiledMetric("Min Unload", $"{_resourceComponent.MinUnloadInterval:F1}s", "Release cadence"),
                new ProfiledMetric("Max Unload", $"{_resourceComponent.MaxUnloadInterval:F1}s", "Release cadence"));

            if (_resourceComponent.ResourceMode == ResourceMode.YooAsset)
            {
                DrawSection(
                    "YooAsset Runtime",
                    "Current package and play mode used by the resource component while the YooAsset backend is active.",
                    () =>
                    {
                        DrawKeyValueRow("Package Name", _resourceComponent.YooAssetPackageName ?? "N/A");
                        DrawKeyValueRow("Play Mode", _resourceComponent.YooAssetsPlayModel.ToString());
                    });
            }
        }
    }
}
