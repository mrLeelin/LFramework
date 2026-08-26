using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Sound
{
    public sealed class DefaultSoundAgentHelperTests
    {
        private static readonly MethodInfo UpdateMethod = typeof(DefaultSoundAgentHelper).GetMethod(
            "Update", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void Update_DoesNotResetPausedAudioSource()
        {
            GameObject gameObject = new GameObject("Paused Sound Agent");
            AudioClip clip = AudioClip.Create("pause-test", 44100, 1, 44100, false);
            try
            {
                DefaultSoundAgentHelper helper = gameObject.AddComponent<DefaultSoundAgentHelper>();
                int resetCount = 0;
                helper.ResetSoundAgent += (_, __) => resetCount++;

                Assert.That(helper.SetSoundAsset(clip), Is.True);
                helper.Play(0f);
                helper.Pause(0f);
                InvokeUpdate(helper);
                InvokeUpdate(helper);

                Assert.That(resetCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Resume_ClearsPausedCompletionGuard()
        {
            GameObject gameObject = new GameObject("Resumed Sound Agent");
            AudioClip clip = AudioClip.Create("resume-test", 44100, 1, 44100, false);
            try
            {
                DefaultSoundAgentHelper helper = gameObject.AddComponent<DefaultSoundAgentHelper>();
                int resetCount = 0;
                helper.ResetSoundAgent += (_, __) => resetCount++;
                helper.SetSoundAsset(clip);
                helper.Play(0f);
                helper.Pause(0f);

                helper.Resume(0f);
                gameObject.GetComponent<AudioSource>().Stop();
                InvokeUpdate(helper);

                Assert.That(resetCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Reset_ClearsPausedCompletionGuardAndClip()
        {
            GameObject gameObject = new GameObject("Reset Sound Agent");
            AudioClip clip = AudioClip.Create("reset-test", 44100, 1, 44100, false);
            try
            {
                DefaultSoundAgentHelper helper = gameObject.AddComponent<DefaultSoundAgentHelper>();
                int resetCount = 0;
                helper.ResetSoundAgent += (_, __) => resetCount++;
                helper.SetSoundAsset(clip);
                helper.Play(0f);
                helper.Pause(0f);

                helper.Reset();
                Assert.That(gameObject.GetComponent<AudioSource>().clip, Is.Null);
                helper.SetSoundAsset(clip);
                InvokeUpdate(helper);

                Assert.That(resetCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Time_SetterMovesAndClampsPlaybackPosition()
        {
            GameObject gameObject = new GameObject("Timed Sound Agent");
            AudioClip clip = AudioClip.Create("time-test", 44100, 1, 44100, false);
            try
            {
                DefaultSoundAgentHelper helper = gameObject.AddComponent<DefaultSoundAgentHelper>();
                helper.SetSoundAsset(clip);

                helper.Time = 0.25f;
                Assert.That(helper.Time, Is.EqualTo(0.25f).Within(0.02f));
                helper.Time = 2f;
                Assert.That(helper.Time, Is.EqualTo(1f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void InvokeUpdate(DefaultSoundAgentHelper helper)
        {
            Assert.That(UpdateMethod, Is.Not.Null);
            UpdateMethod.Invoke(helper, null);
        }
    }
}
