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

            DrawMetricCards(
                new ProfiledMetric("Config Count", _configComponent.Count.ToString(), "Loaded config assets"),
                new ProfiledMetric("Cached Bytes", _configComponent.CachedBytesSize.ToString(), "Runtime cache footprint"));
        }
    }
}
