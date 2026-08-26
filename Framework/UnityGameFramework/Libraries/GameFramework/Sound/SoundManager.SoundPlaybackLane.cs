//------------------------------------------------------------
// Game Framework
// Copyright (C) 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework.Sound
{
    internal sealed partial class SoundManager : GameFrameworkModule, ISoundManager
    {
        private readonly struct SoundPlaybackLaneKey : IEquatable<SoundPlaybackLaneKey>
        {
            public readonly SoundGroup SoundGroup;
            public readonly string SoundAssetName;

            public SoundPlaybackLaneKey(SoundGroup soundGroup, string soundAssetName)
            {
                SoundGroup = soundGroup;
                SoundAssetName = soundAssetName;
            }

            public bool Equals(SoundPlaybackLaneKey other)
            {
                return ReferenceEquals(SoundGroup, other.SoundGroup) &&
                       string.Equals(SoundAssetName, other.SoundAssetName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is SoundPlaybackLaneKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (SoundGroup.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(SoundAssetName);
                }
            }
        }

        private sealed class SoundPlaybackLane
        {
            public readonly SoundPlaybackLaneKey Key;
            public readonly SoundGroup SoundGroup;
            public readonly SoundAssetEntry AssetEntry;
            public readonly LinkedList<SoundPlayRequest> Requests = new LinkedList<SoundPlayRequest>();
            public double ScheduleOffset;
            public bool HasScheduleOffset;

            public SoundPlaybackLane(SoundPlaybackLaneKey key, SoundGroup soundGroup, SoundAssetEntry assetEntry)
            {
                Key = key;
                SoundGroup = soundGroup;
                AssetEntry = assetEntry;
                ScheduleOffset = 0d;
                HasScheduleOffset = false;
            }

            public double GetDueTime(SoundPlayRequest request, double now)
            {
                if (!HasScheduleOffset)
                {
                    ScheduleOffset = request.WaitedForLoad ? now - request.RequestedRealTime : 0d;
                    HasScheduleOffset = true;
                }

                return request.RequestedRealTime + ScheduleOffset;
            }

            public void ShiftAfterLateStart(double dueTime, double actualStart)
            {
                if (actualStart > dueTime)
                {
                    ScheduleOffset += actualStart - dueTime;
                }
            }
        }
    }
}
