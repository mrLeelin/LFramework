using GameFramework.Resource;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// YooAsset 资源辅助器（平台初始化 + 查询）
    /// </summary>
    public class YooAssetResourceHelper : ResourceHelperBase
    {
        /// <summary>
        /// YooAsset 包名称（通过 ResourceComponentSetting 配置）
        /// </summary>
        public string PackageName { get; set; } = "DefaultPackage";

        /// <summary>
        /// YooAsset 运行模式
        /// </summary>
        public YooAssetPlayMode PlayMode { get; set; } = YooAssetPlayMode.EditorSimulateMode;

        /// <summary>
        /// 资源服务器地址
        /// </summary>
        public string HostServerUrl { get; set; }

        /// <summary>
        /// 备用服务器地址
        /// </summary>
        public string FallbackHostServerUrl { get; set; }

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public override void InitializeResources(ResourceInitCallBack callback)
        {
            YooAssets.Initialize();
            var package = YooAssets.TryGetPackage(PackageName)
                          ?? YooAssets.CreatePackage(PackageName);
            YooAssets.SetDefaultPackage(package);

            InitializePackageAsync(package, callback);
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public override HasAssetResult HasAsset(string assetName)
        {
            var package = YooAssets.GetPackage(PackageName);
            if (package == null) return HasAssetResult.NotReady;
            return package.CheckLocationValid(assetName)
                ? HasAssetResult.Exist
                : HasAssetResult.NotExist;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Release(object asset)
        {
            // YooAsset 通过 handle.Release() 释放
            // 此处由 AgentHelper 管理 handle 生命周期
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public override void UnloadScene(string sceneAssetName,
            UnloadSceneCallbacks callbacks, object userData)
        {
            var op = SceneManager.UnloadSceneAsync(sceneAssetName);
            if (op == null)
            {
                callbacks.UnloadSceneFailureCallback?.Invoke(sceneAssetName, userData);
                return;
            }
            op.completed += (_) =>
            {
                callbacks.UnloadSceneSuccessCallback?.Invoke(sceneAssetName, userData);
            };
        }

        private async void InitializePackageAsync(ResourcePackage package, ResourceInitCallBack callback)
        {
            InitializationOperation initOperation = null;

            switch (PlayMode)
            {
                case YooAssetPlayMode.EditorSimulateMode:
                    var simulateParams = new EditorSimulateModeParameters();
                    initOperation = package.InitializeAsync(simulateParams);
                    break;

                case YooAssetPlayMode.OfflinePlayMode:
                    var offlineParams = new OfflinePlayModeParameters();
                    initOperation = package.InitializeAsync(offlineParams);
                    break;

                case YooAssetPlayMode.HostPlayMode:
                    var hostParams = new HostPlayModeParameters();
                    initOperation = package.InitializeAsync(hostParams);
                    break;

                case YooAssetPlayMode.WebPlayMode:
                    var webParams = new WebPlayModeParameters();
                    initOperation = package.InitializeAsync(webParams);
                    break;
            }

            if (initOperation != null)
            {
                await initOperation.Task;
                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    callback?.ResourceInitSuccessCallBack?.Invoke();
                }
                else
                {
                    callback?.ResourceInitFailureCallBack?.Invoke(
                        initOperation.Error ?? "YooAsset initialization failed.");
                }
            }
            else
            {
                callback?.ResourceInitFailureCallBack?.Invoke(
                    "Unknown YooAsset play mode, initialization failed.");
            }
        }
    }
}
