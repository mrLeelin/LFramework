using System;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using LFramework.Runtime;
using LFramework.Runtime.LaunchPipeline.Basic;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 版本检查启动任务。
    /// 通过 <see cref="WebRequestComponent"/> 发起 HTTP 请求获取远程版本信息，
    /// 解析 <see cref="IGameVersionConfig"/> JSON 数据，比较客户端与远程版本，决定后续流程。
    /// </summary>
    public class CheckVersionTask : RetryableLaunchTaskBase
    {
        private readonly ICheckVersionConfigProvider _configProvider;

        /// <summary>
        /// 游戏设置，包含版本 URL、应用版本等配置
        /// </summary>
        [Inject] private GameSetting _gameSetting;

        /// <summary>
        /// 设置组件，用于读写本地存储的资源版本
        /// </summary>
        [Inject] private SettingComponent _settingComponent;

        /// <summary>
        /// 事件组件，用于监听 Web 请求事件
        /// </summary>
        [Inject] private EventComponent _eventComponent;

        /// <summary>
        /// Web 请求组件，用于发起 HTTP 请求
        /// </summary>
        [Inject] private WebRequestComponent _webRequestComponent;

        /// <summary>
        /// 使用默认版本检查配置策略创建任务。
        /// </summary>
        public CheckVersionTask() : this(new DefaultCheckVersionConfigProvider())
        {
        }

        /// <summary>
        /// 使用指定的版本检查配置策略创建任务。
        /// </summary>
        /// <param name="configProvider">版本检查配置策略。为空时使用默认策略。</param>
        public CheckVersionTask(ICheckVersionConfigProvider configProvider)
        {
            _configProvider = configProvider ?? new DefaultCheckVersionConfigProvider();
        }

        /// <summary>
        /// 任务名称
        /// </summary>
        public override string TaskName => "CheckVersion";

        /// <summary>
        /// 任务描述
        /// </summary>
        public override string Description => "检查版本更新";

        /// <summary>
        /// 异步执行版本检查任务。
        /// 通过 WebRequestComponent 发起请求，监听成功/失败事件，
        /// 解析远程版本信息并与客户端版本比较。
        /// </summary>
        /// <param name="context">启动管线上下文，用于写入版本检查结果。</param>
        /// <returns>任务执行结果。</returns>
        protected override async UniTask<LaunchTaskResult> ExecuteOnceAsync(LaunchContext context)
        {
            try
            {
                if (_gameSetting == null)
                {
                    SetFailedResult(context, "GameSetting is null");
                    return LaunchTaskResult.CreateFailed(TaskName, "GameSetting is null");
                }

                // 1. 构造版本检查 URL
                var url = _configProvider.GetVersionUrl(_gameSetting, _settingComponent);
                if (string.IsNullOrWhiteSpace(url))
                {
                    SetFailedResult(context, "version check url is empty");
                    return LaunchTaskResult.CreateFailed(TaskName, "version check url is empty");
                }

                Log.Info("[CheckVersionTask] 版本检查 URL: {0}", url);
                context.ProgressReporter.ReportProgress(0.1f, "正在请求版本信息...");

                // 2. 使用 UniTaskCompletionSource 将事件回调转换为异步等待
                var tcs = new UniTaskCompletionSource<byte[]>();
                var requestSerialId = _webRequestComponent.AddWebRequest(url);

                // 成功回调
                void OnWebRequestSuccess(object sender, GameEventArgs e)
                {
                    var arg = e as WebRequestSuccessEventArgs;
                    if (arg == null || arg.SerialId != requestSerialId) return;
                    tcs.TrySetResult(arg.GetWebResponseBytes());
                }

                // 失败回调
                void OnWebRequestFailure(object sender, GameEventArgs e)
                {
                    var arg = e as WebRequestFailureEventArgs;
                    if (arg == null || arg.SerialId != requestSerialId) return;
                    tcs.TrySetException(new Exception(arg.ErrorMessage));
                }

                // 注册事件
                _eventComponent.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
                _eventComponent.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

                byte[] bytes;
                try
                {
                    // 等待请求完成
                    bytes = await tcs.Task;
                }
                finally
                {
                    // 无论成功失败都取消订阅
                    _eventComponent.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
                    _eventComponent.Unsubscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);
                }

                // 3. 检查响应数据
                context.ProgressReporter.ReportProgress(0.5f, "正在解析版本数据...");
                if (bytes == null || bytes.Length == 0)
                {
                    Log.Error("[CheckVersionTask] 响应数据为空");
                    SetFailedResult(context, "响应数据为空");
                    return LaunchTaskResult.CreateFailed(TaskName, "响应数据为空");
                }

                var jsonStr = System.Text.Encoding.UTF8.GetString(bytes);
                if (string.IsNullOrEmpty(jsonStr))
                {
                    Log.Error("[CheckVersionTask] JSON 数据为空");
                    SetFailedResult(context, "JSON 数据为空");
                    return LaunchTaskResult.CreateFailed(TaskName, "JSON 数据为空");
                }

                Log.Info("[CheckVersionTask] The parsed game version is: {0}", jsonStr);
                // 4. 解析远程版本信息
                IGameVersionConfig remoteVersionInfo;
                try
                {
                    remoteVersionInfo = _configProvider.ParseRemoteGameVersion(jsonStr);
                }
                catch (Exception parseEx)
                {
                    Log.Error("[CheckVersionTask] JSON 解析失败: {0}", parseEx.Message);
                    SetFailedResult(context, $"JSON 解析失败: {parseEx.Message}");
                    return LaunchTaskResult.CreateFailed(TaskName, $"JSON 解析失败: {parseEx.Message}");
                }

                if (remoteVersionInfo == null)
                {
                    Log.Error("[CheckVersionTask] 解析的远程版本信息为空");
                    SetFailedResult(context, "解析的远程版本信息为空");
                    return LaunchTaskResult.CreateFailed(TaskName, "解析的远程版本信息为空");
                }

                // 6. 构造客户端版本信息用于比较
                var clientVersionConfig = _configProvider.BuildLocalGameVersion(_gameSetting, _settingComponent);
                Log.Info("[CheckVersionTask] 客户端版本: {0}, 远程版本: {1}",
                    clientVersionConfig, remoteVersionInfo);
                // 7. 版本比较
                context.ProgressReporter.ReportProgress(0.8f, "正在比较版本...");
                var (result, errorMessage) = _configProvider.CompareGameVersion(
                    remoteVersionInfo, clientVersionConfig);

                // 8. 根据比较结果处理
                switch (result)
                {
                    case GameVersionCompareResult.ForceUpdate:
                        Log.Error("[CheckVersionTask] Need force update.");
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.ForceUpdate,
                            RemoteVersion = remoteVersionInfo,
                        };
                        return LaunchTaskResult.CreateFailed(TaskName,
                            $"Need force update");

                    case GameVersionCompareResult.Update:
                        Log.Info("[CheckVersionTask] 需要热更新资源");
                        ApplyRemoteGameVersion(remoteVersionInfo, _gameSetting, _settingComponent);
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.HotUpdate,
                            RemoteVersion = remoteVersionInfo,
                        };
                        return LaunchTaskResult.CreateSuccess(TaskName);

                    case GameVersionCompareResult.NoUpdate:
                        Log.Info("[CheckVersionTask] 无需更新");
                        ApplyRemoteGameVersion(remoteVersionInfo, _gameSetting, _settingComponent);
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.NoUpdate,
                            RemoteVersion = remoteVersionInfo,
                        };
                        return LaunchTaskResult.CreateSuccess(TaskName);

                    case GameVersionCompareResult.Invalid:
                        Log.Error("[CheckVersionTask] 版本比较异常: {0}", errorMessage);
                        SetFailedResult(context, errorMessage);
                        return LaunchTaskResult.CreateFailed(TaskName, errorMessage);

                    default:
                        Log.Error("[CheckVersionTask] 未知的版本比较结果: {0}", result);
                        SetFailedResult(context, $"未知的版本比较结果: {result}");
                        return LaunchTaskResult.CreateFailed(TaskName, $"未知的版本比较结果: {result}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[CheckVersionTask] 版本检查异常: {0}", ex);
                SetFailedResult(context, ex.Message);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
        
        /// <summary>
        /// 默认将资源版本、CDN 和服务器地址写回 GameSetting。
        /// </summary>
        protected virtual void ApplyRemoteGameVersion(
            IGameVersionConfig remote,
            GameSetting gameSetting,
            SettingComponent settingComponent)
        {
            gameSetting.SetResourceVersion(settingComponent, remote.ResourceVersion);
            if (remote is IGameVersionEndpointConfig endpointConfig)
            {
                gameSetting.ip = endpointConfig.LogicIp;
                gameSetting.webSocketIp = endpointConfig.WebSocketIp;
                gameSetting.cdnUrl = endpointConfig.CdnUrl;
            }

            Log.Info("[CheckVersionTask] 更新 GameSetting 完成: {0}", gameSetting);
        }

        protected override bool ShouldRetry(LaunchTaskResult result, Exception exception,
            LaunchErrorCategory errorCategory,
            LaunchContext context)
        {
            return errorCategory == LaunchErrorCategory.Network ||
                   errorCategory == LaunchErrorCategory.Timeout ||
                   errorCategory == LaunchErrorCategory.Server;
        }

        protected override LaunchErrorCategory ClassifyFailure(LaunchTaskResult result, Exception exception,
            LaunchContext context)
        {
            string errorMessage = result?.ErrorMessage ?? exception?.Message;
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return LaunchErrorCategory.Unknown;
            }

            if (ContainsAny(errorMessage, "timeout", "timed out", "超时"))
            {
                return LaunchErrorCategory.Timeout;
            }

            if (ContainsAny(errorMessage, "json", "解析"))
            {
                return LaunchErrorCategory.Parse;
            }

            if (ContainsAny(errorMessage, "versionurl", "defaultconfig", "配置为空", "gamesetting is null"))
            {
                return LaunchErrorCategory.Config;
            }

            if (ContainsAny(errorMessage, "500", "502", "503", "504", "server", "服务"))
            {
                return LaunchErrorCategory.Server;
            }

            if (ContainsAny(errorMessage, "network", "socket", "dns", "resolve", "连接", "reachable", "web request"))
            {
                return LaunchErrorCategory.Network;
            }

            return LaunchErrorCategory.Unknown;
        }

        protected override UniTask BeforeRetryAsync(int attempt, LaunchContext context, LaunchTaskResult result,
            Exception exception)
        {
            string message = $"版本检查失败，正在重试 ({attempt}/{GetMaxRetryCount(context)})...";
            Log.Warning("[CheckVersionTask] {0} Error: {1}", message, result?.ErrorMessage ?? exception?.Message);
            context.ProgressReporter.ReportProgress(0.1f, message);
            return UniTask.CompletedTask;
        }

        protected override int GetMaxRetryCount(LaunchContext context)
        {
            return context.GetCustomData("CheckVersionRetryCount", context.DefaultRetryCount);
        }

        /// <summary>
        /// 设置失败的版本检查结果到上下文
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <param name="errorMessage">错误信息</param>
        private void SetFailedResult(LaunchContext context, string errorMessage)
        {
            context.VersionCheckResult = new VersionCheckResult
            {
                ResultType = VersionCheckResultType.Failed,
                ErrorMessage = errorMessage
            };
            OnFailedResult(context, errorMessage);
        }

        /// <summary>
        /// 失败回调
        /// </summary>
        /// <param name="context"></param>
        /// <param name="errorMessage"></param>
        protected virtual void OnFailedResult(LaunchContext context, string errorMessage)
        {
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            foreach (var value in values)
            {
                if (source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}