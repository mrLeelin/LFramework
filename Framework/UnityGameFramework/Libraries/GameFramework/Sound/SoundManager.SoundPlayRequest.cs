//------------------------------------------------------------
// Game Framework
// Copyright (C) 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

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
            public SoundAssetEntry AssetEntry;
            public int ResourcePriority;
            public PlaySoundParams PlaySoundParams;
            public object UserData;
            public double RequestedRealTime;
            public bool WaitedForLoad;
            public bool CountsTowardQueue;
            public SoundPlayRequestState State;
            public LinkedListNode<SoundPlayRequest> LaneNode;

            public static SoundPlayRequest Create(int serialId, string soundAssetName, SoundGroup soundGroup, int resourcePriority, PlaySoundParams playSoundParams, object userData, double requestedRealTime)
            {
                SoundPlayRequest request = ReferencePool.Acquire<SoundPlayRequest>();
                request.SerialId = serialId;
                request.SoundAssetName = soundAssetName;
                request.SoundGroup = soundGroup;
                request.AssetEntry = null;
                request.ResourcePriority = resourcePriority;
                request.PlaySoundParams = playSoundParams;
                request.UserData = userData;
                request.RequestedRealTime = requestedRealTime;
                request.WaitedForLoad = false;
                request.CountsTowardQueue = false;
                request.State = SoundPlayRequestState.Created;
                request.LaneNode = null;
                return request;
            }

            public void Clear()
            {
                SerialId = 0;
                SoundAssetName = null;
                SoundGroup = null;
                AssetEntry = null;
                ResourcePriority = 0;
                PlaySoundParams = null;
                UserData = null;
                RequestedRealTime = 0d;
                WaitedForLoad = false;
                CountsTowardQueue = false;
                State = SoundPlayRequestState.Created;
                LaneNode = null;
            }
        }
    }
}
