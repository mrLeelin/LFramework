using System;
using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public sealed class RequestQueue : IDisposable
    {
        private const float ReissueTime = 1;

        private sealed class RetryRequestMsg : IReference
        {
            public static RetryRequestMsg Create(RequestMsg msg, int queueVersion)
            {
                var retryMsg = ReferencePool.Acquire<RetryRequestMsg>();
                retryMsg.Msg = msg;
                retryMsg.QueueVersion = queueVersion;
                return retryMsg;
            }

            public RequestMsg Msg;

            public int QueueVersion;

            public int TimerId;

            public RequestMsg TakeMsg()
            {
                var msg = Msg;
                Msg = null;
                return msg;
            }

            public void Clear()
            {
                Msg = null;
                QueueVersion = 0;
                TimerId = 0;
            }
        }

        private readonly Queue<RequestMsg> _requestQueue = new Queue<RequestMsg>();
        private readonly Queue<ResponseMsg> _responseQueue = new Queue<ResponseMsg>();
        private readonly Queue<ThrowFailMsg> _throwFailQueue = new Queue<ThrowFailMsg>();
        private readonly Dictionary<ulong, RequestMsg> _sendingRequestDict = new Dictionary<ulong, RequestMsg>();
        private readonly Dictionary<int, RetryRequestMsg> _pendingRetryDict = new Dictionary<int, RetryRequestMsg>();
        private readonly List<RetryRequestMsg> _retryClearBatch = new List<RetryRequestMsg>();
        private readonly INetworkChannel _networkChannel;
        private readonly NetworkRequestErrorCallBack _requestErrorCallBack;
        private TimerComponent _timerComponent;
        private int _queueVersion;
        private bool _disposed;

        public RequestQueue(INetworkChannel networkChannel, NetworkRequestErrorCallBack requestErrorCallBack)
        {
            _networkChannel = networkChannel;
            _requestErrorCallBack = requestErrorCallBack;
        }

        public void OnUpdate()
        {
            if (_disposed)
            {
                return;
            }

            ThreadRequestMsg();
            ThreadResponseMsg();
            ThreadThrowFailMsg();
        }

        public void PushRequestMsg(
            NetworkRequestParam requestParam,
            NetworkMessageCallBack callback,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            var msg = RequestMsg.Create(requestParam, successfulCallBack, callback, errorCallBack);
            if (_disposed)
            {
                ReferencePool.Release(msg);
                return;
            }

            if (CheckRepeat(msg))
            {
                ReferencePool.Release(msg);
                return;
            }

            msg.PushTime = DateTime.Now.Ticks;
            _requestQueue.Enqueue(msg);
        }

        public void Dispose()
        {
            _disposed = true;
            Clear();
        }

        public void Clear()
        {
            _queueVersion++;

            _retryClearBatch.Clear();
            foreach (var retryMsg in _pendingRetryDict.Values)
            {
                _retryClearBatch.Add(retryMsg);
            }

            _pendingRetryDict.Clear();
            foreach (var retryMsg in _retryClearBatch)
            {
                var requestMsg = retryMsg.TakeMsg();
                if (requestMsg != null)
                {
                    ReferencePool.Release(requestMsg);
                }

                _timerComponent?.CloseOneTimerImmediately(retryMsg.TimerId);
                ReferencePool.Release(retryMsg);
            }

            _retryClearBatch.Clear();

            foreach (var requestMsg in _requestQueue)
            {
                ReferencePool.Release(requestMsg);
            }

            foreach (var requestMsg in _sendingRequestDict.Values)
            {
                ReferencePool.Release(requestMsg);
            }

            foreach (var throwFailMsg in _throwFailQueue)
            {
                ReferencePool.Release(throwFailMsg);
            }

            foreach (var responseMsg in _responseQueue)
            {
                ReferencePool.Release(responseMsg);
            }

            _requestQueue.Clear();
            _sendingRequestDict.Clear();
            _throwFailQueue.Clear();
            _responseQueue.Clear();
        }

        private void PushResponseMsg(
            ulong serialID,
            int messageID,
            byte[] bytes,
            NetworkMessageCallBack callback,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            OnRequestSuccessful(serialID);
            var msg = ResponseMsg.Create(serialID, messageID, bytes, callback, successfulCallBack, errorCallBack);
            _responseQueue.Enqueue(msg);
        }

        private void OnResponseReceived(
            ulong serialID,
            int messageID,
            byte[] bytes,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            if (!_sendingRequestDict.TryGetValue(serialID, out var requestMsg))
            {
                return;
            }

            PushResponseMsg(
                serialID,
                messageID,
                bytes,
                requestMsg.Callback,
                successfulCallBack,
                errorCallBack);
        }

        private void OnRequestFailed(
            NetworkRequestFailure failure,
            ulong serialID,
            int messageID,
            NetworkRequestInfo requestInfo)
        {
            Log.Error("Request Error id '{0}' state '{1}' serialID '{2}'", messageID, failure, serialID);
            if (!_sendingRequestDict.TryGetValue(serialID, out var requestMsg))
            {
                return;
            }

            var shouldRetry = _requestErrorCallBack?.Invoke(
                failure,
                messageID,
                requestInfo,
                requestMsg.ReSendTimes) ?? false;
            if (shouldRetry && RequestFail(serialID))
            {
                return;
            }

            Log.Error("Throw Fail id '{0}' state '{1}' serialID '{2}'", messageID, failure, serialID);
            ThrowFail(serialID, messageID, requestMsg.Callback, requestMsg.ErrorCallBack);
        }

        private bool RequestFail(ulong serialID)
        {
            if (!_sendingRequestDict.TryGetValue(serialID, out var msg))
            {
                return false;
            }

            msg.ReSendTimes++;
            if (!_sendingRequestDict.Remove(serialID))
            {
                return false;
            }

            var retryMsg = RetryRequestMsg.Create(msg, _queueVersion);
            try
            {
                _timerComponent ??= LFrameworkAspect.Instance.Get<TimerComponent>();
                retryMsg.TimerId = _timerComponent
                    .OpenOneTimerWithSecond(ReissueTime, OnTimerCallBack, retryMsg);
                _pendingRetryDict.Add(retryMsg.TimerId, retryMsg);
            }
            catch
            {
                if (retryMsg.TimerId != 0)
                {
                    _timerComponent?.CloseOneTimerImmediately(retryMsg.TimerId);
                }

                ReferencePool.Release(retryMsg.TakeMsg());
                ReferencePool.Release(retryMsg);
                throw;
            }

            return true;
        }

        private void ThrowFail(
            ulong serialID,
            int messageID,
            NetworkMessageCallBack callBack,
            NetworkMessageErrorCallBack msgErrorCallBack)
        {
            if (!_sendingRequestDict.TryGetValue(serialID, out var requestMsg))
            {
                return;
            }

            _sendingRequestDict.Remove(serialID);
            try
            {
                var throwFailMsg = ThrowFailMsg.Create(serialID, messageID, callBack, msgErrorCallBack);
                _throwFailQueue.Enqueue(throwFailMsg);
            }
            finally
            {
                ReferencePool.Release(requestMsg);
            }
        }

        private void OnTimerCallBack(object obj)
        {
            if (obj is not RetryRequestMsg retryMsg)
            {
                return;
            }

            try
            {
                var ownsRetry = _pendingRetryDict.TryGetValue(retryMsg.TimerId, out var pendingRetry) &&
                                ReferenceEquals(pendingRetry, retryMsg);
                if (ownsRetry)
                {
                    _pendingRetryDict.Remove(retryMsg.TimerId);
                }

                var requestMsg = retryMsg.TakeMsg();
                if (!ownsRetry || _disposed || retryMsg.QueueVersion != _queueVersion)
                {
                    if (requestMsg != null)
                    {
                        ReferencePool.Release(requestMsg);
                    }

                    return;
                }

                if (requestMsg != null)
                {
                    _requestQueue.Enqueue(requestMsg);
                }
            }
            finally
            {
                ReferencePool.Release(retryMsg);
            }
        }

        private bool CheckRepeat(RequestMsg requestMsg)
        {
            if (_sendingRequestDict.Count == 0 && _requestQueue.Count == 0 && _pendingRetryDict.Count == 0)
            {
                return false;
            }

            foreach (var pair in _sendingRequestDict)
            {
                if (pair.Value.RequestParam.SerialID == requestMsg.RequestParam.SerialID)
                {
                    return true;
                }
            }

            foreach (var queuedMsg in _requestQueue)
            {
                if (queuedMsg.RequestParam.SerialID == requestMsg.RequestParam.SerialID)
                {
                    return true;
                }
            }

            foreach (var retryMsg in _pendingRetryDict.Values)
            {
                if (retryMsg.Msg?.RequestParam.SerialID == requestMsg.RequestParam.SerialID)
                {
                    return true;
                }
            }

            return false;
        }

        private void ThreadRequestMsg()
        {
            var count = _requestQueue.Count;
            if (count <= 0)
            {
                return;
            }

            if (!_requestQueue.TryDequeue(out var msg))
            {
                return;
            }

            msg.SendTime = DateTime.Now.Ticks;
            _sendingRequestDict[msg.RequestParam.SerialID] = msg;
            SendRequestMsg(msg);
        }

        private void ThreadThrowFailMsg()
        {
            var count = _throwFailQueue.Count;
            if (count == 0)
            {
                return;
            }

            if (!_throwFailQueue.TryDequeue(out var msg))
            {
                return;
            }

            try
            {
                msg.Callback(msg.SerialID, msg.MessageID, null, null, msg.ErrorCallBack);
            }
            finally
            {
                ReferencePool.Release(msg);
            }
        }

        private void ThreadResponseMsg()
        {
            var count = _responseQueue.Count;
            if (count <= 0)
            {
                return;
            }

            if (!_responseQueue.TryDequeue(out var msg))
            {
                return;
            }

            try
            {
                msg.Callback(
                    msg.SerialID,
                    msg.MessageID,
                    msg.Bytes,
                    msg.SuccessfulCallBack,
                    msg.ErrorCallBack);
            }
            finally
            {
                ReferencePool.Release(msg);
            }
        }

        private void SendRequestMsg(RequestMsg requestMsg)
        {
            _networkChannel?.Post(
                requestMsg.RequestParam,
                OnResponseReceived,
                requestMsg.SuccessfulCallBack,
                requestMsg.ErrorCallBack,
                OnRequestFailed);
        }

        private void OnRequestSuccessful(ulong serialID)
        {
            if (_sendingRequestDict.TryGetValue(serialID, out var requestMsg))
            {
                ReferencePool.Release(requestMsg);
            }

            _sendingRequestDict.Remove(serialID);
        }
    }
}
