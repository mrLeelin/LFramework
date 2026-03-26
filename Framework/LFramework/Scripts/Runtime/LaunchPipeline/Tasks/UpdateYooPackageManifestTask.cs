#if YOOASSET_SUPPORT
using System;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using UnityGameFramework.Runtime;
using YooAsset;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// YooAsset 资源包清单更新任务。
    /// </summary>
    public class UpdateYooPackageManifestTask : ILaunchTask
    {
        [Inject] private ResourceComponent _resourceComponent;

        public string TaskName => "UpdateYooPackageManifest";
        public string Description => "更新 YooAsset 资源包清单";

        public bool CanExecute(LaunchContext context)
        {
            return _resourceComponent.ResourceMode == ResourceMode.YooAsset;
        }

        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                var packageName = _resourceComponent.YooAssetPackageName;
                var package = YooAssets.GetPackage(packageName);

                context.ProgressReporter.ReportProgress(0f, "正在请求资源包版本...");
                Log.Info("[UpdateYooPackageManifestTask] 请求资源包版本, 包名: {0}", packageName);
                var versionOperation = package.RequestPackageVersionAsync();
                await versionOperation.Task;

                if (versionOperation.Status != EOperationStatus.Succeed)
                {
                    Log.Error("[UpdateYooPackageManifestTask] 请求版本失败: {0}", versionOperation.Error);
                    return LaunchTaskResult.CreateFailed(TaskName, versionOperation.Error);
                }

                var packageVersion = versionOperation.PackageVersion;
                Log.Info("[UpdateYooPackageManifestTask] 获取到版本: {0}", packageVersion);

                context.ProgressReporter.ReportProgress(0.5f, $"正在更新资源清单 v{packageVersion}...");
                var manifestOperation = package.UpdatePackageManifestAsync(packageVersion);
                await manifestOperation.Task;

                if (manifestOperation.Status != EOperationStatus.Succeed)
                {
                    Log.Error("[UpdateYooPackageManifestTask] 更新清单失败: {0}", manifestOperation.Error);
                    return LaunchTaskResult.CreateFailed(TaskName, manifestOperation.Error);
                }

                Log.Info("[UpdateYooPackageManifestTask] 清单更新完成, 版本: {0}", packageVersion);
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                Log.Error("[UpdateYooPackageManifestTask] 异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}
#endif
