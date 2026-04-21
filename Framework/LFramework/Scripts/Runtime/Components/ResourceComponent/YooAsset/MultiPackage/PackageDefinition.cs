using System;
using System.Collections.Generic;
using GameFramework.Resource;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [Serializable]
    public class PackageDefinition
    {
        public string packageId;
        public string yooPackageName;
        public YooAssetPlayMode playModeOverride = YooAssetPlayMode.EditorSimulateMode;
        public int routePriority;
        public bool initOnLaunch = true;
        public bool updateManifestOnLaunch = true;
        public bool downloadOnLaunch = true;
        public string fallbackPackageId;
        public List<string> platformFilter = new List<string>();
        public List<string> channelFilter = new List<string>();
        public PackageDefinition Clone()
        {
            return new PackageDefinition
            {
                packageId = packageId,
                yooPackageName = yooPackageName,
                playModeOverride = playModeOverride,
                routePriority = routePriority,
                initOnLaunch = initOnLaunch,
                updateManifestOnLaunch = updateManifestOnLaunch,
                downloadOnLaunch = downloadOnLaunch,
                fallbackPackageId = fallbackPackageId,
                platformFilter = new List<string>(platformFilter ?? new List<string>()),
                channelFilter = new List<string>(channelFilter ?? new List<string>()),
            };
        }
    }
}
