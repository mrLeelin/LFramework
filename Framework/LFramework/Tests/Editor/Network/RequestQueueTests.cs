using System;
using System.Linq;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Network
{
    public sealed class RequestQueueTests
    {
        private static readonly FieldInfo ServerTimestampField = typeof(TimerComponent).GetField(
            "_serverTimeStamp",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo UpdateAllTimerMethod = typeof(TimerComponent).GetMethod(
            "UpdateAllTimer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private TimerComponent _timer;
        private bool _previousStrictCheck;

        [SetUp]
        public void SetUp()
        {
            _previousStrictCheck = ReferencePool.EnableStrictCheck;
            ReferencePool.EnableStrictCheck = true;
            SingletonManager.Close();
            SingletonManager.AddSingleton(new LFrameworkAspect());
            _timer = new TimerComponent();
            LServices.Register(_timer);
        }

        [TearDown]
        public void TearDown()
        {
            _timer.ClearAllTimer(false);
            SingletonManager.Close();
            ReferencePool.EnableStrictCheck = _previousStrictCheck;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearingPendingRetryReleasesRequestExactlyOnce(bool dispose)
        {
            var baseline = GetUsingCount<RequestMsg>();
            var channel = new FakeNetworkChannel();
            var queue = new RequestQueue(channel, (_, _, _, _) => true);
            queue.PushRequestMsg(CreateParam(1), (_, _, _, _, _) => { }, null, null);
            queue.OnUpdate();
            channel.FailLastRequest();

            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline + 1));
            if (dispose)
            {
                queue.Dispose();
            }
            else
            {
                queue.Clear();
            }

            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
            Assert.That(() => queue.Clear(), Throws.Nothing);
            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
        }

        [Test]
        public void FinalFailureReleasesRequestBeforeFailureNotificationRuns()
        {
            var baseline = GetUsingCount<RequestMsg>();
            var callbackCount = 0;
            var channel = new FakeNetworkChannel();
            using var queue = new RequestQueue(channel, (_, _, _, _) => false);
            queue.PushRequestMsg(CreateParam(2), (_, _, _, _, _) => callbackCount++, null, null);
            queue.OnUpdate();

            channel.FailLastRequest();

            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
            Assert.That(callbackCount, Is.Zero);
            queue.OnUpdate();
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
        }

        [Test]
        public void RetryTimerTransfersRequestBackToQueueAndFinalFailureReleasesIt()
        {
            var baseline = GetUsingCount<RequestMsg>();
            var callbackCount = 0;
            var channel = new FakeNetworkChannel();
            using var queue = new RequestQueue(channel, (_, _, _, retryCount) => retryCount == 0);
            queue.PushRequestMsg(CreateParam(3), (_, _, _, _, _) => callbackCount++, null, null);
            queue.OnUpdate();
            channel.FailLastRequest();

            AdvanceTimer(2);
            queue.OnUpdate();
            channel.FailLastRequest();

            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
            queue.OnUpdate();
            Assert.That(callbackCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearingDuringDueTimerBatchReleasesRetryExactlyOnce(bool dispose)
        {
            var baseline = GetUsingCount<RequestMsg>();
            var channel = new FakeNetworkChannel();
            using var queue = new RequestQueue(channel, (_, _, _, _) => true);
            _timer.OpenOneTimerWithSecond(1, _ =>
            {
                if (dispose)
                {
                    queue.Dispose();
                }
                else
                {
                    queue.Clear();
                }
            });
            queue.PushRequestMsg(CreateParam(4), (_, _, _, _, _) => { }, null, null);
            queue.OnUpdate();
            channel.FailLastRequest();
            var retryType = GetPendingRetry(queue).GetType();
            var retryBaseline = GetUsingCount(retryType) - 1;

            AdvanceTimer(2);
            queue.OnUpdate();
            AdvanceTimer(2);

            Assert.That(channel.PostCount, Is.EqualTo(1));
            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
            Assert.That(GetUsingCount(retryType), Is.EqualTo(retryBaseline));
        }

        [Test]
        public void ClearedRetryReusedInSameBatchWaitsForItsOwnTimer()
        {
            var baseline = GetUsingCount<RequestMsg>();
            var channel = new FakeNetworkChannel();
            using var queue = new RequestQueue(channel, (_, _, _, _) => true);
            object oldRetry = null;
            object newRetry = null;
            _timer.OpenOneTimerWithSecond(1, _ =>
            {
                // 移除其他空闲项，确保旧回调数据在本帧被复用。
                ReferencePool.RemoveAll(oldRetry.GetType());
                queue.Clear();
                queue.PushRequestMsg(CreateParam(6), (_, _, _, _, _) => { }, null, null);
                queue.OnUpdate();
                channel.FailLastRequest();
                newRetry = GetPendingRetry(queue);
            });
            queue.PushRequestMsg(CreateParam(5), (_, _, _, _, _) => { }, null, null);
            queue.OnUpdate();
            channel.FailLastRequest();
            oldRetry = GetPendingRetry(queue);

            AdvanceTimer(2);
            queue.OnUpdate();

            Assert.That(newRetry, Is.SameAs(oldRetry));
            Assert.That(channel.PostCount, Is.EqualTo(2), "An old due callback must not consume the replacement retry.");
            AdvanceTimer(0.5f);
            queue.OnUpdate();
            Assert.That(channel.PostCount, Is.EqualTo(2));
            AdvanceTimer(0.5f);
            queue.OnUpdate();
            Assert.That(channel.PostCount, Is.EqualTo(3));
            queue.Clear();
            Assert.That(GetUsingCount<RequestMsg>(), Is.EqualTo(baseline));
        }

        private static NetworkRequestParam CreateParam(ulong serialId)
        {
            return new NetworkRequestParam
            {
                SerialID = serialId,
                MessageID = (int)serialId
            };
        }

        private static int GetUsingCount<T>() where T : class, IReference
        {
            return GetUsingCount(typeof(T));
        }

        private static int GetUsingCount(Type type)
        {
            return ReferencePool.GetAllReferencePoolInfos().FirstOrDefault(info => info.Type == type).UsingReferenceCount;
        }

        private static object GetPendingRetry(RequestQueue queue)
        {
            var pending = (System.Collections.IDictionary)typeof(RequestQueue)
                .GetField("_pendingRetryDict", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(queue);
            return pending.Values.Cast<object>().Single();
        }

        private void AdvanceTimer(float seconds)
        {
            ServerTimestampField.SetValue(_timer, _timer.ServerTimeStamp + seconds.ToTicks());
            UpdateAllTimerMethod.Invoke(_timer, null);
        }

        private sealed class FakeNetworkChannel : INetworkChannel
        {
            private NetworkRequestFailedCallBack _failureCallback;
            private NetworkRequestParam _lastRequest;

            public int PostCount { get; private set; }

            public event EventHandler TokenErrorCallBack;

            public void Initialize() { }
            public void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
            public void Connect(string url) { }
            public void SetTimeOut(int loginTimeOut, int timeOut) { }
            public void Clear() { }

            public void Post(
                NetworkRequestParam requestParam,
                NetworkMessageCallBack callBack,
                NetworkMessageSuccessfulCallBack successfulCallBack,
                NetworkMessageErrorCallBack errorCallBack,
                NetworkRequestFailedCallBack requestFailedCallBack)
            {
                PostCount++;
                _lastRequest = requestParam;
                _failureCallback = requestFailedCallBack;
            }

            public void FailLastRequest()
            {
                _failureCallback(
                    new NetworkRequestFailure(-1, "test failure"),
                    _lastRequest.SerialID,
                    _lastRequest.MessageID,
                    _lastRequest.RequestInfo);
            }
        }
    }
}
