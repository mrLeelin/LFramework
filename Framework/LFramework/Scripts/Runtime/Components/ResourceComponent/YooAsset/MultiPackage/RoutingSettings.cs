using System;

namespace LFramework.Runtime.Settings
{
    [Serializable]
    public class RoutingSettings
    {
        public const string DefaultRouteIndexAddress = "route-index";
        public const string DefaultRouteIndexPackageId = "";
        public const string DefaultRouteIndexAssetPath = "Assets/Framework/Generated/RouteIndex.asset";

        public bool enableRouteIndex = true;
        public string routeIndexAddress = DefaultRouteIndexAddress;
        public string routeIndexPackageId = DefaultRouteIndexPackageId;
        public string routeIndexAssetPath = DefaultRouteIndexAssetPath;
        public bool allowDefaultPackageFallback = true;
        public bool detectDuplicateAddress = true;
    }
}
