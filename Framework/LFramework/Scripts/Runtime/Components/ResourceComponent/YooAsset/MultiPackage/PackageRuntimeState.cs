using System;

namespace LFramework.Runtime
{
    /// <summary>
    /// Tracks the runtime status of a single physical YooAsset package.
    /// </summary>
    public sealed class PackageRuntimeState
    {
        public string PackageName { get; set; }
        public string LogicalPackageId { get; set; }
        public bool IsInitializing { get; set; }
        public bool IsInitialized { get; set; }
        public bool ManifestUpdated { get; set; }
        public bool DownloadCompleted { get; set; }
        public string LastError { get; set; }
        public DateTime LastRouteIndexRefreshUtc { get; set; }

        public PackageRuntimeState Clone()
        {
            return new PackageRuntimeState
            {
                PackageName = PackageName,
                LogicalPackageId = LogicalPackageId,
                IsInitializing = IsInitializing,
                IsInitialized = IsInitialized,
                ManifestUpdated = ManifestUpdated,
                DownloadCompleted = DownloadCompleted,
                LastError = LastError,
                LastRouteIndexRefreshUtc = LastRouteIndexRefreshUtc
            };
        }
    }
}
