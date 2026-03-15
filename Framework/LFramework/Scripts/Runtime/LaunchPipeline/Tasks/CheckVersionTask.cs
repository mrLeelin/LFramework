using System;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 版本检查启动任务。
    /// 通过 <see cref="WebRequestComponent"/> 发起 HTTP 请求获取远程版本信息，
    /// 解析 <see cref="GameVersion"/> JSON 数据，比较客户端与远程版本，决定后续流程。
    /// </summary>
    public class CheckVersionTask : ILaunchTask
    {
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
        /// 任务名称
        /// </summary>
        public string TaskName => "CheckVersion";

        /// <summary>
        /// 任务描述
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
        /// 通过 WebRequestComponent 发起请求，监听成功/失败事件，
        /// 解析远程版本信息并与客户端版本比较。
        /// </summary>
        /// <param name="context">启动管线上下文，用于写入版本检查结果。</param>
        /// <returns>任务执行结果。</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                // 1. 构造版本检查 URL
                var url = $"{_gameSetting.versionUrl}/{_gameSetting.GetVersionRootDir()}";
                Log.Info("[CheckVersionTask] 版本检查 URL: {0}", url);

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

                // 4. 解析远程版本信息
                GameVersion remoteVersionInfo;
                try
                {
                    remoteVersionInfo = JsonUtility.FromJson<GameVersion>(jsonStr);
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

                // 5. 获取远程版本配置（支持白名单）
                var remoteVersionConfig = GetRemoteVersionConfig(remoteVersionInfo);
                if (remoteVersionConfig == null)
                {
                    Log.Error("[CheckVersionTask] 远程版本配置为空");
                    SetFailedResult(context, "远程版本配置为空");
                    return LaunchTaskResult.CreateFailed(TaskName, "远程版本配置为空");
                }

                // 6. 构造客户端版本信息用于比较
                var clientVersionConfig = new GameVersionConfig
                {
                    resourceVersion = _gameSetting.GetResourceVersion(_settingComponent)
                };
                var clientVersionInfo = new GameVersion
                {
                    appVersion = _gameSetting.appVersion,
                    defaultConfig = clientVersionConfig
                };

                Log.Info("[CheckVersionTask] 客户端版本: {0}, 远程版本: {1}",
                    clientVersionInfo, remoteVersionInfo);

                // 7. 版本比较
                var (result, errorMessage) = GameVersion.IsNeedUpdate(
                    remoteVersionInfo, clientVersionInfo, remoteVersionConfig, clientVersionConfig);

                // 8. 根据比较结果处理
                switch (result)
                {
                    case Result.ForceUpdate:
                        Log.Error("[CheckVersionTask] 需要强制更新，下载地址: {0}",
                            remoteVersionInfo.downloadPackage);
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.ForceUpdate,
                            DownloadUrl = remoteVersionInfo.downloadPackage,
                            RemoteVersion = remoteVersionInfo,
                            RemoteVersionConfig = remoteVersionConfig
                        };
                        return LaunchTaskResult.CreateFailed(TaskName,
                            $"需要强制更新，下载地址: {remoteVersionInfo.downloadPackage}");

                    case Result.Update:
                        Log.Info("[CheckVersionTask] 需要热更新资源");
                        UpdateGameSettings(remoteVersionConfig);
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.HotUpdate,
                            RemoteVersion = remoteVersionInfo,
                            RemoteVersionConfig = remoteVersionConfig
                        };
                        return LaunchTaskResult.CreateSuccess(TaskName);

                    case Result.NoUpdate:
                        Log.Info("[CheckVersionTask] 无需更新");
                        UpdateGameSettings(remoteVersionConfig);
                        context.VersionCheckResult = new VersionCheckResult
                        {
                            ResultType = VersionCheckResultType.NoUpdate,
                            RemoteVersion = remoteVersionInfo,
                            RemoteVersionConfig = remoteVersionConfig
                        };
                        return LaunchTaskResult.CreateSuccess(TaskName);

                    case Result.Exception:
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
        /// 更新游戏设置，将远程版本配置写入 GameSetting
        /// </summary>
        /// <param name="remoteConfig">远程版本配置</param>
        private void UpdateGameSettings(GameVersionConfig remoteConfig)
        {
            _gameSetting.SetResourceVersion(_settingComponent, remoteConfig.resourceVersion);
            _gameSetting.ip = remoteConfig.logicIp;
            _gameSetting.webSocketIp = remoteConfig.webSocketIp;
            _gameSetting.cdnUrl = remoteConfig.cdnUrl;
            Log.Info("[CheckVersionTask] 更新 GameSetting 完成: {0}", _gameSetting);
        }

        /// <summary>
        /// 获取远程版本配置，支持白名单用户使用独立配置
        /// </summary>
        /// <param name="remoteVersionInfo">远程版本信息</param>
        /// <returns>匹配的版本配置，失败返回 null</returns>
        private GameVersionConfig GetRemoteVersionConfig(GameVersion remoteVersionInfo)
        {
            if (remoteVersionInfo.defaultConfig == null ||
                string.IsNullOrEmpty(remoteVersionInfo.defaultConfig.resourceVersion))
            {
                Log.Error("[CheckVersionTask] 远程 defaultConfig 为空");
                return null;
            }

            if (remoteVersionInfo.whiteListConfig == null ||
                string.IsNullOrEmpty(remoteVersionInfo.whiteListConfig.resourceVersion))
            {
                Log.Info("[CheckVersionTask] 白名单配置为空，使用 defaultConfig");
                return remoteVersionInfo.defaultConfig;
            }

            var isWhiteListUser = IsWhiteListUser(remoteVersionInfo);
            Log.Info("[CheckVersionTask] 是否白名单用户: {0}", isWhiteListUser);
            return isWhiteListUser ? remoteVersionInfo.whiteListConfig : remoteVersionInfo.defaultConfig;
        }

        /// <summary>
        /// 判断当前设备是否在白名单中
        /// </summary>
        /// <param name="remoteVersionInfo">远程版本信息</param>
        /// <returns>是否为白名单用户</returns>
        private bool IsWhiteListUser(GameVersion remoteVersionInfo)
        {
            if (string.IsNullOrEmpty(remoteVersionInfo.userList))
            {
                return false;
            }

            var deviceId = SystemInfo.deviceUniqueIdentifier;
            Log.Info("[CheckVersionTask] 设备 ID: {0}", deviceId);

            var userList = remoteVersionInfo.userList.Split(',');
            foreach (var user in userList)
            {
                if (user.Equals(deviceId))
                {
                    return true;
                }
            }

            return false;
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
        }
    }
}
