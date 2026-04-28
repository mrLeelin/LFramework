using System.Collections.Generic;

namespace LFramework.Runtime
{
    /// <summary>
    /// Captures how an address resolved to a final logical package id.
    /// </summary>
    public sealed class PackageRouteResolutionResult
    {
        private readonly List<string> _fallbackChain = new List<string>();

        public string RequestedAddress { get; set; }
        public string ExplicitPackageId { get; set; }
        public string DefaultPackageId { get; set; }
        public string RouteIndexPackageId { get; set; }
        public string FinalPackageId { get; set; }
        public bool UsedExplicitPackageId { get; set; }
        public bool UsedRouteIndex { get; set; }
        public bool UsedDefaultPackage { get; set; }
        public bool UsedFallback { get; set; }
        public IReadOnlyList<string> FallbackChain => _fallbackChain;

        internal List<string> MutableFallbackChain => _fallbackChain;
    }
}
