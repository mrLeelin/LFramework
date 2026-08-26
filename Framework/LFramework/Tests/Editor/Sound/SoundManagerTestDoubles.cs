using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Resource;
using GameFramework.Sound;

namespace LFramework.Editor.Tests.Sound
{
    internal sealed class FakeResourceManager : IResourceManager
    {
        internal sealed class PendingLoad
        {
            internal string AssetName;
            internal Type AssetType;
            internal int Priority;
            internal LoadAssetCallbacks Callbacks;
            internal object UserData;
        }

        internal readonly List<PendingLoad> Loads = new List<PendingLoad>();
        internal readonly List<object> UnloadedAssets = new List<object>();

        public string ReadOnlyPath => string.Empty;
        public string ReadWritePath => string.Empty;
        public ResourceMode ResourceMode => ResourceMode.Unspecified;

        public void LoadAsset(string assetName, Type assetType, int priority,
            LoadAssetCallbacks callbacks, object userData)
        {
            Loads.Add(new PendingLoad
            {
                AssetName = assetName,
                AssetType = assetType,
                Priority = priority,
                Callbacks = callbacks,
                UserData = userData
            });
        }

        internal void CompleteSuccess(int index, object asset, float duration = 0.1f)
        {
            PendingLoad load = Loads[index];
            load.Callbacks.LoadAssetSuccessCallback(
                load.AssetName, asset, duration, load.UserData);
        }

        internal void CompleteFailure(int index, string message)
        {
            PendingLoad load = Loads[index];
            load.Callbacks.LoadAssetFailureCallback(
                load.AssetName, LoadResourceStatus.AssetError, message, load.UserData);
        }

        internal void ReportProgress(int index, float progress)
        {
            PendingLoad load = Loads[index];
            load.Callbacks.LoadAssetUpdateCallback(load.AssetName, progress, load.UserData);
        }

        internal void ReportDependency(int index, string dependencyAssetName)
        {
            PendingLoad load = Loads[index];
            load.Callbacks.LoadAssetDependencyAssetCallback(
                load.AssetName, dependencyAssetName, 1, 1, load.UserData);
        }

        public void UnloadAsset(object asset)
        {
            UnloadedAssets.Add(asset);
        }

        public void SetReadOnlyPath(string readOnlyPath)
        {
            throw new NotSupportedException();
        }

        public void SetReadWritePath(string readWritePath)
        {
            throw new NotSupportedException();
        }

        public void SetResourceMode(ResourceMode resourceMode)
        {
            throw new NotSupportedException();
        }

        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            throw new NotSupportedException();
        }

        public void InitResources(InitResourcesCompleteCallback callback)
        {
            throw new NotSupportedException();
        }

        public HasAssetResult HasAsset(string assetName)
        {
            throw new NotSupportedException();
        }

        public void LoadAsset(string assetName, Type assetType, string packageId, int priority,
            LoadAssetCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void LoadScene(string sceneAssetName, int priority,
            LoadSceneCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void LoadScene(string sceneAssetName, string packageId, int priority,
            LoadSceneCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void UnloadScene(string sceneAssetName,
            UnloadSceneCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void LoadBinary(string binaryAssetName, string packageId,
            LoadBinaryCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void InstantiateAsset(string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        public void InstantiateAsset(string assetName, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class FakeSoundHelper : ISoundHelper
    {
        internal readonly List<object> ReleasedAssets = new List<object>();
        internal Action<object> ReleaseObserver;

        public void ReleaseSoundAsset(object soundAsset)
        {
            ReleaseObserver?.Invoke(soundAsset);
            ReleasedAssets.Add(soundAsset);
        }
    }

    internal sealed class FakeSoundGroupHelper : ISoundGroupHelper
    {
    }

    internal sealed class FakeLogHelper : GameFrameworkLog.ILogHelper
    {
        internal readonly List<string> Warnings = new List<string>();
        internal Action<GameFrameworkLogLevel, object> LogObserver;

        public void Log(GameFrameworkLogLevel level, object message)
        {
            LogObserver?.Invoke(level, message);
            if (level == GameFrameworkLogLevel.Warning)
            {
                Warnings.Add(message?.ToString());
            }
        }
    }

    internal sealed class FakeSoundAgentHelper : ISoundAgentHelper
    {
        public bool IsPlaying { get; private set; }
        public float Length { get; set; } = 1f;
        public float Time { get; set; }
        public bool Mute { get; set; }
        public bool Loop { get; set; }
        public int Priority { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public float PanStereo { get; set; }
        public float SpatialBlend { get; set; }
        public float MaxDistance { get; set; }
        public float DopplerLevel { get; set; }
        public int PlayCount { get; private set; }
        public int StopCount { get; private set; }
        public int ResetCount { get; private set; }
        public object SoundAsset { get; private set; }
        internal bool AutoCompleteOnStop { get; set; } = true;
        internal bool RejectSoundAsset { get; set; }

        public event EventHandler<ResetSoundAgentEventArgs> ResetSoundAgent;

        public void Play(float fadeInSeconds)
        {
            PlayCount++;
            IsPlaying = true;
        }

        public void Stop(float fadeOutSeconds)
        {
            StopCount++;
            if (AutoCompleteOnStop)
            {
                Complete();
            }
        }

        public void Pause(float fadeOutSeconds)
        {
            IsPlaying = false;
        }

        public void Resume(float fadeInSeconds)
        {
            IsPlaying = true;
        }

        public void Reset()
        {
            ResetCount++;
            IsPlaying = false;
            SoundAsset = null;
        }

        public bool SetSoundAsset(object soundAsset)
        {
            SoundAsset = soundAsset;
            return soundAsset != null && !RejectSoundAsset;
        }

        internal void Complete()
        {
            IsPlaying = false;
            ResetSoundAgentEventArgs args = ResetSoundAgentEventArgs.Create();
            try
            {
                ResetSoundAgent?.Invoke(this, args);
            }
            finally
            {
                ReferencePool.Release(args);
            }
        }
    }
}
