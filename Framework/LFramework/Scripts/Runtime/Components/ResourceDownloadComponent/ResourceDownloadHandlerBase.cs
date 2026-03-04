using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 资源下载处理器抽象基类，包含通用的事件分发、进度计算、速度计算和生命周期管理逻辑
    /// </summary>
    public abstract class ResourceDownloadHandlerBase : IResourceDownloadHandler
    {
        // === 受保护字段（子类可访问） ===
        protected readonly string _handlerName;
        protected readonly List<string> _updateLabels;
        protected readonly int _serialID;
        protected readonly bool _autoReleaseHandle;
        protected long _totalDownloadSize;

        // === 私有字段（速度计算） ===
        private float _downloadSpeed;
        private float _lastUpdateTime;
        private long _lastDownloadedBytes;

        // === 事件委托字段 ===
        private EventHandler<ResourcesDownloadFailureEvent> _downloadFailureEventHandler;
        private EventHandler<ResourcesDownloadSuccessfulEvent> _downloadSuccessfulEventHandler;
        private EventHandler<ResourcesDownloadUpdateEvent> _downloadUpdateEventHandler;
        private EventHandler<ResourceDownloadStepEvent> _downloadStepEventHandler;

        // === 公共委托 ===
        public GameFrameworkAction<ResourceDownloadHandlerBase> RemoveHandleAction;

        // === 公共属性 ===
        public string Name => _handlerName;
        public int SerialID => _serialID;
        public float DownloadSpeed => _downloadSpeed;

        // === 事件实现 ===
        public event EventHandler<ResourceDownloadStepEvent> DownloadStepEventHandler
        {
            add => _downloadStepEventHandler += value;
            remove => _downloadStepEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadFailureEvent> DownloadFailureEventHandler
        {
            add => _downloadFailureEventHandler += value;
            remove => _downloadFailureEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadSuccessfulEvent> DownloadSuccessfulEventHandler
        {
            add => _downloadSuccessfulEventHandler += value;
            remove => _downloadSuccessfulEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadUpdateEvent> DownloadUpdateEventHandler
        {
            add => _downloadUpdateEventHandler += value;
            remove => _downloadUpdateEventHandler -= value;
        }

        // === 构造函数 ===
        protected ResourceDownloadHandlerBase(string handlerName, List<string> updateLabels, int serialID,
            bool autoReleaseHandle)
        {
            _serialID = serialID;
            _handlerName = handlerName;
            _updateLabels = updateLabels;
            _autoReleaseHandle = autoReleaseHandle;

            if (updateLabels == null || updateLabels.Count == 0)
            {
                Log.Fatal($"The '{handlerName}' update labels is null.");
            }

            _totalDownloadSize = 0L;
            _downloadSpeed = 0F;
            _lastDownloadedBytes = 0L;
        }

        // === 抽象方法 ===

        /// <summary>
        /// 每帧更新，由 ResourceDownloadComponent 调用
        /// </summary>
        public abstract void OnUpdate(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 开始检查更新并下载资源
        /// </summary>
        public abstract void CheckAndLoadAsync();

        // === 受保护辅助方法 ===

        protected void SendProgress(float progress, float downloadSize = 0)
        {
            if (_downloadUpdateEventHandler != null)
            {
                ResourcesDownloadUpdateEvent arg;
                if (downloadSize > 0)
                {
                    arg = ResourcesDownloadUpdateEvent.Create(progress, ByteToMb(downloadSize),
                        ByteToMb(_totalDownloadSize), _downloadSpeed);
                }
                else
                {
                    arg = ResourcesDownloadUpdateEvent.Create(progress);
                }

                _downloadUpdateEventHandler(this, arg);
                ReferencePool.Release(arg);
            }
        }

        protected void DownloadSuccessful()
        {
            if (_downloadSuccessfulEventHandler != null)
            {
                var arg = ResourcesDownloadSuccessfulEvent.Create(this._serialID);
                _downloadSuccessfulEventHandler(this, arg);
                ReferencePool.Release(arg);
            }

            if (_autoReleaseHandle)
            {
                RemoveHandleAction(this);
            }
        }

        protected void ExceptionFailure(UpdateResultType resultType)
        {
            if (_downloadFailureEventHandler != null)
            {
                var arg = ResourcesDownloadFailureEvent.Create(this._serialID, resultType);
                _downloadFailureEventHandler(this, arg);
                ReferencePool.Release(arg);
            }

            if (_autoReleaseHandle)
            {
                RemoveHandleAction(this);
            }
        }

        protected void StepEvent(ResourceDownloadStep step, object customData = null)
        {
            if (_downloadStepEventHandler != null)
            {
                var arg = ResourceDownloadStepEvent.Create(this._serialID, step, customData);
                _downloadStepEventHandler(this, arg);
                ReferencePool.Release(arg);
            }
        }

        protected void CalculateSpeed(long downloadSize)
        {
            var currentTime = Time.time;
            var timeDelta = currentTime - _lastUpdateTime;
            if (timeDelta <= 0.5f) // 每 0.5 秒计算一次
            {
                return;
            }

            var downloadedBytes = downloadSize;
            var bytesDelta = downloadedBytes - _lastDownloadedBytes;
            _downloadSpeed = bytesDelta / timeDelta; // 计算速度 (B/s)
            _lastDownloadedBytes = downloadedBytes;
            _lastUpdateTime = currentTime;
        }

        protected void LogInfo(string message)
        {
            Log.Info($"Name '{_handlerName}' Message '{message}'");
        }

        protected void LogError(string message)
        {
            Log.Error($"Name '{_handlerName}' Message '{message}'");
        }

        protected static float ByteToMbFloat(float bytes)
        {
            var v = bytes * 1.0f / 1024 / 1024;
            return v;
        }

        protected static string ByteToMb(float bytes)
        {
            var v = ByteToMbFloat(bytes);
            return $"{v:F2} MB";
        }
    }
}
