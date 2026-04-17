using System;

namespace LFramework.Runtime.Settings
{
    [Serializable]
    public class RoutingSettings
    {
        public bool enableRouteIndex = true;
        public string routeIndexAddress;
        public string routeIndexPackageId;
        public bool enableConventionFallback;
        public bool allowDefaultPackageFallback = true;
        public bool detectDuplicateAddress = true;
    }
}
