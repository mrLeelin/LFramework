using System;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using UnityEngine.Networking;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 版本检查启动任务。
    /// 通过 <see cref="IVersionCheckProvider"/> 获取版本检查 URL，发起 HTTP 请求，
    /// 解析响应数据并根据检查结果决定后续流程。
    /// </summary>
    public class CheckVersionTask : ILaunchTask
    {
        /// <summary>
        /// 版本检查提供者，通过 Zenject 依赖注入获取。
        /// </summary>
        [Inject]
        private IVersionCheckProvider _versionCheckProvider;

        /// <summary>
        /// 任务名称。
        /// </summary>
        public string TaskName => "CheckVersion";

        /// <summary>
        /// 任务描述。
        /// </summary>
        public string Description => "检查版本更新";

        /// <summary>
        /// 判断任务是否可以执行。版本检查任务始终可以执行。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>始终返回 <c>true</c>。</returns>
        public bool CanExecute(LaunchContext context)
        {
            return true;
        }

        /// <summary>
        /// 异步执行版本检查任务。
        /// 获取版本检查 URL，发起 HTTP 请求，解析响应并根据结果类型返回相应的任务结果。
        /// </summary>
        /// <param name="context">启动管线上下文，用于写入版本检查结果。</param>
        /// <returns>任务执行结果。</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                // 1. 获取版本检查 URL
                var url = _versionCheckProvider.GetVersionCheckUrl();
                Log.Info("[CheckVersionTask] 版本检查 URL: {0}", url);

                // 2. 发起 HTTP 请求
                using (var webRequest = UnityWebRequest.Get(url))
                {
                    await webRequest.SendWebRequest().ToUniTask();

                    // 3. 检查请求结果
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        Log.Error("[CheckVersionTask] HTTP 请求失败: {0}", webRequest.error);
                        return LaunchTaskResult.CreateFailed(TaskName, webRequest.error);
                    }

                    Log.Info("[CheckVersionTask] HTTP 请求成功，开始解析响应数据");

                    // 4. 解析响应数据
                    var versionCheckResult = _versionCheckProvider.ParseAndCheck(webRequest.downloadHandler.data);

                    // 5. 将结果写入上下文
                    context.VersionCheckResult = versionCheckResult;

                    Log.Info("[CheckVersionTask] 版本检查结果类型: {0}", versionCheckResult.ResultType);

                    // 6. 根据结果类型返回相应结果
                    switch (versionCheckResult.ResultType)
                    {
                        case VersionCheckResultType.NoUpdate:
                        case VersionCheckResultType.HotUpdate:
                            _versionCheckProvider.OnVersionCheckCompleted(versionCheckResult);
                            Log.Info("[CheckVersionTask] 版本检查完成，结果: {0}", versionCheckResult.ResultType);
                            return LaunchTaskResult.CreateSuccess(TaskName);

                        case VersionCheckResultType.ForceUpdate:
                            Log.Error("[CheckVersionTask] 需要强制更新，下载地址: {0}", versionCheckResult.DownloadUrl);
                            return LaunchTaskResult.CreateFailed(TaskName,
                                $"需要强制更新，下载地址: {versionCheckResult.DownloadUrl}");

                        case VersionCheckResultType.Failed:
                            Log.Error("[CheckVersionTask] 版本检查失败: {0}", versionCheckResult.ErrorMessage);
                            return LaunchTaskResult.CreateFailed(TaskName, versionCheckResult.ErrorMessage);

                        default:
                            Log.Error("[CheckVersionTask] 未知的版本检查结果类型: {0}", versionCheckResult.ResultType);
                            return LaunchTaskResult.CreateFailed(TaskName,
                                $"未知的版本检查结果类型: {versionCheckResult.ResultType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[CheckVersionTask] 版本检查异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}
