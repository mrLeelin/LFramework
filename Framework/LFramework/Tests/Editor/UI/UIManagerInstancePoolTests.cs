using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework.ObjectPool;
using GameFramework.Resource;
using GameFramework.UI;
using NUnit.Framework;
using UnityEngine;
using UnityGameFramework.Runtime;
using LFramework.Runtime;
using FrameworkDefaultUIFormHelper = UnityGameFramework.Runtime.DefaultUIFormHelper;
using LFrameworkDefaultUIFormHelper = LFramework.Runtime.DefaultUIFormHelper;
using RuntimeResourceComponent = UnityGameFramework.Runtime.ResourceComponent;

namespace LFramework.Editor.Tests.UI
{
    public class UIManagerInstancePoolTests
    {
        private IUIManager _manager;
        private IObjectPoolManager _objectPoolManager;
        private TestResourceManager _resourceManager;
        private TestUIFormHelper _uiFormHelper;
        private bool _isShutdown;

        [SetUp]
        public void SetUp()
        {
            _isShutdown = false;
            _manager = CreateInternalModule<IUIManager>("GameFramework.UI.UIManager");
            _objectPoolManager =
                CreateInternalModule<IObjectPoolManager>("GameFramework.ObjectPool.ObjectPoolManager");
            _resourceManager = new TestResourceManager();
            _uiFormHelper = new TestUIFormHelper();

            _manager.SetObjectPoolManager(_objectPoolManager);
            _manager.SetResourceManager(_resourceManager);
            _manager.SetUIFormHelper(_uiFormHelper);
        }

        [TearDown]
        public void TearDown()
        {
            if (!_isShutdown)
            {
                InvokeModuleMethod(_manager, "Shutdown");
            }

            InvokeModuleMethod(_objectPoolManager, "Shutdown");
        }

        [Test]
        public void UIGroupConfiguration_DefaultsToPoolingAndSerializesOptOut()
        {
            var group = new UIComponent.UIGroup();
            Assert.That(group.DisableInstancePool, Is.False);

            JsonUtility.FromJsonOverwrite("{\"m_DisableInstancePool\":true}", group);
            Assert.That(group.DisableInstancePool, Is.True);

            string json = JsonUtility.ToJson(group);
            var restored = JsonUtility.FromJson<UIComponent.UIGroup>(json);
            Assert.That(restored.DisableInstancePool, Is.True);
        }

        [Test]
        public void DisabledPoolGroup_CloseAndReopen_LoadsAndReleasesEachInstance()
        {
            AddGroup("Direct", true);
            Assert.That(_manager.GetUIGroup("Direct").DisableInstancePool, Is.True);

            int firstSerialId = _manager.OpenUIForm("UI/Window", "Direct");
            object firstInstance = new object();
            _resourceManager.CompleteSuccess(0, firstInstance);

            TestUIForm firstForm = _uiFormHelper.CreatedForms[0];
            _manager.CloseUIForm(firstSerialId);

            Assert.That(firstForm.CloseCount, Is.EqualTo(1));
            Assert.That(firstForm.RecycleCount, Is.Zero,
                "Recycle remains deferred until the manager update.");
            Assert.That(_uiFormHelper.ReleaseCalls, Is.Empty,
                "The instance must stay alive until OnRecycle has run.");

            InvokeModuleMethod(_manager, "Update", 0f, 0f);

            Assert.That(firstForm.RecycleCount, Is.EqualTo(1));
            AssertRelease(firstInstance, 0, recycledBeforeRelease: true);

            int secondSerialId = _manager.OpenUIForm("UI/Window", "Direct");
            Assert.That(_resourceManager.PendingLoads, Has.Count.EqualTo(2),
                "A group that disables pooling must instantiate again instead of spawning the released instance.");

            object secondInstance = new object();
            _resourceManager.CompleteSuccess(1, secondInstance);
            _manager.CloseUIForm(secondSerialId);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);

