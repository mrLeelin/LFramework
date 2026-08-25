using System.Collections.Generic;
using GameFramework;
using GameFramework.Sound;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Sound
{
    public sealed class SoundManagerSchedulingTests
    {
        private ISoundManager _manager;
        private FakeResourceManager _resourceManager;
        private FakeSoundHelper _soundHelper;
        private List<FakeSoundAgentHelper> _agentHelpers;

        [SetUp]
        public void SetUp()
        {
            GameFrameworkEntry.Shutdown();
            _manager = GameFrameworkEntry.GetModule<ISoundManager>();
            _resourceManager = new FakeResourceManager();
            _soundHelper = new FakeSoundHelper();
            _agentHelpers = new List<FakeSoundAgentHelper>();
            _manager.SetResourceManager(_resourceManager);
            _manager.SetSoundHelper(_soundHelper);
        }

        [TearDown]
        public void TearDown()
        {
            GameFrameworkEntry.Shutdown();
        }

        [Test]
        public void LegacyAddSoundGroup_UsesReplacementPolicy()
        {
            Assert.That(_manager.AddSoundGroup("Legacy", new FakeSoundGroupHelper()), Is.True);

            Assert.That(_manager.GetSoundGroup("Legacy").OverflowPolicy,
                Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
        }

        [Test]
        public void ExplicitAddSoundGroup_StoresQueuePolicy()
        {
            Assert.That(_manager.AddSoundGroup(
                "Reliable", false, false, 1f,
                SoundOverflowPolicy.QueueNonLooping,
                new FakeSoundGroupHelper()), Is.True);

            Assert.That(_manager.GetSoundGroup("Reliable").OverflowPolicy,
                Is.EqualTo(SoundOverflowPolicy.QueueNonLooping));
        }

        [Test]
        public void QueueGroup_RejectsLoopBeforeLoading()
        {
            AddGroup("Reliable", SoundOverflowPolicy.QueueNonLooping, 1);
            PlaySoundErrorCode? error = null;
            _manager.PlaySoundFailure += (_, args) => error = args.ErrorCode;
            PlaySoundParams parameters = PlaySoundParams.Create();
            parameters.Loop = true;

            _manager.PlaySound("ui/invalid-loop", "Reliable", parameters);

            Assert.That(error, Is.EqualTo(PlaySoundErrorCode.LoopingSoundNotSupportedByQueue));
            Assert.That(_resourceManager.Loads, Is.Empty);
        }

        private void AddGroup(string name, SoundOverflowPolicy policy, int agentCount)
        {
            Assert.That(_manager.AddSoundGroup(
                name, false, false, 1f, policy, new FakeSoundGroupHelper()), Is.True);

            for (int i = 0; i < agentCount; i++)
            {
                var helper = new FakeSoundAgentHelper();
                _agentHelpers.Add(helper);
                _manager.AddSoundAgentHelper(name, helper);
            }
        }
    }
}
