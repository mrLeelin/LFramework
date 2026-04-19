using System.Collections.Generic;
using GameFramework.Resource;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Resource component configuration for Addressables and YooAssets.
    /// This asset is the configuration source only. Runtime-effective package data is derived from it.
    /// </summary>
    [CreateAssetMenu(order = 1, fileName = "ResourceComponentSetting",
        menuName = "LFramework/Settings/ResourceComponentSetting")]
    public class ResourceComponentSetting : ComponentSetting
    {
        [BoxGroup("General")]
        [LabelText("Resource Mode")]
        [SerializeField]
        private ResourceMode _resourceMode = ResourceMode.YooAsset;

        [BoxGroup("General")]
        [LabelText("Resource Helper Type")]
        [SerializeField]
        private string m_ResourceHelperTypeName = "LFramework.Runtime.YooAssetResourceHelper";

        [SerializeField]
        private SettingHelperBase m_CustomResourceHelper = null;

        [BoxGroup("Unload")]
        [LabelText("Min Unload Interval Seconds")]
        [SerializeField]
        private float _minUnloadInterval = 60f;

        [BoxGroup("Unload")]
        [LabelText("Max Unload Interval Seconds")]
        [SerializeField]
        private float _maxUnloadInterval = 300f;
        
        [BoxGroup("YooAsset")]
        [LabelText("Default Package Id")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private string _defaultPackageId;

        [BoxGroup("YooAsset")]
        [LabelText("Packages")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private List<PackageDefinition> _yooAssetPackages = new List<PackageDefinition>();

        [BoxGroup("YooAsset")]
        [LabelText("Routing")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [SerializeField]
        private RoutingSettings _routing = new RoutingSettings();

        [BoxGroup("Addressable")]
        [LabelText("Hotfix Profile Name")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        [SerializeField]
        private string _hotfixProfileName;

        public string HotfixProfileName => _hotfixProfileName;

        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// Legacy single-package compatibility entry.
        /// Maps to the resolved default physical package when multi-package config is present.
        /// </summary>
        public string YooAssetPackageName
        {
            get
            {
                PackageDefinition defaultPackage = GetPackageDefinition(GetResolvedDefaultPackageId());
                if (defaultPackage != null && !string.IsNullOrWhiteSpace(defaultPackage.yooPackageName))
                {
                    return defaultPackage.yooPackageName;
                }
                Log.Fatal("The get YooAssetPackageName is null .");
                return null;
            }
        }

        /// <summary>
        /// Raw configured package definitions. This is editor-owned configuration data.
        /// </summary>
        public List<PackageDefinition> YooAssetPackages => _yooAssetPackages;

        public RoutingSettings YooAssetRouting
        {
            get
            {
                _routing ??= new RoutingSettings();
                return _routing;
            }
        }

        public string DefaultPackageId => GetResolvedDefaultPackageId();

        public IReadOnlyList<PackageDefinition> GetEffectivePackageDefinitions()
        {
            var result = new List<PackageDefinition>();
            if (_resourceMode != ResourceMode.YooAsset)
            {
                return result;
            }

            foreach (PackageDefinition package in _yooAssetPackages)
            {
                if (package == null)
                {
                    continue;
                }

                result.Add(package.Clone());
            }
            
            return result;
        }

        public string GetResolvedDefaultPackageId()
        {
            if (_resourceMode != ResourceMode.YooAsset)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(_defaultPackageId))
            {
                return _defaultPackageId;
            }

            IReadOnlyList<PackageDefinition> packages = GetEffectivePackageDefinitions();
            if (packages.Count > 0)
            {
                return packages[0].packageId;
            }

            Log.Fatal("The none resolve default package count is zero. please check and add [PackageDefinition]");
            return null;
        }

        public string GetResolvedRouteIndexPackageId()
        {
            if (_resourceMode != ResourceMode.YooAsset)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(YooAssetRouting.routeIndexPackageId))
            {
                return YooAssetRouting.routeIndexPackageId;
            }

            return GetResolvedDefaultPackageId();
        }

        public PackageDefinition GetPackageDefinition(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return null;
            }

            IReadOnlyList<PackageDefinition> packages = GetEffectivePackageDefinitions();
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i] != null && packages[i].packageId == packageId)
                {
                    return packages[i];
                }
            }

            return null;
        }

        public bool ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            if (_resourceMode != ResourceMode.YooAsset)
            {
                return true;
            }

            IReadOnlyList<PackageDefinition> effectivePackages = GetEffectivePackageDefinitions();

            var seenPackageIds = new HashSet<string>();
            for (int i = 0; i < effectivePackages.Count; i++)
            {
                PackageDefinition package = effectivePackages[i];
                if (package == null)
                {
                    errors.Add("Package definition cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(package.packageId))
                {
                    errors.Add("Package definition is missing packageId.");
                    continue;
                }

                if (!seenPackageIds.Add(package.packageId))
                {
                    errors.Add($"Duplicate packageId detected: {package.packageId}");
                }

                if (string.IsNullOrWhiteSpace(package.yooPackageName))
                {
                    errors.Add($"Package '{package.packageId}' is missing yooPackageName.");
                }

                if (!string.IsNullOrWhiteSpace(package.fallbackPackageId) &&
                    !HasPackageDefinition(effectivePackages, package.fallbackPackageId))
                {
                    errors.Add($"Package '{package.packageId}' references missing fallback package '{package.fallbackPackageId}'.");
                }
            }

            ValidatePackageReference(GetResolvedDefaultPackageId(), "default", effectivePackages, errors);
            if (YooAssetRouting.enableRouteIndex)
            {
                if (string.IsNullOrWhiteSpace(YooAssetRouting.routeIndexAddress))
                {
                    warnings.Add("Route index address is missing while route-index routing is enabled.");
                }

                if (string.IsNullOrWhiteSpace(YooAssetRouting.routeIndexAssetPath))
                {
                    warnings.Add("Route index asset path is missing while route-index routing is enabled.");
                }

                ValidatePackageReference(GetResolvedRouteIndexPackageId(), "route index", effectivePackages, errors);
            }

            return errors.Count == 0;
        }
        

        private static bool HasPackageDefinition(IReadOnlyList<PackageDefinition> packages, string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return false;
            }

            for (int i = 0; i < packages.Count; i++)
            {
                PackageDefinition package = packages[i];
                if (package != null && package.packageId == packageId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidatePackageReference(string packageId, string referenceName,
            IReadOnlyList<PackageDefinition> packages, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                errors.Add($"The {referenceName} package reference is empty.");
                return;
            }

            if (!HasPackageDefinition(packages, packageId))
            {
                errors.Add($"The {referenceName} package reference '{packageId}' does not exist.");
            }
        }
    }
}
