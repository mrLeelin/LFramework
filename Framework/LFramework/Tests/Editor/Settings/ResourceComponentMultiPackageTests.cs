using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Resource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class ResourceComponentMultiPackageTests
    {
        [Test]
        public void EffectivePackages_IncludeLegacyPackage_WithoutMutatingConfiguredPackages()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            Assert.That(setting.YooAssetPackages, Is.Empty);

            IReadOnlyList<PackageDefinition> effectivePackages = setting.GetEffectivePackageDefinitions();

            Assert.That(effectivePackages, Has.Count.EqualTo(1));
            Assert.That(effectivePackages[0].packageId, Is.EqualTo("DefaultPackage"));
            Assert.That(effectivePackages[0].yooPackageName, Is.EqualTo("DefaultPackage"));
            Assert.That(setting.YooAssetPackages, Is.Empty);
        }

        [Test]
        public void EffectivePackages_ReturnClones_NotLiveConfigurationReferences()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage",
                remoteFolderName = "ui"
            });

            IReadOnlyList<PackageDefinition> firstSnapshot = setting.GetEffectivePackageDefinitions();
            firstSnapshot[0].yooPackageName = "MutatedPackage";

            IReadOnlyList<PackageDefinition> secondSnapshot = setting.GetEffectivePackageDefinitions();

            Assert.That(secondSnapshot[0].yooPackageName, Is.EqualTo("UIPackage"));
            Assert.That(setting.YooAssetPackages[0].yooPackageName, Is.EqualTo("UIPackage"));
        }

        [Test]
        public void ResolvedDefaultAndBootstrapPackageIds_FallBackToLegacyPackage_WhenExplicitIdsAreEmpty()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            Assert.That(setting.GetResolvedDefaultPackageId(), Is.EqualTo("DefaultPackage"));
            Assert.That(setting.GetResolvedBootstrapPackageId(), Is.EqualTo("DefaultPackage"));
        }

        [Test]
        public void YooAssetPackageName_FallsBackToResolvedDefaultPackage_WhenLegacyFieldIsEmpty()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_yooAssetPackageName", string.Empty);
            SetPrivateField(setting, "_defaultPackageId", "ui");
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage",
                remoteFolderName = "ui"
            });

            Assert.That(setting.YooAssetPackageName, Is.EqualTo("UIPackage"));
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsDuplicatePackageIds()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_yooAssetPackageName", string.Empty);
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage_Override" });

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "duplicate")), Is.True);
            Assert.That(warnings, Is.Not.Null);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_WarnsWhenRouteIndexBootstrapFieldsAreMissing()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(warnings.Exists(message => Contains(message, "route")), Is.True);
            Assert.That(warnings.Exists(message => Contains(message, "bootstrap")), Is.True);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsUnknownDefaultBootstrapAndRouteIndexPackages()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_yooAssetPackageName", string.Empty);
            SetPrivateField(setting, "_defaultPackageId", "missing-default");
            SetPrivateField(setting, "_bootstrapPackageId", "missing-bootstrap");
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage" });
            setting.YooAssetRouting.routeIndexPackageId = "missing-route";
            setting.YooAssetRouting.routeIndexAddress = "route-index";

            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            Assert.That(isValid, Is.False);
            Assert.That(errors.Exists(message => Contains(message, "default")), Is.True);
            Assert.That(errors.Exists(message => Contains(message, "bootstrap")), Is.True);
            Assert.That(errors.Exists(message => Contains(message, "route index")), Is.True);
            Assert.That(warnings, Is.Not.Null);
        }

        [Test]
        public void ValidateMultiPackageConfiguration_RejectsUnknownFallbackPackageReferences()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_yooAssetPackageName", string.Empty);
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
        public void PackageRegistry_ActivatesMatchingDefinition_AndStoresAClone()
        {
            var windowsDefinition = new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Windows",
                routePriority = 10,
                remoteFolderName = "ui",
                platformFilter = new List<string> { RuntimePlatform.WindowsEditor.ToString() },
                channelFilter = new List<string> { "Google" }
            };
            var androidDefinition = new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Android",
                routePriority = 1,
                remoteFolderName = "ui",
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
        public void PackageResolver_FallsBackToDefaultPackage_WhenRouteIndexMisses()
        {
            var routing = new RoutingSettings { allowDefaultPackageFallback = true };
            var resolver = new PackageResolver(routing);

            Assert.That(resolver.ResolvePackageId("unknown/address", null, "base"), Is.EqualTo("base"));
        }

        [Test]
        public void RouteIndexBootstrapLoader_LoadsRouteIndexDirectlyFromBootstrapPackage()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage", remoteFolderName = "bootstrap" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var routing = new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            };
            var loader = new RouteIndexBootstrapLoader(registry, routing);
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
        public void RouteIndexBootstrapLoader_ExposesBootstrapRequest_ForRuntimeAsyncLoading()
        {
            var registry = new PackageRegistry();
            registry.Configure(
                new[]
                {
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage", remoteFolderName = "bootstrap" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var loader = new RouteIndexBootstrapLoader(registry, new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            });

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
                    new PackageDefinition { packageId = "bootstrap", yooPackageName = "BootstrapPackage", remoteFolderName = "bootstrap" }
                },
                RuntimePlatform.WindowsEditor,
                "Google");

            var loader = new RouteIndexBootstrapLoader(registry, new RoutingSettings
            {
                routeIndexPackageId = "bootstrap",
                routeIndexAddress = "route-index"
            });

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
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceSceneHandle LoadSceneHandle(string sceneAssetName)
            {
                return null;
            }

            public override UnityGameFramework.Runtime.ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName)
            {
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
    }
}