            Assert.That(_uiFormHelper.CreatedForms, Has.Count.EqualTo(2));
            Assert.That(_uiFormHelper.CreatedForms[1].Handle, Is.SameAs(secondInstance));
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(2));
            AssertRelease(secondInstance, 1, recycledBeforeRelease: true);
        }

        [Test]
        public void DisabledPoolGroup_CloseWhileLoading_ReleasesCompletedInstanceExactlyOnce()
        {
            AddGroup("Direct", true);
            int serialId = _manager.OpenUIForm("UI/SlowWindow", "Direct");

            _manager.CloseUIForm(serialId);
            object loadedInstance = new object();
            _resourceManager.CompleteSuccess(0, loadedInstance);

            Assert.That(_uiFormHelper.CreatedForms, Is.Empty,
                "A cancelled load must not create or open a UI form.");
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
            AssertRelease(loadedInstance, 0, recycledBeforeRelease: false);

            InvokeModuleMethod(_manager, "Update", 0f, 0f);
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
        }

        [Test]
        public void DisabledPoolGroup_CreateFailure_ReleasesInstanceExactlyOnce()
        {
            AddGroup("Direct", true);
            _uiFormHelper.FailCreate = true;
            int failureEventCount = 0;
            _manager.OpenUIFormFailure += (_, _) => failureEventCount++;

            _manager.OpenUIForm("UI/BrokenWindow", "Direct");
            object loadedInstance = new object();
            _resourceManager.CompleteSuccess(0, loadedInstance);

            Assert.That(failureEventCount, Is.EqualTo(1));
            Assert.That(_uiFormHelper.CreatedForms, Is.Empty);
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
            AssertRelease(loadedInstance, 0, recycledBeforeRelease: false);

            InvokeModuleMethod(_manager, "Update", 0f, 0f);
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
        }

        [Test]
        public void DisabledPoolGroup_OnOpenFailure_ClosesRecyclesAndReleasesInstanceExactlyOnce()
        {
            AddGroup("Direct", true);
            _uiFormHelper.ThrowOnOpen = true;
            int failureEventCount = 0;
            _manager.OpenUIFormFailure += (_, _) => failureEventCount++;

            _manager.OpenUIForm("UI/BrokenWindow", "Direct");
            object loadedInstance = new object();
            _resourceManager.CompleteSuccess(0, loadedInstance);

            TestUIForm form = _uiFormHelper.CreatedForms[0];
            Assert.That(failureEventCount, Is.EqualTo(1));
            Assert.That(form.CloseCount, Is.EqualTo(1));
            Assert.That(form.RecycleCount, Is.EqualTo(1));
            Assert.That(_manager.HasUIForm(form.SerialId), Is.False);
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
            AssertRelease(loadedInstance, 0, recycledBeforeRelease: true);

            InvokeModuleMethod(_manager, "Update", 0f, 0f);
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
        }

        [Test]
        public void DisabledPoolGroup_Shutdown_DrainsClosedQueueAndReleasesLateLoadExactlyOnce()
        {
            AddGroup("Direct", true);
            _manager.OpenUIForm("UI/Window", "Direct");
            object closedInstance = new object();
            _resourceManager.CompleteSuccess(0, closedInstance);
            TestUIForm closedForm = _uiFormHelper.CreatedForms[0];
            _manager.CloseUIForm(closedForm);

            _manager.OpenUIForm("UI/ActiveWindow", "Direct");
            object activeInstance = new object();
            _resourceManager.CompleteSuccess(1, activeInstance);
            TestUIForm activeForm = _uiFormHelper.CreatedForms[1];

            _manager.OpenUIForm("UI/LateWindow", "Direct");

            InvokeModuleMethod(_manager, "Shutdown");
            _isShutdown = true;

            Assert.That(closedForm.CloseCount, Is.EqualTo(1));
            Assert.That(closedForm.LastCloseWasShutdown, Is.False,
                "The explicit close happened before shutdown began.");
            Assert.That(closedForm.RecycleCount, Is.EqualTo(1),
                "Shutdown must drain forms that were already waiting in the recycle queue.");
            Assert.That(activeForm.CloseCount, Is.EqualTo(1));
            Assert.That(activeForm.LastCloseWasShutdown, Is.True);
            Assert.That(activeForm.RecycleCount, Is.EqualTo(1));
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(2));
            AssertRelease(closedInstance, 0, recycledBeforeRelease: true);
            AssertRelease(activeInstance, 1, recycledBeforeRelease: true);

            object lateInstance = new object();
            _resourceManager.CompleteSuccess(2, lateInstance);

            Assert.That(_uiFormHelper.CreatedForms, Has.Count.EqualTo(2),
                "A load that completes after shutdown must not create a UI form.");
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(3));
            AssertRelease(lateInstance, 2, recycledBeforeRelease: false);
        }

        [Test]
        public void SameAssetAcrossPooledAndDisabledGroups_DoesNotCrossReuseInstances()
        {
            AddGroup("Pooled", false);
            AddGroup("Direct", true);

            int pooledSerialId = _manager.OpenUIForm("UI/Shared", "Pooled");
            object pooledInstance = new object();
            _resourceManager.CompleteSuccess(0, pooledInstance);
            _manager.CloseUIForm(pooledSerialId);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);

            int directSerialId = _manager.OpenUIForm("UI/Shared", "Direct");
            Assert.That(_resourceManager.PendingLoads, Has.Count.EqualTo(2),
                "The disabled group must not spawn the pooled group's cached instance.");
            object directInstance = new object();
            _resourceManager.CompleteSuccess(1, directInstance);
            _manager.CloseUIForm(directSerialId);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);

            int reopenedPooledSerialId = _manager.OpenUIForm("UI/Shared", "Pooled");
            Assert.That(_resourceManager.PendingLoads, Has.Count.EqualTo(2),
                "The pooled group should still reuse its own cached instance.");
            TestUIForm reopenedPooledForm = _uiFormHelper.CreatedForms[2];
            Assert.That(reopenedPooledForm.Handle, Is.SameAs(pooledInstance));
            Assert.That(reopenedPooledForm.Handle, Is.Not.SameAs(directInstance));

            _manager.CloseUIForm(reopenedPooledSerialId);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);
        }

        [Test]
        public void DisabledPoolGroup_LockPriorityAndDuplicateClose_AreSafeAndReleaseOnce()
        {
            AddGroup("Direct", true);
            _manager.OpenUIForm("UI/Window", "Direct");
            object loadedInstance = new object();
            _resourceManager.CompleteSuccess(0, loadedInstance);
            TestUIForm form = _uiFormHelper.CreatedForms[0];

            Assert.DoesNotThrow(() => _manager.SetUIFormInstanceLocked(loadedInstance, true));
            Assert.DoesNotThrow(() => _manager.SetUIFormInstancePriority(loadedInstance, 100));

            _manager.CloseUIForm(form);
            _manager.CloseUIForm(form);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);

            Assert.That(form.CloseCount, Is.EqualTo(1));
            Assert.That(form.RecycleCount, Is.EqualTo(1));
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
            AssertRelease(loadedInstance, 0, recycledBeforeRelease: true);
        }

        [Test]
        public void DisabledPoolGroup_ShutdownContinuesAfterCloseAndRecycleFailures()
        {
            AddGroup("Direct", true);

            _manager.OpenUIForm("UI/First", "Direct");
            object firstInstance = new object();
            _resourceManager.CompleteSuccess(0, firstInstance);
            TestUIForm firstForm = _uiFormHelper.CreatedForms[0];
            firstForm.ThrowOnRecycle = true;

            _manager.OpenUIForm("UI/Second", "Direct");
            object secondInstance = new object();
            _resourceManager.CompleteSuccess(1, secondInstance);
            TestUIForm secondForm = _uiFormHelper.CreatedForms[1];
            secondForm.ThrowOnClose = true;

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => InvokeModuleMethod(_manager, "Shutdown"));
            _isShutdown = true;

            Assert.That(exception.InnerException, Is.TypeOf<AggregateException>());
            Assert.That(_manager.UIGroupCount, Is.Zero,
                "Shutdown must clear group ownership after collecting lifecycle failures.");
            Assert.That(firstForm.CloseCount, Is.EqualTo(1));
            Assert.That(secondForm.CloseCount, Is.EqualTo(1));
            Assert.That(firstForm.RecycleCount, Is.EqualTo(1));
            Assert.That(secondForm.RecycleCount, Is.EqualTo(1));
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(2));
            Assert.That(_uiFormHelper.ReleaseCalls.Exists(call => ReferenceEquals(call.Instance, firstInstance)), Is.True);
            Assert.That(_uiFormHelper.ReleaseCalls.Exists(call => ReferenceEquals(call.Instance, secondInstance)), Is.True);
        }

        [Test]
        public void PooledGroup_OnRecycleFailure_DiscardsDirtyInstance()
        {
            AddGroup("Pooled", false);
            int serialId = _manager.OpenUIForm("UI/Pooled", "Pooled");
            object dirtyInstance = new object();
            _resourceManager.CompleteSuccess(0, dirtyInstance);
            TestUIForm form = _uiFormHelper.CreatedForms[0];
            form.ThrowOnRecycle = true;

            _manager.CloseUIForm(serialId);
            Assert.Throws<TargetInvocationException>(() => InvokeModuleMethod(_manager, "Update", 0f, 0f));

            _manager.OpenUIForm("UI/Pooled", "Pooled");
            Assert.That(_resourceManager.PendingLoads, Has.Count.EqualTo(2),
                "An instance whose recycle callback failed must be discarded instead of spawned again.");
        }

        [Test]
        public void DisabledPoolGroup_ReentrantShutdownDuringRecycle_ReleasesInstanceOnce()
        {
            AddGroup("Direct", true);
            int serialId = _manager.OpenUIForm("UI/Reentrant", "Direct");
            object loadedInstance = new object();
            _resourceManager.CompleteSuccess(0, loadedInstance);
            TestUIForm form = _uiFormHelper.CreatedForms[0];
            form.RecycleAction = () => InvokeModuleMethod(_manager, "Shutdown");

            _manager.CloseUIForm(serialId);
            InvokeModuleMethod(_manager, "Update", 0f, 0f);
            _isShutdown = true;

            Assert.That(_manager.UIGroupCount, Is.Zero);
            Assert.That(form.RecycleCount, Is.EqualTo(1));
            Assert.That(_uiFormHelper.ReleaseCalls, Has.Count.EqualTo(1));
            AssertRelease(loadedInstance, 0, recycledBeforeRelease: true);
        }

        [Test]
        public void DefaultHelpers_ReleaseThroughCachedResourceHelperAfterResourceShutdown()
        {
            var resourceHelper = new TestResourceHelper();
            IResourceManager resourceManager =
                CreateInternalModule<IResourceManager>("GameFramework.Resource.ResourceManager");
            resourceManager.SetResourceHelper(resourceHelper);

            var frameworkHelperObject = new GameObject("Framework UI Helper");
            var lFrameworkHelperObject = new GameObject("LFramework UI Helper");
            LFrameworkAspect aspect = null;
            try
            {
                var resourceComponent = new RuntimeResourceComponent();
                aspect = new LFrameworkAspect();
                aspect.Register();
                LServices.Register(resourceComponent);
                var frameworkHelper = frameworkHelperObject.AddComponent<FrameworkDefaultUIFormHelper>();
                var lFrameworkHelper = lFrameworkHelperObject.AddComponent<LFrameworkDefaultUIFormHelper>();
                SetPrivateField(frameworkHelper, "m_ResourceHelper", resourceHelper);
                SetPrivateField(lFrameworkHelper, "m_ResourceHelper", resourceHelper);

                resourceComponent.ShutDown();
                InvokeModuleMethod(resourceManager, "Shutdown");

                object firstAsset = new object();
                object secondAsset = new object();
                frameworkHelper.ReleaseUIForm(firstAsset, null);
                lFrameworkHelper.ReleaseUIForm(secondAsset, null);

                Assert.That(resourceHelper.ReleasedAssets, Is.EqualTo(new[] { firstAsset, secondAsset }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lFrameworkHelperObject);
                UnityEngine.Object.DestroyImmediate(frameworkHelperObject);
                aspect?.Destroy();
            }
        }

        private void AddGroup(string name, bool disableInstancePool)
        {
            Assert.That(
                _manager.AddUIGroup(name, 0, disableInstancePool, new TestUIGroupHelper()),
                Is.True);
        }

        private void AssertRelease(object expectedInstance, int index, bool recycledBeforeRelease)
        {
            ReleaseCall release = _uiFormHelper.ReleaseCalls[index];
            Assert.That(release.Asset, Is.SameAs(expectedInstance));
            Assert.That(release.Instance, Is.SameAs(expectedInstance));
            Assert.That(release.RecycledBeforeRelease, Is.EqualTo(recycledBeforeRelease));
        }

        private static T CreateInternalModule<T>(string typeName)
        {
            Type type = typeof(IUIManager).Assembly.GetType(typeName, true);
            object instance = Activator.CreateInstance(type, true);
            Assert.That(instance, Is.AssignableTo<T>());
            return (T)instance;
        }

        private static void InvokeModuleMethod(object module, string methodName, params object[] arguments)
        {
            MethodInfo method = module.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Can not find module method '{methodName}'.");
            method.Invoke(module, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Can not find field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private sealed class TestResourceManager : IResourceManager
        {
            internal sealed class PendingLoad
            {
                internal string AssetName;
                internal LoadAssetCallbacks Callbacks;
                internal object UserData;
            }

            internal readonly List<PendingLoad> PendingLoads = new List<PendingLoad>();

            public string ReadOnlyPath => string.Empty;
            public string ReadWritePath => string.Empty;
            public ResourceMode ResourceMode => ResourceMode.Unspecified;

            public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData)
            {
                PendingLoads.Add(new PendingLoad
                {
                    AssetName = assetName,
                    Callbacks = callbacks,
                    UserData = userData
                });
            }

            internal void CompleteSuccess(int index, object instance)
            {
                PendingLoad load = PendingLoads[index];
                load.Callbacks.LoadAssetSuccessCallback(load.AssetName, instance, 0.1f, load.UserData);
            }

            public void SetReadOnlyPath(string readOnlyPath) => throw new NotSupportedException();
            public void SetReadWritePath(string readWritePath) => throw new NotSupportedException();
            public void SetResourceMode(ResourceMode resourceMode) => throw new NotSupportedException();
            public void SetResourceHelper(IResourceHelper resourceHelper) => throw new NotSupportedException();
            public void InitResources(InitResourcesCompleteCallback callback) => throw new NotSupportedException();
            public HasAssetResult HasAsset(string assetName) => throw new NotSupportedException();

            public void LoadAsset(string assetName, Type assetType, int priority,
                LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void LoadAsset(string assetName, Type assetType, string packageId, int priority,
                LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void UnloadAsset(object asset) => throw new NotSupportedException();

            public void LoadScene(string sceneAssetName, int priority,
                LoadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void LoadScene(string sceneAssetName, string packageId, int priority,
                LoadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void UnloadScene(string sceneAssetName,
                UnloadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void LoadBinary(string binaryAssetName,
                LoadBinaryCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void LoadBinary(string binaryAssetName, string packageId,
                LoadBinaryCallbacks callbacks, object userData) => throw new NotSupportedException();

            public void InstantiateAsset(string assetName, string packageId,
                LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
        }

        private sealed class TestUIGroupHelper : IUIGroupHelper
        {
            public void SetDepth(int depth)
            {
            }
        }

        private sealed class TestResourceHelper : IResourceHelper
        {
            internal readonly List<object> ReleasedAssets = new List<object>();

            public void InitializeResources(ResourceInitCallBack callback) => throw new NotSupportedException();
            public HasAssetResult HasAsset(string assetName) => throw new NotSupportedException();
            public void Release(object asset) => ReleasedAssets.Add(asset);
            public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadAsset(string assetName, Type assetType, string packageId, LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadScene(string sceneAssetName, string packageId, LoadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void LoadBinary(string binaryAssetName, string packageId, LoadBinaryCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
            public void InstantiateAsset(string assetName, string packageId, LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
        }

        private sealed class TestUIFormHelper : IUIFormHelper
        {
            internal readonly List<TestUIForm> CreatedForms = new List<TestUIForm>();
            internal readonly List<ReleaseCall> ReleaseCalls = new List<ReleaseCall>();
            internal bool FailCreate;
            internal bool ThrowOnOpen;

            public object InstantiateUIForm(object uiFormAsset)
            {
                throw new NotSupportedException();
            }

            public IUIForm CreateUIForm(object uiFormInstance, IUIGroup uiGroup,
                bool isNewInstance, object userData)
            {
                if (FailCreate)
                {
                    return null;
                }

                var uiForm = new TestUIForm(uiFormInstance, ThrowOnOpen);
                CreatedForms.Add(uiForm);
                return uiForm;
            }

            public void ReleaseUIForm(object uiFormAsset, object uiFormInstance)
            {
                TestUIForm form = CreatedForms.Find(candidate => ReferenceEquals(candidate.Handle, uiFormInstance));
                ReleaseCalls.Add(new ReleaseCall(
                    uiFormAsset,
                    uiFormInstance,
                    form != null && form.RecycleCount > 0));
            }
        }

        private readonly struct ReleaseCall
        {
            internal ReleaseCall(object asset, object instance, bool recycledBeforeRelease)
            {
                Asset = asset;
                Instance = instance;
                RecycledBeforeRelease = recycledBeforeRelease;
            }

            internal object Asset { get; }
            internal object Instance { get; }
            internal bool RecycledBeforeRelease { get; }
        }

        private sealed class TestUIForm : IUIForm
        {
            private readonly bool _throwOnOpen;

            internal TestUIForm(object handle, bool throwOnOpen)
            {
                Handle = handle;
                _throwOnOpen = throwOnOpen;
            }

            public int SerialId { get; private set; }
            public string UIFormAssetName { get; private set; }
            public object Handle { get; }
            public IUIGroup UIGroup { get; private set; }
            public int DepthInUIGroup { get; private set; }
            public bool PauseCoveredUIForm { get; private set; }
            internal int CloseCount { get; private set; }
            internal int RecycleCount { get; private set; }
            internal bool LastCloseWasShutdown { get; private set; }
            internal bool ThrowOnClose { get; set; }
            internal bool ThrowOnRecycle { get; set; }
            internal Action RecycleAction { get; set; }

            public void OnInit(int serialId, string uiFormAssetName, IUIGroup uiGroup,
                bool pauseCoveredUIForm, bool isNewInstance, object userData)
            {
                SerialId = serialId;
                UIFormAssetName = uiFormAssetName;
                UIGroup = uiGroup;
                PauseCoveredUIForm = pauseCoveredUIForm;
            }

            public void OnRecycle()
            {
                RecycleCount++;
                RecycleAction?.Invoke();
                if (ThrowOnRecycle)
                {
                    throw new InvalidOperationException("Test OnRecycle failure.");
                }
            }

            public void OnOpen(object userData)
            {
                if (_throwOnOpen)
                {
                    throw new InvalidOperationException("Test OnOpen failure.");
                }
            }

            public void OnClose(bool isShutdown, object userData)
            {
                CloseCount++;
                LastCloseWasShutdown = isShutdown;
                if (ThrowOnClose)
                {
                    throw new InvalidOperationException("Test OnClose failure.");
                }
            }

            public void OnPause()
            {
            }

            public void OnResume()
            {
            }

            public void OnCover()
            {
            }

            public void OnReveal()
            {
            }

            public void OnRefocus(object userData)
            {
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
            {
                DepthInUIGroup = depthInUIGroup;
            }

            public TUiBehaviour FindUiBehaviour<TUiBehaviour>() where TUiBehaviour : class
            {
                return null;
            }
        }
    }
}
