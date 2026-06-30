#if YOOASSET_SUPPORT
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using LFramework.Runtime.LaunchPipeline.Basic;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;
using YooAsset;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// Updates YooAsset package manifests before startup continues.
    /// </summary>
    public partial class UpdateYooPackageManifestTask : LaunchTaskBase
    {
        [Inject] private ResourceComponent _resourceComponent;

        public override string TaskName => "UpdateYooPackageManifest";
        public override string Description => "更新 YooAsset 资源包清单";

        public override bool CanExecute(LaunchContext context)
        {
            return _resourceComponent.ResourceMode == ResourceMode.YooAsset;
        }

        public override async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                ResourceComponentSetting setting = SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
                if (setting == null)
                {
                    return LaunchTaskResult.CreateFailed(TaskName, "ResourceComponentSetting is null.");
                }

                List<PackageDefinition> packages = YooAssetMultiPackageUtility.CollectManifestUpdatePackages(
                    setting,
                    Application.platform,
                    GetCurrentChannel());
                if (packages.Count == 0)
                {
                    Log.Info("[UpdateYooPackageManifestTask] No manifest update packages were resolved.");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }

                for (int i = 0; i < packages.Count; i++)
                {
                    PackageDefinition packageDefinition = packages[i];
                    context.ProgressReporter.ReportProgress(
                        (float)i / packages.Count,
                        $"Updating manifest for '{packageDefinition.packageId}'...");

                    ResourcePackage package = YooAssets.GetPackage(packageDefinition.yooPackageName);
                    if (package == null)
                    {
                        return LaunchTaskResult.CreateFailed(
                            TaskName,
                            $"Package '{packageDefinition.packageId}' ({packageDefinition.yooPackageName}) is not initialized.");
                    }

                    Log.Info(
                        "[UpdateYooPackageManifestTask] Request package version. packageId: {0}, packageName: {1}",
                        packageDefinition.packageId,
                        packageDefinition.yooPackageName);
                    RequestPackageVersionOperation versionOperation = package.RequestPackageVersionAsync();
                    await versionOperation.Task;

                    if (versionOperation.Status != EOperationStatus.Succeed)
                    {
                        Log.Error(
                            "[UpdateYooPackageManifestTask] Request package version failed. packageId: {0}, error: {1}",
                            packageDefinition.packageId,
                            versionOperation.Error);
                        return LaunchTaskResult.CreateFailed(TaskName, versionOperation.Error);
                    }

                    string packageVersion = versionOperation.PackageVersion;
                    UpdatePackageManifestOperation manifestOperation = package.UpdatePackageManifestAsync(packageVersion);
                    await manifestOperation.Task;

                    if (manifestOperation.Status != EOperationStatus.Succeed)
                    {
                        Log.Error(
                            "[UpdateYooPackageManifestTask] Update manifest failed. packageId: {0}, error: {1}",
                            packageDefinition.packageId,
                            manifestOperation.Error);
                        return LaunchTaskResult.CreateFailed(TaskName, manifestOperation.Error);
                    }

                    Log.Info(
                        "[UpdateYooPackageManifestTask] Manifest updated. packageId: {0}, version: {1}",
                        packageDefinition.packageId,
                        packageVersion);
                }

                context.ProgressReporter.ReportProgress(1f, "Refreshing route index...");
                await _resourceComponent.RefreshRouteIndexAsync();
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                Log.Error("[UpdateYooPackageManifestTask] Exception: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }

        private static string GetCurrentChannel()
        {
            GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
            return gameSetting != null ? gameSetting.channel : string.Empty;
        }
    }
}
#endif
