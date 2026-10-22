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

        // 类型必须与 UnityGameFramework ResourceComponent.m_CustomResourceHelper（ResourceHelperBase）保持一致，
        // 否则设置字段与组件字段类型不相等，映射时会被静默跳过，自定义 Resource Helper 永远不会生效。
        [SerializeField]
        private ResourceHelperBase m_CustomResourceHelper = null;

        [BoxGroup("Unload")]
        [LabelText("Min Unload Interval Seconds")]
        [SerializeField]
        private float _minUnloadInterval = 60f;

        [BoxGroup("Unload")]
        [LabelText("Max Unload Interval Seconds")]
        [SerializeField]
        private float _maxUnloadInterval = 300f;
        
        // 以下字段由 Resource Helper 通过 SettingManager.GetProjectSelector().GetComponentSetting<ResourceComponentSetting>()
        // 直接读取，不属于组件参数映射范围，因此显式忽略。
        [BoxGroup("YooAsset")]
        [LabelText("Default Package Id")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private string _defaultPackageId;

        [BoxGroup("YooAsset")]
        [LabelText("Packages")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private List<PackageDefinition> _yooAssetPackages = new List<PackageDefinition>();

        [BoxGroup("YooAsset")]
        [LabelText("Routing")]
        [ShowIf("_resourceMode", ResourceMode.YooAsset)]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private RoutingSettings _routing = new RoutingSettings();

        [BoxGroup("Addressable")]
        [LabelText("Hotfix Profile Name")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private string _hotfixProfileName;

        [BoxGroup("Addressable")]
        [LabelText("Force Single Slash Urls")]
        [ShowIf("_resourceMode", ResourceMode.Addressable)]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private bool _forceSingleSlashUrls;

        [BoxGroup("Addressable")]
        [LabelText("Log Load Urls")]
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private bool _logLoadUrls;

        public string HotfixProfileName => _hotfixProfileName;

        public bool ForceSingleSlashUrls => _forceSingleSlashUrls;

        public bool LogLoadUrls => _logLoadUrls;

        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// Gets the physical YooAsset package name resolved from the current default logical package.
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
            if (effectivePackages.Count == 0)
            {
                errors.Add("At least one package definition is required for YooAsset multi-package mode.");
                return false;
            }

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
