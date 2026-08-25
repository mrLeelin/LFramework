//------------------------------------------------------------
// Game Framework
// Copyright (C) 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Sound
{
    internal sealed partial class SoundManager : GameFrameworkModule, ISoundManager
    {
        private enum SoundPlayRequestState : byte
        {
            Created = 0,
            WaitingAsset,
            WaitingAgent,
            Started,
            Cancelled,
            Failed
        }

        private sealed class SoundPlayRequest : IReference
        {
            public int SerialId;
            public string SoundAssetName;
            public SoundGroup SoundGroup;
            public int ResourcePriority;
            public PlaySoundParams PlaySoundParams;
            public object UserData;
            public double RequestedRealTime;
            public bool WaitedForLoad;
            public SoundPlayRequestState State;

            public static SoundPlayRequest Create(int serialId, string soundAssetName, SoundGroup soundGroup, int resourcePriority, PlaySoundParams playSoundParams, object userData, double requestedRealTime)
            {
                SoundPlayRequest request = ReferencePool.Acquire<SoundPlayRequest>();
                request.SerialId = serialId;
                request.SoundAssetName = soundAssetName;
                request.SoundGroup = soundGroup;
                request.ResourcePriority = resourcePriority;
                request.PlaySoundParams = playSoundParams;
                request.UserData = userData;
                request.RequestedRealTime = requestedRealTime;
                request.WaitedForLoad = false;
                request.State = SoundPlayRequestState.Created;
                return request;
            }

            public void Clear()
            {
                SerialId = 0;
                SoundAssetName = null;
                SoundGroup = null;
                ResourcePriority = 0;
                PlaySoundParams = null;
                UserData = null;
                RequestedRealTime = 0d;
                WaitedForLoad = false;
                State = SoundPlayRequestState.Created;
            }
        }
    }
}
