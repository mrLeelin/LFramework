using System;
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
        private FakeLogHelper _logHelper;
        private List<FakeSoundAgentHelper> _agentHelpers;
        private double _clock;

        [SetUp]
        public void SetUp()
        {
            GameFrameworkEntry.Shutdown();
            _manager = GameFrameworkEntry.GetModule<ISoundManager>();
            _resourceManager = new FakeResourceManager();
            _soundHelper = new FakeSoundHelper();
            _logHelper = new FakeLogHelper();
            _agentHelpers = new List<FakeSoundAgentHelper>();
            _clock = 0d;
            _manager.SetResourceManager(_resourceManager);
            _manager.SetSoundHelper(_soundHelper);
            GameFrameworkLog.SetLogHelper(_logHelper);
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
            AssertNoUsingReferences("PlaySoundParams");
        }

        [Test]
        public void ValidationFailureWithoutListener_ReleasesParametersBeforeThrow()
        {
            AddGroup("Reliable", SoundOverflowPolicy.QueueNonLooping, 1);
            PlaySoundParams parameters = PlaySoundParams.Create();
            parameters.Loop = true;

            Assert.Throws<GameFrameworkException>(() =>
                _manager.PlaySound("ui/invalid-loop", "Reliable", parameters));

            AssertNoUsingReferences("PlaySoundParams");
        }

        [Test]
        public void TenColdRequests_UseOneUntypedLoad_AndStartTenAgents()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 16);
            var succeededIds = new List<int>();
            _manager.PlaySoundSuccess += (_, args) => succeededIds.Add(args.SerialId);

            var serialIds = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                serialIds.Add(_manager.PlaySound("ui/tick", "UISound"));
            }

            Assert.That(serialIds, Is.Unique);
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(_resourceManager.Loads[0].AssetType, Is.Null);
            Assert.That(succeededIds, Is.Empty);

            object soundAsset = new object();
            _resourceManager.CompleteSuccess(0, soundAsset);

            Assert.That(succeededIds, Is.EquivalentTo(serialIds));
            int totalPlayCount = 0;
            foreach (FakeSoundAgentHelper helper in _agentHelpers)
            {
                totalPlayCount += helper.PlayCount;
            }

            Assert.That(totalPlayCount, Is.EqualTo(10));
            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
            Assert.That(_resourceManager.UnloadedAssets, Is.Empty);
        }

        [Test]
        public void HotCacheRequest_DoesNotReload_AndReportsZeroLoadDuration()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var durations = new List<float>();
            _manager.PlaySoundSuccess += (_, args) => durations.Add(args.Duration);

            _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, new object(), 0.25f);
            _manager.PlaySound("ui/tick", "UISound");

            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(durations, Is.EqualTo(new[] { 0.25f, 0f }));
        }

        [Test]
        public void SuccessCallback_CanStopStartedSound_WithoutDoubleReleasingRequest()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            bool stopped = false;
            _manager.PlaySoundSuccess += (_, args) => stopped = _manager.StopSound(args.SerialId);

            _manager.PlaySound("ui/tick", "UISound");
            Assert.DoesNotThrow(() => _resourceManager.CompleteSuccess(0, new object()));

            Assert.That(stopped, Is.True);
            Assert.That(_agentHelpers[0].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void FailureCallback_CanQueryStop_WithoutDoubleReleasingRequest()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            bool stopped = true;
            _manager.PlaySoundFailure += (_, args) => stopped = _manager.StopSound(args.SerialId);

            _manager.PlaySound("ui/missing", "UISound");
            Assert.DoesNotThrow(() => _resourceManager.CompleteFailure(0, "missing"));

            Assert.That(stopped, Is.False);
        }

        [Test]
        public void Shutdown_ResetsAgentBeforeReleasingAsset()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            _agentHelpers[0].AutoCompleteOnStop = false;
            object soundAsset = new object();
            _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, soundAsset);

            GameFrameworkEntry.Shutdown();

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { soundAsset }));
            Assert.That(_agentHelpers[0].SoundAsset, Is.Null);
        }

        [Test]
        public void Replacement_UsesActualStartOrder_NotRequestOrder()
        {
            AddGroup("Sound", SoundOverflowPolicy.ReplaceByPriority, 2);
            object firstRequestedAsset = new object();
            object secondRequestedAsset = new object();
            object replacementAsset = new object();

            _manager.PlaySound("sound/first", "Sound");
            GameFrameworkEntry.Update(0f, 1f);
            _manager.PlaySound("sound/second", "Sound");
            _resourceManager.CompleteSuccess(1, secondRequestedAsset);
            GameFrameworkEntry.Update(0f, 1f);
            _resourceManager.CompleteSuccess(0, firstRequestedAsset);
            _manager.PlaySound("sound/replacement", "Sound");
            _resourceManager.CompleteSuccess(2, replacementAsset);

            Assert.That(_agentHelpers[0].SoundAsset, Is.SameAs(replacementAsset));
            Assert.That(_agentHelpers[1].SoundAsset, Is.SameAs(firstRequestedAsset));
        }

        [Test]
        public void CancelledLastWaiter_LateSuccessReleasesAndAllowsRetry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object abandonedAsset = new object();
            int serialId = _manager.PlaySound("ui/tick", "UISound");

            Assert.That(_manager.StopSound(serialId), Is.True);
            _resourceManager.CompleteSuccess(0, abandonedAsset);
            _manager.PlaySound("ui/tick", "UISound");

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { abandonedAsset }));
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
        }

        [Test]
        public void CancelledLastWaiter_NewRequestBeforeCallbackJoinsExistingLoad()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var startedIds = new List<int>();
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int cancelledId = _manager.PlaySound("ui/tick", "UISound");
            Assert.That(_manager.StopSound(cancelledId), Is.True);

            int survivingId = _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(startedIds, Is.EqualTo(new[] { survivingId }));
            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
        }

        [Test]
        public void ProgressAndDependencyCallbacks_TolerateCancellationReentry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            int first = _manager.PlaySound("ui/tick", "UISound");
            int second = _manager.PlaySound("ui/tick", "UISound");
            _manager.PlaySoundUpdate += (_, args) => _manager.StopSound(args.SerialId);
            _manager.PlaySoundDependencyAsset += (_, args) => _manager.StopSound(args.SerialId);

            Assert.DoesNotThrow(() => _resourceManager.ReportProgress(0, 0.5f));
            Assert.That(_manager.IsLoadingSound(first), Is.False);
            Assert.That(_manager.IsLoadingSound(second), Is.False);

            int dependencyFirst = _manager.PlaySound("ui/dependency-owner", "UISound");
            int dependencySecond = _manager.PlaySound("ui/dependency-owner", "UISound");

            Assert.DoesNotThrow(() => _resourceManager.ReportDependency(1, "ui/dependency"));
            Assert.That(_manager.IsLoadingSound(dependencyFirst), Is.False);
            Assert.That(_manager.IsLoadingSound(dependencySecond), Is.False);
        }

        [Test]
        public void ProgressAndDependencyParameters_RemainStableAfterCancellationReentry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            PlaySoundParams progressParams = PlaySoundParams.Create();
            progressParams.VolumeInSoundGroup = 0.25f;
            int progressId = _manager.PlaySound("ui/progress", "UISound", progressParams);
            float progressVolume = -1f;
            _manager.PlaySoundUpdate += (_, args) => _manager.StopSound(args.SerialId);
            _manager.PlaySoundUpdate += (_, args) => progressVolume = args.PlaySoundParams.VolumeInSoundGroup;

            _resourceManager.ReportProgress(0, 0.5f);

            PlaySoundParams dependencyParams = PlaySoundParams.Create();
            dependencyParams.Pitch = 1.5f;
            int dependencyId = _manager.PlaySound("ui/dependency", "UISound", dependencyParams);
            float dependencyPitch = -1f;
            _manager.PlaySoundDependencyAsset += (_, args) => _manager.StopSound(args.SerialId);
            _manager.PlaySoundDependencyAsset += (_, args) => dependencyPitch = args.PlaySoundParams.Pitch;
            _resourceManager.ReportDependency(1, "ui/shared-dependency");

            Assert.That(progressVolume, Is.EqualTo(0.25f));
            Assert.That(dependencyPitch, Is.EqualTo(1.5f));
            Assert.That(_manager.IsLoadingSound(progressId), Is.False);
            Assert.That(_manager.IsLoadingSound(dependencyId), Is.False);
            AssertNoUsingReferences("PlaySoundParams", "PlaySoundUpdateEventArgs", "PlaySoundDependencyAssetEventArgs");
        }

        [Test]
        public void SharedFailureWithoutListener_CleansEveryWaiterThenThrowsOnce()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            _manager.PlaySound("ui/missing", "UISound");
            _manager.PlaySound("ui/missing", "UISound");

            Assert.Throws<GameFrameworkException>(() =>
                _resourceManager.CompleteFailure(0, "missing"));

            _manager.PlaySound("ui/missing", "UISound");
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
        }

        [Test]
        public void SharedSuccessCallback_DoesNotStartRequestReusedDuringReentry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 3);
            var startedIds = new List<int>();
            int replacementId = 0;
            int firstId = _manager.PlaySound("ui/tick", "UISound");
            int secondId = _manager.PlaySound("ui/tick", "UISound");
            _manager.PlaySoundSuccess += (_, args) =>
            {
                startedIds.Add(args.SerialId);
                if (args.SerialId == firstId)
                {
                    Assert.That(_manager.StopSound(secondId), Is.True);
                    replacementId = _manager.PlaySound("ui/replacement", "UISound");
                }
            };

            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(startedIds, Is.EqualTo(new[] { firstId }));
            Assert.That(_manager.IsLoadingSound(replacementId), Is.True);
            _resourceManager.CompleteSuccess(1, new object());
            Assert.That(startedIds, Is.EqualTo(new[] { firstId, replacementId }));
        }

        [Test]
        public void ProgressAndDependencyCallbacks_DoNotVisitRequestsReusedDuringReentry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 4);
            var updatedIds = new List<int>();
            int progressFirst = _manager.PlaySound("ui/progress", "UISound");
            int progressSecond = _manager.PlaySound("ui/progress", "UISound");
            int progressReplacement = 0;
            _manager.PlaySoundUpdate += (_, args) =>
            {
                updatedIds.Add(args.SerialId);
                if (args.SerialId == progressFirst)
                {
                    Assert.That(_manager.StopSound(progressSecond), Is.True);
                    progressReplacement = _manager.PlaySound("ui/progress-replacement", "UISound");
                }
            };

            _resourceManager.ReportProgress(0, 0.5f);

            Assert.That(updatedIds, Is.EqualTo(new[] { progressFirst }));
            Assert.That(_manager.IsLoadingSound(progressReplacement), Is.True);

            var dependencyIds = new List<int>();
            int dependencyFirst = _manager.PlaySound("ui/dependency-owner", "UISound");
            int dependencySecond = _manager.PlaySound("ui/dependency-owner", "UISound");
            int dependencyReplacement = 0;
            _manager.PlaySoundDependencyAsset += (_, args) =>
            {
                dependencyIds.Add(args.SerialId);
                if (args.SerialId == dependencyFirst)
                {
                    Assert.That(_manager.StopSound(dependencySecond), Is.True);
                    dependencyReplacement = _manager.PlaySound("ui/dependency-replacement", "UISound");
                }
            };

            _resourceManager.ReportDependency(2, "ui/dependency");

            Assert.That(dependencyIds, Is.EqualTo(new[] { dependencyFirst }));
            Assert.That(_manager.IsLoadingSound(dependencyReplacement), Is.True);
        }

        [Test]
        public void SharedFailureCallback_DoesNotFailRequestReusedDuringReentry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var failedIds = new List<int>();
            int replacementId = 0;
            int firstId = _manager.PlaySound("ui/missing", "UISound");
            int secondId = _manager.PlaySound("ui/missing", "UISound");
            _manager.PlaySoundFailure += (_, args) =>
            {
                failedIds.Add(args.SerialId);
                if (args.SerialId == firstId)
                {
                    Assert.That(_manager.StopSound(secondId), Is.True);
                    replacementId = _manager.PlaySound("ui/retry", "UISound");
                }
            };

            _resourceManager.CompleteFailure(0, "missing");

            Assert.That(failedIds, Is.EqualTo(new[] { firstId }));
            Assert.That(_manager.IsLoadingSound(replacementId), Is.True);
        }

        [Test]
        public void SharedSuccessListenerException_DoesNotStrandRemainingRequests()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var startedIds = new List<int>();
            var laterSubscriberIds = new List<int>();
            int firstId = _manager.PlaySound("ui/tick", "UISound");
            int secondId = _manager.PlaySound("ui/tick", "UISound");
            _manager.PlaySoundSuccess += (_, args) =>
            {
                startedIds.Add(args.SerialId);
                if (args.SerialId == firstId)
                {
                    throw new InvalidOperationException("listener failure");
                }
            };
            _manager.PlaySoundSuccess += (_, args) => laterSubscriberIds.Add(args.SerialId);

            Assert.Throws<InvalidOperationException>(() =>
                _resourceManager.CompleteSuccess(0, new object()));

            Assert.That(startedIds, Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(laterSubscriberIds, Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(_manager.IsLoadingSound(firstId), Is.False);
            Assert.That(_manager.IsLoadingSound(secondId), Is.False);
        }

        [Test]
        public void SharedFailureListenerException_DoesNotStrandRemainingRequests()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var failedIds = new List<int>();
            var laterSubscriberIds = new List<int>();
            int firstId = _manager.PlaySound("ui/missing", "UISound");
            int secondId = _manager.PlaySound("ui/missing", "UISound");
            _manager.PlaySoundFailure += (_, args) =>
            {
                failedIds.Add(args.SerialId);
                if (args.SerialId == firstId)
                {
                    throw new InvalidOperationException("listener failure");
                }
            };
            _manager.PlaySoundFailure += (_, args) => laterSubscriberIds.Add(args.SerialId);

            Assert.Throws<InvalidOperationException>(() =>
                _resourceManager.CompleteFailure(0, "missing"));

            Assert.That(failedIds, Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(laterSubscriberIds, Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(_manager.IsLoadingSound(firstId), Is.False);
            Assert.That(_manager.IsLoadingSound(secondId), Is.False);
        }

        [Test]
        public void Shutdown_ReleasesAssetOnlyAfterAgentHelperDropsReference()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            _agentHelpers[0].AutoCompleteOnStop = false;
            bool helperWasClearAtRelease = false;
            _soundHelper.ReleaseObserver = _ =>
                helperWasClearAtRelease = _agentHelpers[0].SoundAsset == null;
            _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, new object());

            GameFrameworkEntry.Shutdown();

            Assert.That(helperWasClearAtRelease, Is.True);
        }

        [Test]
        public void SetSoundAssetFailure_InvalidatesCacheAndAllowsRetry()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var failedIds = new List<int>();
            var startedIds = new List<int>();
            _manager.PlaySoundFailure += (_, args) => failedIds.Add(args.SerialId);
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            _agentHelpers[0].RejectSoundAsset = true;
            object rejectedAsset = new object();
            int failedId = _manager.PlaySound("ui/tick", "UISound");

            _resourceManager.CompleteSuccess(0, rejectedAsset);

            Assert.That(failedIds, Is.EqualTo(new[] { failedId }));
            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { rejectedAsset }));
            Assert.That(_agentHelpers[0].SoundAsset, Is.Null);

            _agentHelpers[0].RejectSoundAsset = false;
            int retryId = _manager.PlaySound("ui/tick", "UISound");
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
            _resourceManager.CompleteSuccess(1, new object());
            Assert.That(startedIds, Is.EqualTo(new[] { retryId }));
        }

        [Test]
        public void ColdRequests_PreserveOriginalIntervalsAfterLoad()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 3);
            var starts = new List<double>();
            _manager.PlaySoundSuccess += (_, __) => starts.Add(_clock);

            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.21f);
            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.21f);
            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.08f);
            _resourceManager.CompleteSuccess(0, new object());

            Advance(0f);
            Advance(0.21f);
            Advance(0.21f);

            Assert.That(starts, Has.Count.EqualTo(3));
            Assert.That(starts[1] - starts[0], Is.EqualTo(0.21d).Within(0.001d));
            Assert.That(starts[2] - starts[1], Is.EqualTo(0.21d).Within(0.001d));
        }

        [Test]
        public void BusyQueueAgent_DelaysWithoutDroppingOrBurstingLaterRequests()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var startedIds = new List<int>();
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int[] ids =
            {
                _manager.PlaySound("ui/tick", "UISound"),
                _manager.PlaySound("ui/tick", "UISound"),
                _manager.PlaySound("ui/tick", "UISound")
            };
            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(startedIds, Is.EqualTo(new[] { ids[0] }));
            _agentHelpers[0].Complete();
            Advance(0f);
            Assert.That(startedIds, Is.EqualTo(new[] { ids[0], ids[1] }));
            _agentHelpers[0].Complete();
            Advance(0f);
            Assert.That(startedIds, Is.EqualTo(ids));
        }

        [Test]
        public void LateAgentAvailability_ShiftsRemainingCadenceWithoutCatchUpBurst()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var startTimes = new List<double>();
            _manager.PlaySoundSuccess += (_, __) => startTimes.Add(_clock);

            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.21f);
            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.21f);
            _manager.PlaySound("ui/tick", "UISound");
            Advance(0.08f);
            _resourceManager.CompleteSuccess(0, new object());

            Advance(0.4f);
            _agentHelpers[0].Complete();
            Advance(0f);
            _agentHelpers[0].Complete();
            Advance(0.2f);

            Assert.That(startTimes, Has.Count.EqualTo(2));
            Advance(0.01f);
            Assert.That(startTimes, Has.Count.EqualTo(3));
            Assert.That(startTimes[2] - startTimes[1], Is.EqualTo(0.21d).Within(0.001d));
        }

        [Test]
        public void PausedQueueAgent_RemainsOccupiedUntilItsSoundCompletes()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var startedIds = new List<int>();
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int pausedId = _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, new object());
            _manager.PauseSound(pausedId);

            int queuedId = _manager.PlaySound("ui/tick", "UISound");

            Assert.That(startedIds, Is.EqualTo(new[] { pausedId }));
            _agentHelpers[0].Complete();
            Advance(0f);
            Assert.That(startedIds, Is.EqualTo(new[] { pausedId, queuedId }));
        }

        [Test]
        public void TenSameFrameQueueRequests_StartInOneManagerTickWhenAgentsAvailable()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 16);
            var startTimes = new List<double>();
            _manager.PlaySoundSuccess += (_, __) => startTimes.Add(_clock);
            for (int i = 0; i < 10; i++)
            {
                _manager.PlaySound("ui/tick", "UISound");
            }

            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(startTimes, Has.Count.EqualTo(10));
            Assert.That(startTimes, Is.All.EqualTo(0d));
        }

        [Test]
        public void ReplacementGroup_KeepsSamePriorityReplacementBehavior()
        {
            AddGroup("Music", SoundOverflowPolicy.ReplaceByPriority, 1);
            var failedIds = new List<int>();
            _manager.PlaySoundFailure += (_, args) => failedIds.Add(args.SerialId);
            _manager.PlaySound("music/a", "Music");
            _resourceManager.CompleteSuccess(0, new object());
            _manager.PlaySound("music/b", "Music");
            _resourceManager.CompleteSuccess(1, new object());

            Assert.That(_agentHelpers[0].PlayCount, Is.EqualTo(2));
            Assert.That(failedIds, Is.Empty);
        }

        [Test]
        public void CancellingOneSharedWaiter_DoesNotCancelTheOther()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var cancelledIds = new List<int>();
            var startedIds = new List<int>();
            _manager.PlaySoundCanceled += (_, args) => cancelledIds.Add(args.SerialId);
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int cancelledId = _manager.PlaySound("ui/tick", "UISound");
            int survivingId = _manager.PlaySound("ui/tick", "UISound");

            Assert.That(_manager.StopSound(cancelledId), Is.True);
            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(cancelledIds, Is.EqualTo(new[] { cancelledId }));
            Assert.That(startedIds, Is.EqualTo(new[] { survivingId }));
            AssertNoUsingReferences("SoundPlayRequest", "PlaySoundParams");
        }

        [Test]
        public void CancellationListenerException_DoesNotBlockCleanupOrOtherListeners()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            bool secondListenerCalled = false;
            _manager.PlaySoundCanceled += (_, __) => throw new InvalidOperationException("listener");
            _manager.PlaySoundCanceled += (_, __) => secondListenerCalled = true;
            int serialId = _manager.PlaySound("ui/tick", "UISound");

            Assert.DoesNotThrow(() => _manager.StopSound(serialId));

            Assert.That(secondListenerCalled, Is.True);
            AssertNoUsingReferences("SoundPlayRequest", "PlaySoundParams", "PlaySoundCanceledEventArgs");
        }

        [Test]
        public void SharedLoadFailure_FailsEveryWaiterOnce_ThenRetries()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var failedIds = new List<int>();
            var primaryFailureFlags = new List<bool>();
            _manager.PlaySoundFailure += (_, args) =>
            {
                failedIds.Add(args.SerialId);
                primaryFailureFlags.Add(args.IsPrimaryFailure);
            };
            int first = _manager.PlaySound("ui/missing", "UISound");
            int second = _manager.PlaySound("ui/missing", "UISound");

            _resourceManager.CompleteFailure(0, "missing");

            Assert.That(failedIds, Is.EquivalentTo(new[] { first, second }));
            Assert.That(primaryFailureFlags.FindAll(value => value), Has.Count.EqualTo(1));
            _manager.PlaySound("ui/missing", "UISound");
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
        }

        [Test]
        public void StopAllLoadingSounds_CancelsOnlyWaitingAssetRequests()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var cancelledIds = new List<int>();
            _manager.PlaySoundCanceled += (_, args) => cancelledIds.Add(args.SerialId);
            int first = _manager.PlaySound("ui/first", "UISound");
            int second = _manager.PlaySound("ui/second", "UISound");

            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Is.EqualTo(new[] { first, second }));
            _manager.StopAllLoadingSounds();

            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Is.Empty);
            Assert.That(cancelledIds, Is.EqualTo(new[] { first, second }));
            AssertNoUsingReferences("SoundPlayRequest", "PlaySoundParams");
        }

        [Test]
        public void StopAllLoadedSounds_CancelsWaitingAgentAndStopsPlayingAgent()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var cancelledIds = new List<int>();
            var startedIds = new List<int>();
            _manager.PlaySoundCanceled += (_, args) => cancelledIds.Add(args.SerialId);
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int playingId = _manager.PlaySound("ui/tick", "UISound");
            int queuedId = _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, new object());

            _manager.StopAllLoadedSounds();

            Assert.That(startedIds, Is.EqualTo(new[] { playingId }));
            Assert.That(cancelledIds, Is.EqualTo(new[] { queuedId }));
            Assert.That(_agentHelpers[0].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void AllWaitersCancelled_LateSuccessReleasesOnceWithoutStarting()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var cancelledIds = new List<int>();
            var startedIds = new List<int>();
            _manager.PlaySoundCanceled += (_, args) => cancelledIds.Add(args.SerialId);
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            int first = _manager.PlaySound("ui/tick", "UISound");
            int second = _manager.PlaySound("ui/tick", "UISound");
            object lateAsset = new object();

            _manager.StopAllLoadingSounds();
            _resourceManager.CompleteSuccess(0, lateAsset);

            Assert.That(cancelledIds, Is.EqualTo(new[] { first, second }));
            Assert.That(startedIds, Is.Empty);
            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { lateAsset }));
        }

        [Test]
        public void LoadProgressAndDependency_FanOutToEveryLiveWaitingAssetRequest()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            var updateIds = new List<int>();
            var dependencyIds = new List<int>();
            _manager.PlaySoundUpdate += (_, args) => updateIds.Add(args.SerialId);
            _manager.PlaySoundDependencyAsset += (_, args) => dependencyIds.Add(args.SerialId);
            int first = _manager.PlaySound("ui/tick", "UISound");
            int second = _manager.PlaySound("ui/tick", "UISound");

            _resourceManager.ReportProgress(0, 0.5f);
            _resourceManager.ReportDependency(0, "ui/dependency");

            Assert.That(updateIds, Is.EqualTo(new[] { first, second }));
            Assert.That(dependencyIds, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void SharedAsset_ReleasesOnceAfterLastAgentAndIdleTimeout()
        {
            _manager.SetSoundCacheSettings(2f, 64, 64, 32);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
            _manager.PlaySound("ui/tick", "UISound");
            _manager.PlaySound("ui/tick", "UISound");
            object asset = new object();
            _resourceManager.CompleteSuccess(0, asset);

            _agentHelpers[0].Complete();
            _agentHelpers[1].Complete();
            Advance(1.99f);
            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
            Advance(0.01f);

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { asset }));
        }

        [Test]
        public void RequestBeforeIdleTimeout_ReusesReadyAssetAndCancelsEviction()
        {
            _manager.SetSoundCacheSettings(2f, 64, 64, 32);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object asset = new object();
            _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, asset);
            _agentHelpers[0].Complete();
            Advance(1.5f);

            _manager.PlaySound("ui/tick", "UISound");
            Advance(1f);

            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(_agentHelpers[0].PlayCount, Is.EqualTo(2));
            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
        }

        [Test]
        public void MaxIdleEntryCount_EvictsOnlyOldestIdleEntry()
        {
            _manager.SetSoundCacheSettings(30f, 1, 64, 32);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object firstAsset = new object();
            object secondAsset = new object();

            _manager.PlaySound("ui/first", "UISound");
            _resourceManager.CompleteSuccess(0, firstAsset);
            _agentHelpers[0].Complete();
            Advance(0.5f);
            _manager.PlaySound("ui/second", "UISound");
            _resourceManager.CompleteSuccess(1, secondAsset);
            _agentHelpers[0].Complete();
            Advance(0.01f);

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { firstAsset }));
        }

        [Test]
        public void IdleEviction_RevalidatesEntriesAfterReleaseCallbackReentry()
        {
            _manager.SetSoundCacheSettings(30f, 64, 64, 32);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object firstAsset = new object();
            object secondAsset = new object();

            _manager.PlaySound("ui/first", "UISound");
            _resourceManager.CompleteSuccess(0, firstAsset);
            _agentHelpers[0].Complete();
            Advance(0.5f);
            _manager.PlaySound("ui/second", "UISound");
            _resourceManager.CompleteSuccess(1, secondAsset);
            _agentHelpers[0].Complete();

            _soundHelper.ReleaseObserver = releasedAsset =>
            {
                if (ReferenceEquals(releasedAsset, firstAsset))
                {
                    _manager.PlaySound("ui/second", "UISound");
                }
            };

            _manager.SetSoundCacheSettings(30f, 0, 64, 32);

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { firstAsset }));
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
            Assert.That(_agentHelpers[0].SoundAsset, Is.SameAs(secondAsset));
        }

        [Test]
        public void ActiveAndLoadingEntries_MayExceedIdleMaximumWithoutEviction()
        {
            _manager.SetSoundCacheSettings(0f, 0, 64, 32);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object activeAsset = new object();
            _manager.PlaySound("ui/active", "UISound");
            _resourceManager.CompleteSuccess(0, activeAsset);
            _manager.PlaySound("ui/loading", "UISound");

            Advance(10f);

            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Has.Length.EqualTo(1));
        }

        [Test]
        public void SamePathInTwoGroups_UsesOneLoadAndIndependentAgents()
        {
            AddGroup("UIA", SoundOverflowPolicy.QueueNonLooping, 1);
            AddGroup("UIB", SoundOverflowPolicy.QueueNonLooping, 1);
            _manager.PlaySound("ui/shared", "UIA");
            _manager.PlaySound("ui/shared", "UIB");

            _resourceManager.CompleteSuccess(0, new object());

            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(_agentHelpers[0].PlayCount, Is.EqualTo(1));
            Assert.That(_agentHelpers[1].PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void Shutdown_ResetsEveryActiveAgentAndReleasesReadyEntriesOnce()
        {
            AddGroup("UIA", SoundOverflowPolicy.QueueNonLooping, 1);
            AddGroup("UIB", SoundOverflowPolicy.QueueNonLooping, 1);
            object firstAsset = new object();
            object secondAsset = new object();
            _manager.PlaySound("ui/first", "UIA");
            _resourceManager.CompleteSuccess(0, firstAsset);
            _manager.PlaySound("ui/second", "UIB");
            _resourceManager.CompleteSuccess(1, secondAsset);
            int firstResetCount = _agentHelpers[0].ResetCount;
            int secondResetCount = _agentHelpers[1].ResetCount;

            GameFrameworkEntry.Shutdown();

            Assert.That(_agentHelpers[0].ResetCount, Is.EqualTo(firstResetCount + 1));
            Assert.That(_agentHelpers[1].ResetCount, Is.EqualTo(secondResetCount + 1));
            Assert.That(_soundHelper.ReleasedAssets, Is.EquivalentTo(new[] { firstAsset, secondAsset }));
        }

        [Test]
        public void LateSuccessAfterShutdown_ReleasesReturnedAssetOnce()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object asset = new object();
            _manager.PlaySound("ui/tick", "UISound");

            GameFrameworkEntry.Shutdown();
            _resourceManager.CompleteSuccess(0, asset);

            Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { asset }));
        }

        [Test]
        public void LateFailureAfterShutdown_DoesNotEmitFailureOrLeaveLoadingState()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            int failureCount = 0;
            _manager.PlaySoundFailure += (_, __) => failureCount++;
            _manager.PlaySound("ui/tick", "UISound");

            GameFrameworkEntry.Shutdown();
            _resourceManager.CompleteFailure(0, "late");

            Assert.That(failureCount, Is.Zero);
            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Is.Empty);
        }

        [Test]
        public void ShutdownCancellationListener_CannotCreateAReplacementRequest()
        {
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            bool rejectedDuringShutdown = false;
            _manager.PlaySoundCanceled += (_, __) =>
            {
                PlaySoundParams parameters = PlaySoundParams.Create();
                try
                {
                    _manager.PlaySound("ui/reentrant", "UISound", parameters);
                }
                catch (GameFrameworkException)
                {
                    rejectedDuringShutdown = true;
                }
            };
            _manager.PlaySound("ui/tick", "UISound");

            GameFrameworkEntry.Shutdown();

            Assert.That(rejectedDuringShutdown, Is.True);
            Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Is.Empty);
            AssertNoUsingReferences("PlaySoundParams");
        }

        [Test]
        public void InvalidSoundCacheSettings_DoNotMutateExistingRetention()
        {
            _manager.SetSoundCacheSettings(10f, 64, 64, 32);
            Assert.Throws<GameFrameworkException>(() => _manager.SetSoundCacheSettings(-1f, 0, 1, 0));
            Assert.Throws<GameFrameworkException>(() => _manager.SetSoundCacheSettings(0f, -1, 1, 0));
            Assert.Throws<GameFrameworkException>(() => _manager.SetSoundCacheSettings(0f, 0, 0, 0));
            Assert.Throws<GameFrameworkException>(() => _manager.SetSoundCacheSettings(0f, 0, 2, 2));
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            object asset = new object();
            _manager.PlaySound("ui/tick", "UISound");
            _resourceManager.CompleteSuccess(0, asset);
            _agentHelpers[0].Complete();

            Advance(2f);

            Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
        }

        [Test]
        public void QueueWarning_UsesHysteresisWithoutDroppingRequests()
        {
            _manager.SetSoundCacheSettings(30f, 64, 3, 1);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            var startedIds = new List<int>();
            _manager.PlaySoundSuccess += (_, args) => startedIds.Add(args.SerialId);
            var serialIds = new List<int>();
            serialIds.Add(_manager.PlaySound("ui/tick", "UISound"));
            _resourceManager.CompleteSuccess(0, new object());
            for (int i = 0; i < 4; i++)
            {
                serialIds.Add(_manager.PlaySound("ui/tick", "UISound"));
            }

            Assert.That(_logHelper.Warnings, Has.Count.EqualTo(1));
            for (int i = 0; i < 3; i++)
            {
                _agentHelpers[0].Complete();
                Advance(0f);
            }

            for (int i = 0; i < 3; i++)
            {
                serialIds.Add(_manager.PlaySound("ui/tick", "UISound"));
            }

            Assert.That(_logHelper.Warnings, Has.Count.EqualTo(2));
            while (startedIds.Count < serialIds.Count)
            {
                _agentHelpers[0].Complete();
                Advance(0f);
            }

            Assert.That(startedIds, Is.EquivalentTo(serialIds));
            Assert.That(_logHelper.Warnings[0], Does.Contain("UISound"));
            Assert.That(_logHelper.Warnings[0], Does.Contain("QueueNonLooping"));
        }

        [Test]
        public void QueueWarning_ReentrantShutdownDoesNotContinueRequestSetup()
        {
            _manager.SetSoundCacheSettings(30f, 64, 1, 0);
            AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 1);
            _logHelper.LogObserver = (level, _) =>
            {
                if (level == GameFrameworkLogLevel.Warning)
                {
                    GameFrameworkEntry.Shutdown();
                }
            };

            Assert.DoesNotThrow(() => _manager.PlaySound("ui/tick", "UISound"));

            Assert.That(_resourceManager.Loads, Is.Empty);
            Assert.That(_manager.GetAllLoadingSoundSerialIds(), Is.Empty);
            AssertNoUsingReferences("SoundPlayRequest", nameof(PlaySoundParams));
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

        private void Advance(float seconds)
        {
            _clock += seconds;
            GameFrameworkEntry.Update(seconds, seconds);
        }

        private static void AssertNoUsingReferences(params string[] typeNames)
        {
            foreach (ReferencePoolInfo info in ReferencePool.GetAllReferencePoolInfos())
            {
                if (Array.IndexOf(typeNames, info.Type.Name) >= 0)
                {
                    Assert.That(info.UsingReferenceCount, Is.Zero, info.Type.FullName);
                }
            }
        }

    }
}
