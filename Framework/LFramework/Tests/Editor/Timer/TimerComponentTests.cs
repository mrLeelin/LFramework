using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Timer
{
    public sealed class TimerComponentTests
    {
        private readonly List<TimerComponent> _createdTimers = new List<TimerComponent>();

        private static readonly FieldInfo ServerTimestampField = typeof(TimerComponent).GetField(
            "_serverTimeStamp",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo UpdateAllTimerMethod = typeof(TimerComponent).GetMethod(
            "UpdateAllTimer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            ReferencePool.RemoveAll<TimerData>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var timer in _createdTimers)
            {
                timer.ClearAllTimer(false);
            }

            _createdTimers.Clear();
        }

        [Test]
        public void LoopTimerInvokesCallbackAndRemainsRunning()
        {
            var timer = CreateTimer();
            var callbackCount = 0;
            var timerId = timer.OpenOneTimerWithSecond(1, _ => callbackCount++, loop: true);

            Advance(timer, 2);
            Advance(timer, 2);

            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(timer.IsTimerOpen(timerId), Is.True);
        }

        [Test]
        public void PauseFreezesRemainingTimeUntilContinue()
        {
            var timer = CreateTimer();
            var timerId = timer.OpenOneTimerWithSecond(10, _ => { });
            var remainingBeforePause = timer.GetRemainTimeToLong(timerId);

            timer.TimerPause(timerId);
            Advance(timer, 20);

            Assert.That(timer.GetRemainTimeToLong(timerId), Is.EqualTo(remainingBeforePause));
            timer.TimerContinue(timerId);
            Advance(timer, 1);
            Assert.That(timer.GetRemainTimeToFloat(timerId), Is.EqualTo(9f).Within(0.001f));
        }

        [Test]
        public void ClearWithCallbackDoesNotReuseIdOrDeleteNewTimer()
        {
            var timer = CreateTimer();
            var oldCallbackCount = 0;
            var newCallbackCount = 0;
            var oldTimerId = timer.OpenOneTimerWithSecond(1, _ => oldCallbackCount++);

            timer.ClearAllTimer(true);
            var newTimerId = timer.OpenOneTimerWithSecond(100, _ => newCallbackCount++);
            Advance(timer, 0.1f);

            Assert.That(newTimerId, Is.GreaterThan(oldTimerId));
            Assert.That(oldCallbackCount, Is.EqualTo(1));
            Assert.That(newCallbackCount, Is.Zero);
            Assert.That(timer.IsTimerOpen(newTimerId), Is.True);
        }

        [Test]
        public void CallbackCanCloseAnotherTimerWithoutMutatingCurrentBatch()
        {
            var timer = CreateTimer();
            var callbackCount = 0;
            var secondTimerId = 0;
            timer.OpenOneTimerWithSecond(1, _ => timer.CloseOneTimer(secondTimerId, true));
            secondTimerId = timer.OpenOneTimerWithSecond(100, _ => callbackCount++);

            Assert.DoesNotThrow(() => Advance(timer, 2));
            Assert.That(callbackCount, Is.Zero);

            Advance(timer, 0.1f);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(timer.GetTimer(secondTimerId), Is.Null);
        }

        [Test]
        public void CallbackCreatedTimerIsExcludedFromOldDeleteBatch()
        {
            var timer = CreateTimer();
            var newTimerId = 0;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                timer.ClearAllTimer(true);
                newTimerId = timer.OpenOneTimerWithSecond(100, __ => { });
            });

            Advance(timer, 2);
            Advance(timer, 0.1f);

            Assert.That(newTimerId, Is.GreaterThan(0));
            Assert.That(timer.IsTimerOpen(newTimerId), Is.True);
        }

        [Test]
        public void CallbackUpdateIsDeferredUntilCurrentCallbackBatchCompletes()
        {
            var timer = CreateTimer();
            var firstCallbackCount = 0;
            var secondCallbackCount = 0;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                firstCallbackCount++;
                timer.UpdateComponent(0f, 0f);
            });
            timer.OpenOneTimerWithSecond(1, _ => secondCallbackCount++);

            Assert.DoesNotThrow(() => Advance(timer, 2));
            Assert.That(firstCallbackCount, Is.EqualTo(1));
            Assert.That(secondCallbackCount, Is.EqualTo(1));
        }

        [Test]
        [Timeout(1000)]
        public void LoopCallbackCannotDrainUpdatesIndefinitelyInOneFrame()
        {
            var timer = CreateTimer();
            var callbackCount = 0;
            var timerId = 0;
            timerId = timer.OpenOneTimerWithSecond(1, _ =>
            {
                callbackCount++;
                if (callbackCount < 100)
                {
                    timer.GetTimer(timerId).DecreaseTime(1);
                }
            }, loop: true);

            Advance(timer, 1);

            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(timer.IsTimerOpen(timerId), Is.True);

            // The still-due timer is processed by a later outer update.
            Advance(timer, 0);
            Assert.That(callbackCount, Is.EqualTo(4));
        }

        [Test]
        public void TimerDataUpdateDuringCallbackDoesNotRedispatchCurrentBatch()
        {
            var timer = CreateTimer();
            var firstCallbackCount = 0;
            var secondCallbackCount = 0;
            TimerData secondTimer = null;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                firstCallbackCount++;
                secondTimer.OnUpdate();
            });
            var secondTimerId = timer.OpenOneTimerWithSecond(1, _ => secondCallbackCount++);
            secondTimer = timer.GetTimer(secondTimerId);

            Assert.DoesNotThrow(() => Advance(timer, 2));
            Assert.That(firstCallbackCount, Is.EqualTo(1));
            Assert.That(secondCallbackCount, Is.EqualTo(1));
        }

        [Test]
        public void LoopBatchUsesOriginalIdWhenClosedTimerObjectIsReused()
        {
            var timer = CreateTimer();
            var removedTimerCallbackCount = 0;
            var replacementCallbackCount = 0;
            var replacementTimerId = 0;
            TimerData removedTimer = null;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                timer.CloseOneTimerImmediately(removedTimer.OnlyId);
                replacementTimerId = timer.OpenOneTimerWithSecond(100, __ => replacementCallbackCount++);
            }, loop: true);
            var removedTimerId = timer.OpenOneTimerWithSecond(1, _ => removedTimerCallbackCount++, loop: true);
            removedTimer = timer.GetTimer(removedTimerId);

            Advance(timer, 2);

            Assert.That(timer.GetTimer(replacementTimerId), Is.SameAs(removedTimer));
            Assert.That(removedTimerCallbackCount, Is.Zero);
            Assert.That(replacementCallbackCount, Is.Zero);
            Assert.That(timer.IsTimerOpen(replacementTimerId), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EarlierCallbackCanCancelAnotherDueTimer(bool immediately)
        {
            var timer = CreateTimer();
            var secondTimerId = 0;
            var secondCallbackCount = 0;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                if (immediately)
                {
                    timer.CloseOneTimerImmediately(secondTimerId);
                }
                else
                {
                    timer.CloseOneTimer(secondTimerId);
                }
            });
            secondTimerId = timer.OpenOneTimerWithSecond(1, _ => secondCallbackCount++);

            Advance(timer, 2);

            Assert.That(secondCallbackCount, Is.Zero);
            Assert.That(timer.GetTimer(secondTimerId), Is.Null);
        }

        [Test]
        public void ClearDuringCallbackCancelsRemainingDueTimers()
        {
            var timer = CreateTimer();
            var secondCallbackCount = 0;
            timer.OpenOneTimerWithSecond(1, _ => timer.ClearAllTimer(false));
            var secondTimerId = timer.OpenOneTimerWithSecond(1, _ => secondCallbackCount++);

            Advance(timer, 2);

            Assert.That(secondCallbackCount, Is.Zero);
            Assert.That(timer.GetTimer(secondTimerId), Is.Null);
        }

        [Test]
        public void DueBatchDoesNotDispatchAReusedTimer()
        {
            var timer = CreateTimer();
            var oldTimerId = 0;
            var newTimerId = 0;
            var oldCallbackCount = 0;
            var newCallbackCount = 0;
            timer.OpenOneTimerWithSecond(1, _ =>
            {
                timer.CloseOneTimerImmediately(oldTimerId);
                newTimerId = timer.OpenOneTimerWithSecond(1, __ => newCallbackCount++);
            });
            oldTimerId = timer.OpenOneTimerWithSecond(1, _ => oldCallbackCount++);
            var oldTimer = timer.GetTimer(oldTimerId);

            Advance(timer, 2);

            Assert.That(timer.GetTimer(newTimerId), Is.SameAs(oldTimer));
            Assert.That(oldCallbackCount, Is.Zero);
            Assert.That(newCallbackCount, Is.Zero);
            Advance(timer, 1);
            Assert.That(newCallbackCount, Is.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.Epsilon)]
        [TestCase(0.00000001f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.MaxValue)]
        public void InvalidSecondDurationDoesNotCreateATimer(float seconds)
        {
            var timer = CreateTimer();

            Assert.That(timer.OpenOneTimerWithSecond(seconds, _ => { }, loop: true), Is.Zero);
            Assert.That(timer.OpenOneTimerWithSecond(1, _ => { }), Is.EqualTo(1));
        }

        [Test]
        [Timeout(1000)]
        public void SingleTickLoopSkipsLargeMissedIntervalsAndPreservesPhase()
        {
            var timer = CreateTimer();
            var callbackCount = 0;
            var timerId = timer.OpenOneTimerWithSecond(0.0000001f, _ => callbackCount++, loop: true);

            ServerTimestampField.SetValue(timer, 1000000000000L);
            UpdateAllTimerMethod.Invoke(timer, null);

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(timer.GetTimer(timerId).EndTimeStamp, Is.EqualTo(1000000000001L));
        }

        [Test]
        public void DurationOverflowIsRejectedAndLoopStopsAtTimestampLimit()
        {
            var timer = CreateTimer();
            var callbackCount = 0;
            ServerTimestampField.SetValue(timer, long.MaxValue - 10);
            Assert.That(timer.OpenOneTimerWithMillisecond(11, _ => { }, loop: true), Is.Zero);
            var timerId = timer.OpenOneTimerWithMillisecond(10, _ => callbackCount++, loop: true);

            ServerTimestampField.SetValue(timer, long.MaxValue);
            UpdateAllTimerMethod.Invoke(timer, null);

            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(timer.GetTimer(timerId), Is.Null);
        }

        [Test]
        public void ResumingPausedTimerAtTimestampLimitDoesNotWrapDeadline()
        {
            var timer = CreateTimer();
            ServerTimestampField.SetValue(timer, long.MaxValue - 2);
            var timerId = timer.OpenOneTimerWithMillisecond(2, _ => { });

            timer.TimerPause(timerId);
            ServerTimestampField.SetValue(timer, long.MaxValue - 1);
            timer.TimerContinue(timerId);

            Assert.That(timer.GetTimer(timerId).EndTimeStamp, Is.EqualTo(long.MaxValue));
            Assert.That(timer.GetRemainTimeToLong(timerId), Is.EqualTo(1));
            UpdateAllTimerMethod.Invoke(timer, null);
            Assert.That(timer.IsTimerOpen(timerId), Is.True);
        }

        [Test]
        public void DirectTimerInitializationRejectsNonPositiveTickDurations()
        {
            var data = new TimerData();
            data.SetComponent(CreateTimer());

            Assert.Throws<ArgumentOutOfRangeException>(() => data.InitializeWithMillisecond(1, 0, _ => { }, true, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => data.InitWithSecond(1, float.NaN, _ => { }, true, null));
            Assert.That(data.Running, Is.False);
        }

        [Test]
        public void TimerIdWrapsAtMaxValueAndSkipsLiveIds()
        {
            var timer = CreateTimer();
            var onlyIdField = typeof(TimerComponent).GetField("_onlyId", BindingFlags.Instance | BindingFlags.NonPublic);

            // ID 1 保持存活，回绕后必须跳过它
            var liveId = timer.OpenOneTimerWithSecond(100, _ => { });
            Assert.That(liveId, Is.EqualTo(1));

            onlyIdField.SetValue(timer, int.MaxValue - 1);
            var maxId = timer.OpenOneTimerWithSecond(100, _ => { });
            var wrappedId = timer.OpenOneTimerWithSecond(100, _ => { });

            Assert.That(maxId, Is.EqualTo(int.MaxValue));
            Assert.That(wrappedId, Is.EqualTo(2));
            Assert.That(timer.IsTimerOpen(liveId), Is.True);
            Assert.That(timer.IsTimerOpen(maxId), Is.True);
            Assert.That(timer.IsTimerOpen(wrappedId), Is.True);
        }

        private static void Advance(TimerComponent timer, float seconds)
        {
            ServerTimestampField.SetValue(timer, timer.ServerTimeStamp + seconds.ToTicks());
            UpdateAllTimerMethod.Invoke(timer, null);
        }

        private TimerComponent CreateTimer()
        {
            var timer = new TimerComponent();
            _createdTimers.Add(timer);
            return timer;
        }
    }
}
