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
        private sealed class SoundAssetEntry
        {
            public readonly string SoundAssetName;
            public readonly List<SoundPlayRequest> LoadingRequests = new List<SoundPlayRequest>();
            public object Asset;
            public float LoadDuration;
            public int AgentBorrowCount;
            public double IdleSince;
            public bool IsLoading;
            public bool ReleaseOnLoad;
            public bool ReleaseWhenIdle;
            public bool Released;

            public SoundAssetEntry(string soundAssetName)
            {
                SoundAssetName = soundAssetName;
                Asset = null;
                LoadDuration = 0f;
                AgentBorrowCount = 0;
                IdleSince = 0d;
                IsLoading = true;
                ReleaseOnLoad = false;
                ReleaseWhenIdle = false;
                Released = false;
            }
        }
    }
}
