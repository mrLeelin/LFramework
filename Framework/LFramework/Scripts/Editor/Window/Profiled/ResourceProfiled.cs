using GameFramework.Resource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;
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
                ResourceComponentSetting setting = SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
                string channel = SettingManager.GetSetting<GameSetting>()?.channel ?? string.Empty;
                string packageName = YooAssetMultiPackageUtility.ResolveDefaultPackageName(setting, Application.platform, channel);
                string routeIndexPackageName = YooAssetMultiPackageUtility.ResolveRouteIndexPackageName(setting, Application.platform, channel);
                string playMode = setting?.GetPackageDefinition(setting.GetResolvedDefaultPackageId())?.playModeOverride.ToString() ?? "N/A";

                DrawSection(
                    "YooAsset Runtime",
                    "Resolved default package and route-index package used by the multi-package runtime.",
                    () =>
                    {
                        DrawKeyValueRow("Default Package", packageName ?? "N/A");
                        DrawKeyValueRow("RouteIndex Package", routeIndexPackageName ?? "N/A");
                        DrawKeyValueRow("Play Mode", playMode);
                    });
            }
        }
    }
}
