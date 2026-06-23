#if YOOASSET_SUPPORT
using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using GameFramework.Resource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;
using YooAsset;

namespace LFramework.Editor.Tests.Settings
{
    public class ResourceComponentMultiPackageTests
    {
        [Test]
        public void EffectivePackages_ReturnEmpty_WhenNoPackagesAreConfigured()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            Assert.That(setting.YooAssetPackages, Is.Empty);

            IReadOnlyList<PackageDefinition> effectivePackages = setting.GetEffectivePackageDefinitions();

            Assert.That(effectivePackages, Is.Empty);
            Assert.That(setting.YooAssetPackages, Is.Empty);
        }

        [Test]
        public void EffectivePackages_ReturnClones_NotLiveConfigurationReferences()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage"
            });

            IReadOnlyList<PackageDefinition> firstSnapshot = setting.GetEffectivePackageDefinitions();
            firstSnapshot[0].yooPackageName = "MutatedPackage";

            IReadOnlyList<PackageDefinition> secondSnapshot = setting.GetEffectivePackageDefinitions();

            Assert.That(secondSnapshot[0].yooPackageName, Is.EqualTo("UIPackage"));
            Assert.That(setting.YooAssetPackages[0].yooPackageName, Is.EqualTo("UIPackage"));
        }

        [Test]
        public void ResolvedDefaultAndRouteIndexPackageIds_UseConfiguredPackages_WhenExplicitIdsAreEmpty()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "base",
                yooPackageName = "BasePackage"
            });

            Assert.That(setting.GetResolvedDefaultPackageId(), Is.EqualTo("base"));
            Assert.That(setting.GetResolvedRouteIndexPackageId(), Is.EqualTo("base"));
        }

        [Test]
        public void RoutingSettings_ProvideExpectedDefaults()
        {
            var routing = new RoutingSettings();

            Assert.That(routing.enableRouteIndex, Is.True);
            Assert.That(routing.routeIndexAddress, Is.EqualTo("route-index"));
            Assert.That(routing.routeIndexPackageId, Is.Empty);
            Assert.That(routing.routeIndexAssetPath, Is.EqualTo("Assets/Framework/Generated/RouteIndex.asset"));
            Assert.That(routing.allowDefaultPackageFallback, Is.True);
            Assert.That(routing.detectDuplicateAddress, Is.True);
        }

        [Test]
        public void YooAssetPackageName_UsesResolvedDefaultPackageDefinition()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "ui");
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage"
            });

            Assert.That(setting.YooAssetPackageName, Is.EqualTo("UIPackage"));
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsDuplicatePackageIds()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage_Override" });

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "duplicate")), Is.True);
            Assert.That(warnings, Is.Not.Null);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsEmptyPackageConfiguration()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "package")), Is.True);
            Assert.That(warnings, Is.Not.Null);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_WarnsWhenRouteIndexAssetPathIsMissing()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "bootstrap",
                yooPackageName = "BootstrapPackage"
            });
            setting.YooAssetRouting.routeIndexPackageId = "bootstrap";
            setting.YooAssetRouting.routeIndexAddress = "route-index";
            setting.YooAssetRouting.routeIndexAssetPath = string.Empty;

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(warnings.Exists(message => Contains(message, "asset path")), Is.True);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsUnknownDefaultAndRouteIndexPackages()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "missing-default");
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage" });
            setting.YooAssetRouting.routeIndexPackageId = "missing-route";
            setting.YooAssetRouting.routeIndexAddress = "route-index";

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "default")), Is.True);
            Assert.That(errors.Exists(message => Contains(message, "route index")), Is.True);
            Assert.That(warnings, Is.Not.Null);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsUnknownFallbackPackageReferences()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage",
                fallbackPackageId = "missing-fallback"
            });

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out _);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "fallback")), Is.True);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_IsNoOp_WhenResourceModeIsAddressable()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_resourceMode", ResourceMode.Addressable);

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void ResourceComponentSetting_DisablesLoadUrlLoggingByDefault()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            Assert.That(setting.LogLoadUrls, Is.False);
        }

        [Test]
        public void AddressableResourceHelper_BuildLoadUrlLogMessage_IncludesLookupFields()
        {
            Type helperType = Type.GetType(
                "LFramework.Runtime.AddressableResourceHelper, LFramework.Runtime");
            Assert.That(helperType, Is.Not.Null, "Expected AddressableResourceHelper type to exist in LFramework.Runtime.");

            MethodInfo method = helperType.GetMethod(
                "BuildLoadUrlLogMessage",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected Addressables URL logging message builder.");

            object result = method.Invoke(
                null,
                new object[]
                {
                    "ModelHero",
                    typeof(object),
                    "remote_Android/model.bundle",
                    "https://cdn.example.com/1.0.0/model.bundle",
                    "UserScript.LoadModel"
                });

            string message = (string)result;
            Assert.That(message, Does.Contain("[ResourceLoadUrl]"));
            Assert.That(message, Does.Contain("Backend: Addressables"));
            Assert.That(message, Does.Contain("PrimaryKey: ModelHero"));
            Assert.That(message, Does.Contain("ResourceType: Object"));
            Assert.That(message, Does.Contain("InternalId: remote_Android/model.bundle"));
            Assert.That(message, Does.Contain("Url: https://cdn.example.com/1.0.0/model.bundle"));
            Assert.That(message, Does.Contain("Stack: UserScript.LoadModel"));
        }

        [Test]
        public void DefaultRemoteServices_BuildLoadUrlLogMessage_IncludesYooAssetFields()
        {
            Type remoteServicesType = Type.GetType(
                "LFramework.Runtime.DefaultRemoteServices, LFramework.Runtime");
            Assert.That(remoteServicesType, Is.Not.Null, "Expected DefaultRemoteServices type to exist in LFramework.Runtime.");

            MethodInfo method = remoteServicesType.GetMethod(
                "BuildLoadUrlLogMessage",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected YooAsset URL logging message builder.");

            object result = method.Invoke(
                null,
                new object[]
                {
                    "Main",
                    "model.bundle",
                    "https://cdn.example.com/model.bundle",
                    "YooAsset.DownloadSystem"
                });

            string message = (string)result;
            Assert.That(message, Does.Contain("[ResourceLoadUrl]"));
            Assert.That(message, Does.Contain("Backend: YooAsset"));
            Assert.That(message, Does.Contain("RemoteKind: Main"));
            Assert.That(message, Does.Contain("FileName: model.bundle"));
            Assert.That(message, Does.Contain("Url: https://cdn.example.com/model.bundle"));
            Assert.That(message, Does.Contain("Stack: YooAsset.DownloadSystem"));
        }

        [Test]
        public void PackageRegistry_ActivatesMatchingDefinition_AndStoresAClone()
        {
            var windowsDefinition = new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Windows",
                routePriority = 10,
                platformFilter = new List<string> { RuntimePlatform.WindowsEditor.ToString() },
                channelFilter = new List<string> { "Google" }
            };
            var androidDefinition = new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Android",
                routePriority = 1,
                platformFilter = new List<string> { RuntimePlatform.Android.ToString() },
                channelFilter = new List<string> { "Google" }
            };
            var registry = new PackageRegistry();

            registry.Configure(new[] { windowsDefinition, androidDefinition }, RuntimePlatform.WindowsEditor, "Google");
            windowsDefinition.yooPackageName = "MutatedAfterConfigure";

            PackageDefinition activePackage = registry.GetPackage("ui");

            Assert.That(activePackage, Is.Not.Null);
            Assert.That(activePackage.yooPackageName, Is.EqualTo("UIPackage_Windows"));
        }

        [Test]
        public void PackageResolver_UsesExplicitOverrideBeforeRouteIndex()
        {
            var routing = new RoutingSettings { allowDefaultPackageFallback = true };
            var resolver = new PackageResolver(routing);
            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "ui" });

            resolver.LoadRouteIndex(routeIndex);

            Assert.That(resolver.ResolvePackageId("ui/home", "scene", "base"), Is.EqualTo("scene"));
            Assert.That(resolver.ResolvePackageId("ui/home", null, "base"), Is.EqualTo("ui"));
        }

        [Test]
        public void PackageResolver_Diagnostics_ReportExplicitOverrideSelection()
        {
            var routing = new RoutingSettings { allowDefaultPackageFallback = true };
            var resolver = new PackageResolver(routing);

            PackageRouteResolutionResult result = resolver.ResolveWithDiagnostics("ui/home", "scene", "base");

            Assert.That(result.RequestedAddress, Is.EqualTo("ui/home"));
            Assert.That(result.ExplicitPackageId, Is.EqualTo("scene"));
            Assert.That(result.FinalPackageId, Is.EqualTo("scene"));
            Assert.That(result.UsedExplicitPackageId, Is.True);
            Assert.That(result.UsedRouteIndex, Is.False);
            Assert.That(result.UsedFallback, Is.False);
        }

        [Test]
        public void PackageResolver_FallsBackToDefaultPackage_WhenRouteIndexMisses()
        {
            var routing = new RoutingSettings { allowDefaultPackageFallback = true };
            var resolver = new PackageResolver(routing);

            Assert.That(resolver.ResolvePackageId("unknown/address", null, "base"), Is.EqualTo("base"));
        }

        [Test]
        public void PackageResolver_UsesFallbackPackageChain_WhenRouteIndexTargetsInactivePackage()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition
                    {
                        packageId = "premium-ui",
                        yooPackageName = "PremiumUIPackage",
                        fallbackPackageId = "shared-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "shared-ui",
                        yooPackageName = "SharedUIPackage"
                    },
                    new PackageDefinition
                    {
                        packageId = "default-ui",
                        yooPackageName = "DefaultUIPackage"
                    }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "premium-ui" });

            var resolver = new PackageResolver(new RoutingSettings { allowDefaultPackageFallback = true }, registry);
            resolver.LoadRouteIndex(routeIndex);

            Assert.That(resolver.ResolvePackageId("ui/home", null, "default-ui"), Is.EqualTo("shared-ui"));
        }

        [Test]
        public void PackageResolver_Diagnostics_ReportRouteIndexAndFallbackChain()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition
                    {
                        packageId = "premium-ui",
                        yooPackageName = "PremiumUIPackage",
                        fallbackPackageId = "shared-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "shared-ui",
                        yooPackageName = "SharedUIPackage"
                    },
                    new PackageDefinition
                    {
                        packageId = "default-ui",
                        yooPackageName = "DefaultUIPackage"
                    }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "premium-ui" });

            var resolver = new PackageResolver(new RoutingSettings { allowDefaultPackageFallback = true }, registry);
            resolver.LoadRouteIndex(routeIndex);

            PackageRouteResolutionResult result = resolver.ResolveWithDiagnostics("ui/home", null, "default-ui");

            Assert.That(result.RouteIndexPackageId, Is.EqualTo("premium-ui"));
            Assert.That(result.FinalPackageId, Is.EqualTo("shared-ui"));
            Assert.That(result.UsedRouteIndex, Is.True);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.FallbackChain, Is.EqualTo(new[] { "premium-ui", "shared-ui" }));
        }

        [Test]
        public void PackageResolver_UsesLastResolvableFallback_WhenFallbackChainContainsMultipleInactivePackages()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition
                    {
                        packageId = "premium-ui",
                        yooPackageName = "PremiumUIPackage",
                        fallbackPackageId = "regional-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "regional-ui",
                        yooPackageName = "RegionalUIPackage",
                        fallbackPackageId = "shared-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "shared-ui",
                        yooPackageName = "SharedUIPackage"
                    },
                    new PackageDefinition
                    {
                        packageId = "default-ui",
                        yooPackageName = "DefaultUIPackage"
                    }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "premium-ui" });

            var resolver = new PackageResolver(new RoutingSettings { allowDefaultPackageFallback = true }, registry);
            resolver.LoadRouteIndex(routeIndex);

            Assert.That(resolver.ResolvePackageId("ui/home", null, "default-ui"), Is.EqualTo("shared-ui"));
        }

        [Test]
        public void PackageResolver_FallsBackToDefaultPackage_WhenFallbackChainCyclesWithoutResolvablePackage()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition
                    {
                        packageId = "premium-ui",
                        yooPackageName = "PremiumUIPackage",
                        fallbackPackageId = "regional-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "regional-ui",
                        yooPackageName = "RegionalUIPackage",
                        fallbackPackageId = "premium-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "default-ui",
                        yooPackageName = "DefaultUIPackage"
                    }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "premium-ui" });

            var resolver = new PackageResolver(new RoutingSettings { allowDefaultPackageFallback = true }, registry);
            resolver.LoadRouteIndex(routeIndex);

            Assert.That(resolver.ResolvePackageId("ui/home", null, "default-ui"), Is.EqualTo("default-ui"));
        }

        [Test]
        public void PackageResolver_Diagnostics_ReportDefaultFallbackAfterFallbackCycle()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition
                    {
                        packageId = "premium-ui",
                        yooPackageName = "PremiumUIPackage",
                        fallbackPackageId = "regional-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "regional-ui",
                        yooPackageName = "RegionalUIPackage",
                        fallbackPackageId = "premium-ui",
                        platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
                    },
                    new PackageDefinition
                    {
                        packageId = "default-ui",
                        yooPackageName = "DefaultUIPackage"
                    }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routeIndex = ScriptableObject.CreateInstance<RouteIndexAsset>();
            routeIndex.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "premium-ui" });

            var resolver = new PackageResolver(new RoutingSettings { allowDefaultPackageFallback = true }, registry);
            resolver.LoadRouteIndex(routeIndex);

            PackageRouteResolutionResult result = resolver.ResolveWithDiagnostics("ui/home", null, "default-ui");

            Assert.That(result.RouteIndexPackageId, Is.EqualTo("premium-ui"));
            Assert.That(result.FinalPackageId, Is.EqualTo("default-ui"));
            Assert.That(result.UsedRouteIndex, Is.True);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.UsedDefaultPackage, Is.True);
            Assert.That(result.FallbackChain, Is.EqualTo(new[] { "premium-ui", "regional-ui" }));
        }

        [Test]
        public void RouteIndexBootstrapLoader_LoadsRouteIndexDirectlyFromBootstrapPackage()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routing = new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            };
            var loader = new RouteIndexBootstrapLoader(registry, routing, "default");
            var expectedAsset = ScriptableObject.CreateInstance<RouteIndexAsset>();
            expectedAsset.entries.Add(new RouteIndexEntry { address = "ui/home", packageId = "ui" });

            string capturedPackageId = null;
            string capturedAddress = null;
            bool loaded = loader.TryLoad(
                (packageId, address) =>
                {
                    capturedPackageId = packageId;
                    capturedAddress = address;
                    return expectedAsset;
                },
                out RouteIndexAsset routeIndex,
                out string errorMessage);

            Assert.That(loaded, Is.True);
            Assert.That(errorMessage, Is.Null.Or.Empty);
            Assert.That(capturedPackageId, Is.EqualTo("bootstrap"));
            Assert.That(capturedAddress, Is.EqualTo("route-index"));
            Assert.That(routeIndex, Is.SameAs(expectedAsset));
        }

        [Test]
        public void RouteIndexBootstrapLoader_FallsBackToDefaultPackage_WhenRouteIndexPackageIdIsEmpty()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "default", yooPackageName = "DefaultPackage" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var loader = new RouteIndexBootstrapLoader(registry, new RoutingSettings
            {
                routeIndexPackageId = string.Empty,
                routeIndexAddress = "route-index"
            }, "default");

            bool ok = loader.TryGetBootstrapRequest(out string packageId, out string address, out string errorMessage);

            Assert.That(ok, Is.True);
            Assert.That(packageId, Is.EqualTo("default"));
            Assert.That(address, Is.EqualTo("route-index"));
            Assert.That(errorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void RouteIndexBootstrapLoader_ExposesBootstrapRequest_ForRuntimeAsyncLoading()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var loader = new RouteIndexBootstrapLoader(registry, new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            }, "default");

            bool ok = loader.TryGetBootstrapRequest(out string packageId, out string address, out string errorMessage);

            Assert.That(ok, Is.True);
            Assert.That(packageId, Is.EqualTo("bootstrap"));
            Assert.That(address, Is.EqualTo("route-index"));
            Assert.That(errorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void RouteIndexBootstrapLoader_ReturnsFailure_WhenLoaderThrows()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var loader = new RouteIndexBootstrapLoader(registry, new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            }, "default");

            bool loaded = loader.TryLoad(
                (_, __) => throw new InvalidOperationException("boom"),
                out RouteIndexAsset routeIndex,
                out string errorMessage);

            Assert.That(loaded, Is.False);
            Assert.That(routeIndex, Is.Null);
            Assert.That(Contains(errorMessage, "boom"), Is.True);
        }

        [Test]
        public void ResourceHelperBase_PackageAwareOverloads_FallbackToLegacyImplementations()
        {
            var helperGo = new GameObject("TestResourceHelper");
            try
            {
                var helper = helperGo.AddComponent<TestResourceHelper>();
                var assetCallbacks = new LoadAssetCallbacks(null);
                var sceneCallbacks = new LoadSceneCallbacks(null);
                var binaryCallbacks = new LoadBinaryCallbacks(null);

                helper.LoadAsset("ui/home", typeof(TextAsset), "ui", assetCallbacks, "asset-user");
                helper.LoadScene("scene/home", "scene", sceneCallbacks, "scene-user");
                helper.LoadBinary("config.bytes", "config", binaryCallbacks, "binary-user");
                helper.InstantiateAsset("ui/prefab", "ui", assetCallbacks, "instantiate-user");

                Assert.That(helper.LastAssetName, Is.EqualTo("ui/home"));
                Assert.That(helper.LastSceneName, Is.EqualTo("scene/home"));
                Assert.That(helper.LastBinaryName, Is.EqualTo("config.bytes"));
                Assert.That(helper.LastInstantiateName, Is.EqualTo("ui/prefab"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        [Test]
        public void ResourceManager_PackageAwareOverloads_ForwardPackageIdToHelper()
        {
            IResourceManager manager = CreateResourceManager();
            var helper = new TestResourceHelperProxy();
            var assetCallbacks = new LoadAssetCallbacks(null);
            var sceneCallbacks = new LoadSceneCallbacks(null);
            var binaryCallbacks = new LoadBinaryCallbacks(null);

            manager.SetResourceHelper(helper);
            manager.LoadAsset("ui/home", typeof(TextAsset), "ui", 0, assetCallbacks, "asset-user");
            manager.LoadScene("scene/home", "scene", 0, sceneCallbacks, "scene-user");
            manager.LoadBinary("config.bytes", "config", binaryCallbacks, "binary-user");
            manager.InstantiateAsset("ui/prefab", "ui", assetCallbacks, "instantiate-user");

            Assert.That(helper.LastPackageId, Is.EqualTo("ui"));
            Assert.That(helper.LastScenePackageId, Is.EqualTo("scene"));
            Assert.That(helper.LastBinaryPackageId, Is.EqualTo("config"));
            Assert.That(helper.LastInstantiatePackageId, Is.EqualTo("ui"));
        }

        [Test]
        public void ResourceComponent_PackageAwareOverloads_ForwardPackageIdToManager()
        {
            var componentGo = new GameObject("ResourceComponent");
            try
            {
                var component = (UnityGameFramework.Runtime.ResourceComponent)Activator.CreateInstance(typeof(UnityGameFramework.Runtime.ResourceComponent));
                SetPrivateField(component, "Parent", componentGo);
                component.AwakeComponent();
                var manager = new TestResourceManagerProxy();
                SetPrivateField(component, "_resourceManager", manager);
                var assetCallbacks = new LoadAssetCallbacks(null);
                var sceneCallbacks = new LoadSceneCallbacks(null);
                var binaryCallbacks = new LoadBinaryCallbacks(null);

                component.LoadAsset("ui/button", "ui", assetCallbacks);
                component.LoadAsset("ui/home", typeof(TextAsset), "ui", assetCallbacks, "asset-user");
                component.LoadScene("scene/lobby", "scene", sceneCallbacks);
                component.LoadScene("scene/home", "scene", 0, sceneCallbacks, "scene-user");
                component.LoadBinary("config.bytes", "config", binaryCallbacks, "binary-user");
                component.InstantiateAsset("ui/prefab", "ui", assetCallbacks, "instantiate-user");

                Assert.That(manager.LastAssetPackageId, Is.EqualTo("ui"));
                Assert.That(manager.LastScenePackageId, Is.EqualTo("scene"));
                Assert.That(manager.LastBinaryPackageId, Is.EqualTo("config"));
                Assert.That(manager.LastInstantiatePackageId, Is.EqualTo("ui"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(componentGo);
            }
        }

        [Test]
        public void ResourceComponent_ExposesRouteIndexRefreshApi()
        {
            MethodInfo method = typeof(UnityGameFramework.Runtime.ResourceComponent).GetMethod(
                "RefreshRouteIndexAsync",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(method, Is.Not.Null, "Expected route-index refresh API on ResourceComponent.");
        }

        [Test]
        public void YooAssetResourceHelper_HasAsset_UsesAddressAwarePackageLookup()
        {
            var helperGo = new GameObject("TestYooAssetResourceHelper");
            try
            {
                var helper = helperGo.AddComponent<TestableYooAssetResourceHelper>();

                HasAssetResult result = helper.HasAsset("ui/home");

                Assert.That(result, Is.EqualTo(HasAssetResult.NotReady));
                Assert.That(helper.LastLoadedPackageAddress, Is.EqualTo("ui/home"));
                Assert.That(helper.LastLoadedPackageId, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        [Test]
        public void YooAssetResourceHelper_PackageScopedCacheKeys_DifferAcrossPackages()
        {
            var helperGo = new GameObject("ScopedKeyHelper");
            try
            {
                var helper = helperGo.AddComponent<TestableYooAssetResourceHelper>();

                string uiKey = helper.ExposePackageScopedCacheKey("ui", "shared/address");
                string sceneKey = helper.ExposePackageScopedCacheKey("scene", "shared/address");

                Assert.That(uiKey, Is.Not.EqualTo(sceneKey));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        [Test]
        public void ResourceHelperBase_HandleAwareOverloads_FallbackToLegacyImplementations()
        {
            var helperGo = new GameObject("HandleAwareTestResourceHelper");
            try
            {
                var helper = helperGo.AddComponent<TestResourceHelper>();

                helper.LoadAssetHandle<TextAsset>("ui/home", "ui");
                helper.InstantiateAssetHandle("ui/prefab", "ui");
                helper.LoadSceneHandle("scene/home", "scene");
                helper.LoadRawFileHandle("config.bytes", "config");

                Assert.That(helper.LastHandleAssetName, Is.EqualTo("ui/home"));
                Assert.That(helper.LastHandleInstantiateName, Is.EqualTo("ui/prefab"));
                Assert.That(helper.LastHandleSceneName, Is.EqualTo("scene/home"));
                Assert.That(helper.LastHandleBinaryName, Is.EqualTo("config.bytes"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        [Test]
        public void ResourceComponent_HandleAwareOverloads_ForwardPackageIdToHelper()
        {
            var componentGo = new GameObject("ResourceComponent");
            var helperGo = new GameObject("HandleAwareProxy");
            try
            {
                var component = (UnityGameFramework.Runtime.ResourceComponent)Activator.CreateInstance(typeof(UnityGameFramework.Runtime.ResourceComponent));
                SetPrivateField(component, "Parent", componentGo);
                component.AwakeComponent();

                var helper = helperGo.AddComponent<TestHandleAwareResourceHelper>();
                helper.SetResourceComponent(component);
                SetPrivateField(component, "_resourceHelper", helper);

                component.LoadAssetHandle<TextAsset>("ui/home", "ui");
                component.InstantiateAssetHandle("ui/prefab", "ui");
                component.LoadSceneHandle("scene/home", "scene");
                component.LoadRawFileHandle("config.bytes", "config");

                Assert.That(helper.LastHandleAssetPackageId, Is.EqualTo("ui"));
                Assert.That(helper.LastHandleInstantiatePackageId, Is.EqualTo("ui"));
                Assert.That(helper.LastHandleScenePackageId, Is.EqualTo("scene"));
                Assert.That(helper.LastHandleBinaryPackageId, Is.EqualTo("config"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
                UnityEngine.Object.DestroyImmediate(componentGo);
            }
        }

        [Test]
        public void YooAssetResourceHelper_PackageRuntimeState_TracksInitializationOutcome_AndRouteRefresh()
        {
            var helperGo = new GameObject("RuntimeStateHelper");
            try
            {
                var helper = helperGo.AddComponent<TestableYooAssetResourceHelper>();

                helper.RecordInitializationStarted("UIPackage", "ui");
                bool hasStateAfterStart = helper.TryGetRuntimeState("UIPackage", out PackageRuntimeState startedState);

                Assert.That(hasStateAfterStart, Is.True);
                Assert.That(startedState.PackageName, Is.EqualTo("UIPackage"));
                Assert.That(startedState.LogicalPackageId, Is.EqualTo("ui"));
                Assert.That(startedState.IsInitializing, Is.True);
                Assert.That(startedState.IsInitialized, Is.False);

                helper.RecordInitializationResult("UIPackage", "ui", PackageInitializationResult.CreateFailure("UIPackage", "boom"));
                bool hasStateAfterFailure = helper.TryGetRuntimeState("UIPackage", out PackageRuntimeState failedState);

                Assert.That(hasStateAfterFailure, Is.True);
                Assert.That(failedState.IsInitializing, Is.False);
                Assert.That(failedState.IsInitialized, Is.False);
                Assert.That(failedState.LastError, Is.EqualTo("boom"));

                helper.RecordInitializationStarted("UIPackage", "ui");
                helper.RecordInitializationResult("UIPackage", "ui", PackageInitializationResult.CreateSuccess("UIPackage"));
                helper.RecordRouteIndexRefreshed("UIPackage");

                bool hasStateAfterSuccess = helper.TryGetRuntimeState("UIPackage", out PackageRuntimeState successState);

                Assert.That(hasStateAfterSuccess, Is.True);
                Assert.That(successState.IsInitializing, Is.False);
                Assert.That(successState.IsInitialized, Is.True);
                Assert.That(successState.LastError, Is.Null.Or.Empty);
                Assert.That(successState.LastRouteIndexRefreshUtc, Is.Not.EqualTo(default(DateTime)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        [Test]
        public void YooAssetResourceHelper_DefersRouteIndexBootstrap_UntilManifestIsReady()
        {
            var helperGo = new GameObject("RouteIndexBootstrapHelper");
            try
            {
                var helper = helperGo.AddComponent<TestableYooAssetResourceHelper>();

                Assert.That(helper.ShouldDeferBootstrapRouteIndexLoad(packageValid: false), Is.True);
                Assert.That(helper.ShouldDeferBootstrapRouteIndexLoad(packageValid: true), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperGo);
            }
        }

        private static bool Contains(string message, string fragment)
        {
            return message != null &&
                   message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IResourceManager CreateResourceManager()
        {
            Type managerType = typeof(IResourceManager).Assembly.GetType("GameFramework.Resource.ResourceManager");
            Assert.That(managerType, Is.Not.Null, "Expected internal ResourceManager type.");

            object instance = Activator.CreateInstance(managerType, nonPublic: true);
            Assert.That(instance, Is.AssignableTo<IResourceManager>());
            return (IResourceManager)instance;
        }

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {name}.");
            field.SetValue(instance, value);
        }

        private sealed class TestResourceHelper : UnityGameFramework.Runtime.ResourceHelperBase
        {
            public string LastAssetName { get; private set; }
            public string LastSceneName { get; private set; }
            public string LastBinaryName { get; private set; }
            public string LastInstantiateName { get; private set; }
            public string LastHandleAssetName { get; private set; }
            public string LastHandleSceneName { get; private set; }
            public string LastHandleBinaryName { get; private set; }
            public string LastHandleInstantiateName { get; private set; }

            public override void InitializeResources(ResourceInitCallBack callback)
            {
            }

            public override HasAssetResult HasAsset(string assetName)
            {
                return HasAssetResult.Exist;
            }

            public override void Release(object asset)
            {
            }

            public override void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
            {
            }

            public override void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks, object userData)
            {
                LastAssetName = assetName;
            }

            public override void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks, object userData)
            {
                LastSceneName = sceneAssetName;
            }

            public override void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData)
            {
                LastBinaryName = binaryAssetName;
            }

            public override void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData)
            {
                LastInstantiateName = assetName;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName)
            {
                LastHandleAssetName = assetName;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName)
            {
                LastHandleInstantiateName = assetName;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceSceneHandle LoadSceneHandle(string sceneAssetName)
            {
                LastHandleSceneName = sceneAssetName;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName)
            {
                LastHandleBinaryName = binaryAssetName;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceBatchHandle<T> LoadAssetsByTagHandle<T>(string tag)
            {
                return null;
            }
        }

        private sealed class TestHandleAwareResourceHelper : UnityGameFramework.Runtime.ResourceHelperBase
        {
            public string LastHandleAssetPackageId { get; private set; }
            public string LastHandleScenePackageId { get; private set; }
            public string LastHandleBinaryPackageId { get; private set; }
            public string LastHandleInstantiatePackageId { get; private set; }

            public override void InitializeResources(ResourceInitCallBack callback)
            {
            }

            public override HasAssetResult HasAsset(string assetName)
            {
                return HasAssetResult.Exist;
            }

            public override void Release(object asset)
            {
            }

            public override void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
            {
            }

            public override void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks, object userData)
            {
            }

            public override void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks, object userData)
            {
            }

            public override void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData)
            {
            }

            public override void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData)
            {
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName, string packageId)
            {
                LastHandleAssetPackageId = packageId;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName, string packageId)
            {
                LastHandleInstantiatePackageId = packageId;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceSceneHandle LoadSceneHandle(string sceneAssetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceSceneHandle LoadSceneHandle(string sceneAssetName, string packageId)
            {
                LastHandleScenePackageId = packageId;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName, string packageId)
            {
                LastHandleBinaryPackageId = packageId;
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceBatchHandle<T> LoadAssetsByTagHandle<T>(string tag)
            {
                return null;
            }
        }

        private sealed class TestResourceHelperProxy : IResourceHelper
        {
            public string LastPackageId { get; private set; }
            public string LastScenePackageId { get; private set; }
            public string LastBinaryPackageId { get; private set; }
            public string LastInstantiatePackageId { get; private set; }

            public void InitializeResources(ResourceInitCallBack callback)
            {
            }

            public HasAssetResult HasAsset(string assetName)
            {
                return HasAssetResult.Exist;
            }

            public void Release(object asset)
            {
            }

            public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
            {
            }

            public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks, object userData)
            {
                LastPackageId = null;
            }

            public void LoadAsset(string assetName, Type assetType, string packageId, LoadAssetCallbacks callbacks, object userData)
            {
                LastPackageId = packageId;
            }

            public void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks, object userData)
            {
                LastScenePackageId = null;
            }

            public void LoadScene(string sceneAssetName, string packageId, LoadSceneCallbacks callbacks, object userData)
            {
                LastScenePackageId = packageId;
            }

            public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData)
            {
                LastBinaryPackageId = null;
            }

            public void LoadBinary(string binaryAssetName, string packageId, LoadBinaryCallbacks callbacks, object userData)
            {
                LastBinaryPackageId = packageId;
            }

            public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData)
            {
                LastInstantiatePackageId = null;
            }

            public void InstantiateAsset(string assetName, string packageId, LoadAssetCallbacks callbacks, object userData)
            {
                LastInstantiatePackageId = packageId;
            }
        }

        private sealed class TestResourceManagerProxy : IResourceManager
        {
            public string LastAssetPackageId { get; private set; }
            public string LastScenePackageId { get; private set; }
            public string LastBinaryPackageId { get; private set; }
            public string LastInstantiatePackageId { get; private set; }

            public string ReadOnlyPath => string.Empty;
            public string ReadWritePath => string.Empty;
            public ResourceMode ResourceMode => ResourceMode.YooAsset;

            public void SetReadOnlyPath(string readOnlyPath) { }
            public void SetReadWritePath(string readWritePath) { }
            public void SetResourceMode(ResourceMode resourceMode) { }
            public void SetResourceHelper(IResourceHelper resourceHelper) { }
            public void InitResources(InitResourcesCompleteCallback callback) { }
            public HasAssetResult HasAsset(string assetName) => HasAssetResult.Exist;
            public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks callbacks, object userData) => LastAssetPackageId = null;
            public void LoadAsset(string assetName, Type assetType, string packageId, int priority, LoadAssetCallbacks callbacks, object userData) => LastAssetPackageId = packageId;
            public void UnloadAsset(object asset) { }
            public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks callbacks, object userData) => LastScenePackageId = null;
            public void LoadScene(string sceneAssetName, string packageId, int priority, LoadSceneCallbacks callbacks, object userData) => LastScenePackageId = packageId;
            public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData) { }
            public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData) => LastBinaryPackageId = null;
            public void LoadBinary(string binaryAssetName, string packageId, LoadBinaryCallbacks callbacks, object userData) => LastBinaryPackageId = packageId;
            public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData) => LastInstantiatePackageId = null;
            public void InstantiateAsset(string assetName, string packageId, LoadAssetCallbacks callbacks, object userData) => LastInstantiatePackageId = packageId;
        }

        private sealed class TestableYooAssetResourceHelper : YooAssetResourceHelper
        {
            public string LastLoadedPackageAddress { get; private set; }
            public string LastLoadedPackageId { get; private set; }

            public string ExposePackageScopedCacheKey(string packageName, string address)
            {
                return ComposePackageScopedCacheKey(packageName, address);
            }

            protected override ResourcePackage GetLoadedPackage(string address, string packageId)
            {
                LastLoadedPackageAddress = address;
                LastLoadedPackageId = packageId;
                return null;
            }

            public void RecordInitializationStarted(string packageName, string logicalPackageId)
            {
                MarkPackageInitializationStarted(packageName, logicalPackageId);
            }

            public void RecordInitializationResult(string packageName, string logicalPackageId, PackageInitializationResult result)
            {
                ApplyPackageInitializationResult(packageName, logicalPackageId, result);
            }

            public void RecordRouteIndexRefreshed(string packageName)
            {
                MarkPackageRouteIndexRefreshed(packageName);
            }

            public bool TryGetRuntimeState(string packageName, out PackageRuntimeState state)
            {
                return TryGetPackageRuntimeState(packageName, out state);
            }

            public bool ShouldDeferBootstrapRouteIndexLoad(bool packageValid)
            {
                return ShouldDeferRouteIndexLoadUntilManifestReady(packageValid);
            }
        }
    }
}
#endif
