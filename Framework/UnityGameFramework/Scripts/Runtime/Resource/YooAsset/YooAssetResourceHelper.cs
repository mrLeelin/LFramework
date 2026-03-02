using System;
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

            op.completed += (_) => { callbacks.UnloadSceneSuccessCallback?.Invoke(sceneAssetName, userData); };
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public override void LoadAsset(string assetName, Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadAssetAsync(assetName, assetType);

            handle.Completed += (op) =>
            {
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.AssetObject, 0f, userData);
                }
                else
                {
                    callbacks.LoadAssetFailureCallback?.Invoke(
                        assetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public override void LoadScene(string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadSceneAsync(sceneAssetName);

            handle.Completed += (op) =>
            {
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadSceneSuccessCallback?.Invoke(
                        sceneAssetName, 0f, userData);
                }
                else
                {
                    callbacks.LoadSceneFailureCallback?.Invoke(
                        sceneAssetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load scene failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public override void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadRawFileAsync(binaryAssetName);

            handle.Completed += (op) =>
            {
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadBinarySuccessCallback?.Invoke(
                        binaryAssetName, op.GetRawFileData(), 0f, userData);
                }
                else
                {
                    callbacks.LoadBinaryFailureCallback?.Invoke(
                        binaryAssetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load binary failed.", userData);
                }
            };
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        public override void InstantiateAsset(string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadAssetAsync<GameObject>(assetName);

            handle.Completed += (op) =>
            {
                if (op.Status == EOperationStatus.Succeed)
                {
                    var instance = handle.InstantiateAsync();
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, instance, 0f, userData);
                }
                else
                {
                    callbacks.LoadAssetFailureCallback?.Invoke(
                        assetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Instantiate failed.", userData);
                }
            };
        }

        private async void InitializePackageAsync(ResourcePackage package, ResourceInitCallBack callback)
        {
            InitializationOperation initOperation = null;

            switch (PlayMode)
            {
                case YooAssetPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(PackageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var createParameters = new EditorSimulateModeParameters();
                    createParameters.EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.OfflinePlayMode:
                {
                    var createParameters = new OfflinePlayModeParameters();
                    createParameters.BuildinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.HostPlayMode:
                {
                    string defaultHostServer = "";//GetHostServerURL();
                    string fallbackHostServer = "";// GetHostServerURL();
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    createParameters.CacheFileSystemParameters =
                        FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.WebPlayMode:
                {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                    var createParameters = new WebPlayModeParameters();
			        string defaultHostServer = GetHostServerURL();
                    string fallbackHostServer = GetHostServerURL();
                    string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    createParameters.WebServerFileSystemParameters =
                    WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
                    initOperation = package.InitializeAsync(createParameters);
#else
                    var createParameters = new WebPlayModeParameters();
                    createParameters.WebServerFileSystemParameters =
                        FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    initOperation = package.InitializeAsync(createParameters);
#endif
                }
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