//------------------------------------------------------------
// Game Framework
// Copyright (C) 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Sound
{
    /// <summary>
    /// Play sound canceled event.
    /// </summary>
    public sealed class PlaySoundCanceledEventArgs : GameFrameworkEventArgs
    {
        public PlaySoundCanceledEventArgs()
        {
            SerialId = 0;
            SoundAssetName = null;
            SoundGroupName = null;
            UserData = null;
        }

        /// <summary>
        /// Gets the sound serial id.
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// Gets the sound asset name.
        /// </summary>
        public string SoundAssetName { get; private set; }

        /// <summary>
        /// Gets the sound group name.
        /// </summary>
        public string SoundGroupName { get; private set; }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// Creates a play sound canceled event.
        /// </summary>
        public static PlaySoundCanceledEventArgs Create(int serialId, string soundAssetName, string soundGroupName, object userData)
        {
            PlaySoundCanceledEventArgs eventArgs = ReferencePool.Acquire<PlaySoundCanceledEventArgs>();
            eventArgs.SerialId = serialId;
            eventArgs.SoundAssetName = soundAssetName;
            eventArgs.SoundGroupName = soundGroupName;
            eventArgs.UserData = userData;
            return eventArgs;
        }

        /// <summary>
        /// Clears the event data.
        /// </summary>
        public override void Clear()
        {
            SerialId = 0;
            SoundAssetName = null;
            SoundGroupName = null;
            UserData = null;
        }
    }
}
