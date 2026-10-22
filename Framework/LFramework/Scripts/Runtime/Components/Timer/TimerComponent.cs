using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public sealed class TimerComponent : GameFrameworkComponent
    {
        private readonly struct LoopCallbackEntry
        {
            public LoopCallbackEntry(int timerId, TimerData data)
            {
                TimerId = timerId;
                Data = data;
            }

            public int TimerId { get; }

            public TimerData Data { get; }
        }

        private int _onlyId = 0;

        private readonly Dictionary<int, TimerData> _timerDic = new Dictionary<int, TimerData>();

        private readonly HashSet<int> _deleteListWithCallBack = new HashSet<int>();

        private readonly HashSet<int> _deleteListNoCallBack = new HashSet<int>();

        private readonly List<int> _callbackBatch = new List<int>();

        private readonly List<LoopCallbackEntry> _loopCallbackBatch = new List<LoopCallbackEntry>();

        private bool _isUpdating;

        private bool _updateRequested;


        private long _serverTimeStamp;

        public long ServerTimeStamp => _serverTimeStamp;

        internal bool IsUpdating => _isUpdating;
        
        
        #region Public Method
        
        /// <summary>
        /// 清空所有计时器
        /// </summary>
        /// <param name="isTrigger"></param>
        public void ClearAllTimer(bool isTrigger)
        {
            if (_timerDic.Count <= 0)
            {
                return;
            }

            foreach (var key in _timerDic.Keys)
            {
                JoinDeleteList(key, isTrigger);
            }

            ProcessDeleteListNoCallback();
        }

        /// <summary>
        /// 计时器是否开启
        /// </summary>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public bool IsTimerOpen(int onlyId)
        {
            return _timerDic.TryGetValue(onlyId, out var data) && data.Running;
        }
        
        /// <summary>
        /// 获取剩余毫秒
        /// </summary>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public long GetRemainTimeToLong(int onlyId)
        {
            return _timerDic.TryGetValue(onlyId, out var data) ? data.RemainTimeToLong : 0L;
        }
        /// <summary>
        /// 获取剩余秒数
        /// </summary>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public float GetRemainTimeToFloat(int onlyId)
        {
            return _timerDic.TryGetValue(onlyId, out var data) ? data.RemainTimeToFloat : 0F;
        }

        /// <summary>
        /// 获取计时器
        /// </summary>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public TimerData GetTimer(int onlyId)
        {
            return _timerDic.GetValueOrDefault(onlyId);
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        /// <param name="onlyId"></param>
        public void Reset(int onlyId)
        {
            if (_timerDic.TryGetValue(onlyId, out var data))
            {
                data.Reset();
            }
        }
        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="onlyId"></param>
        public void TimerPause(int onlyId)
        {
            if (_timerDic.TryGetValue(onlyId, out var data))
            {
                data.Pause = true;
            }
        }
        /// <summary>
        /// 继续计时器
        /// </summary>
        /// <param name="onlyId"></param>
        public void TimerContinue(int onlyId)
        {
            if (_timerDic.TryGetValue(onlyId, out var data))
            {
                data.Pause = false;
            }
        }
        /// <summary>
        /// 增加计时器的时间
        /// </summary>
        /// <param name="onlyId"></param>
        /// <param name="delayTime"></param>
        public void AppendTime(int onlyId, float delayTime)
        {
            if (!_timerDic.TryGetValue(onlyId, out var data))
            {
                return;
            }

            data.IncreaseTime(delayTime);
        }
        /// <summary>
        /// 下一帧关闭一个计时器
        /// </summary>
        /// <param name="onlyId"></param>
        /// <param name="isTrigger"></param>
        public void CloseOneTimer(int onlyId, bool isTrigger = false)
        {
            if (!_timerDic.TryGetValue(onlyId, out var data))
            {
                return;
            }

            JoinDeleteList(onlyId, isTrigger);
        }
        /// <summary>
        /// 直接关闭一个计时器
        /// </summary>
        /// <param name="onlyId"></param>
        /// <param name="isTrigger"></param>
        public void CloseOneTimerImmediately(int onlyId, bool isTrigger = false)
        {
            if (!DetachTimer(onlyId, out var data))
            {
                return;
            }

            try
            {
                if (isTrigger)
                {
                    data.TriggerCallBack();
                }
            }
            finally
            {
                ReferencePool.Release(data);
            }
        }
        /// <summary>
        /// 开启一个计时器
        /// </summary>
        /// <param name="totalTime"></param>
        /// <param name="callBack"></param>
        /// <param name="userData"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int OpenOneTimerWithSecond(float totalTime, OnTimerCallBack callBack, object userData = null,
            bool loop = false)
        {
            if (!TimerExtension.TryGetPositiveTicks(totalTime, out var ticks))
            {
                return 0;
            }

            return OpenOneTimerWithMillisecond(ticks, callBack, userData, loop);
        }
        /// <summary>
        /// 开启一个计时器
        /// </summary>
        /// <param name="totalTime"></param>
        /// <param name="callBack"></param>
        /// <param name="userData"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int OpenOneTimerWithMillisecond(long totalTime, OnTimerCallBack callBack, object userData = null,
            bool loop = false)
        {
            if (totalTime <= 0 || _serverTimeStamp > long.MaxValue - totalTime)
            {
                return 0;
            }
            
            int onlyId = AllocateTimerId();
            var data = ReferencePool.Acquire<TimerData>();
            data.SetComponent(this);
            try
            {
                data.InitializeWithMillisecond(onlyId, totalTime, callBack, loop, userData);
            }
            catch
            {
                ReferencePool.Release(data);
                throw;
            }

            _timerDic.Add(onlyId, data);
            return onlyId;
        }
        
        #endregion


        #region Private Mathod

        /// <summary>
        /// 分配计时器 ID：到达 int.MaxValue 后回绕到 1，跳过 0（无效 ID）和仍存活的计时器 ID。
        /// </summary>
        private int AllocateTimerId()
        {
            // 最多尝试 _timerDic.Count + 1 次即可找到空闲 ID（鸽巢原理）
            for (int attempt = 0; attempt <= _timerDic.Count; attempt++)
            {
                _onlyId = _onlyId == int.MaxValue ? 1 : _onlyId + 1;
                if (!_timerDic.ContainsKey(_onlyId))
                {
                    return _onlyId;
                }
            }

            throw new InvalidOperationException("No timer id is available.");
        }

        internal void JoinDeleteList(int onlyId, bool trigger)
        {
            if (_timerDic.Count <= 0)
            {
                return;
            }

            if (!_timerDic.TryGetValue(onlyId, out var data))
            {
                return;
            }

            data.Close();

            if (trigger)
            {
                if (!_deleteListWithCallBack.Contains(onlyId))
                {
                    _deleteListWithCallBack.Add(onlyId);
                }
            }
            else
            {
                if (!_deleteListNoCallBack.Contains(onlyId))
                {
                    _deleteListNoCallBack.Add(onlyId);
                }
            }
        }

        internal void RequestUpdate()
        {
            UpdateAllTimer();
        }

        public override void AwakeComponent()
        {
            base.AwakeComponent();
            _serverTimeStamp = DateTime.Now.Ticks;
        }

        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            base.UpdateComponent(elapseSeconds, realElapseSeconds);
            _serverTimeStamp += Time.unscaledDeltaTime.ToTicks();
            UpdateAllTimer();
        }

        private bool DetachTimer(int onlyId, out TimerData data)
        {
            if (!_timerDic.TryGetValue(onlyId, out data))
            {
                return false;
            }

            data.Close();
            _timerDic.Remove(onlyId);
            _deleteListWithCallBack.Remove(onlyId);
            _deleteListNoCallBack.Remove(onlyId);
            return true;
        }

        private void ProcessDeleteListWithCallback()
        {
            if (_deleteListWithCallBack.Count == 0)
            {
                return;
            }

            _callbackBatch.Clear();
            foreach (var onlyId in _deleteListWithCallBack)
            {
                _callbackBatch.Add(onlyId);
            }

            _deleteListWithCallBack.Clear();
            foreach (var onlyId in _callbackBatch)
            {
                // 待派发计时器仍可取消；使用原始 ID，避免回池复用后误派发。
                var trigger = !_deleteListNoCallBack.Contains(onlyId);
                if (!DetachTimer(onlyId, out var data))
                {
                    continue;
                }

                try
                {
                    if (trigger)
                    {
                        data.TriggerCallBack();
                    }
                }
                finally
                {
                    ReferencePool.Release(data);
                }
            }

            _callbackBatch.Clear();
        }

        private void ProcessDeleteListNoCallback()
        {
            foreach (var onlyId in _deleteListNoCallBack)
            {
                if (_timerDic.TryGetValue(onlyId, out var data))
                {
                    data.Close();
                    _timerDic.Remove(onlyId);
                    ReferencePool.Release(data);
                }
            }

            _deleteListNoCallBack.Clear();
        }

        private void ProcessLoopCallbacks()
        {
            foreach (var entry in _loopCallbackBatch)
            {
                if (!_timerDic.TryGetValue(entry.TimerId, out var current) ||
                    !ReferenceEquals(current, entry.Data) ||
                    !entry.Data.Running ||
                    !entry.Data.CheckExpiration())
                {
                    continue;
                }

                entry.Data.RearmLoop();
                entry.Data.TriggerCallBack();
            }

            _loopCallbackBatch.Clear();
        }


        private void UpdateAllTimer()
        {
            if (_isUpdating)
            {
                _updateRequested = true;
                return;
            }

            _isUpdating = true;
            try
            {
                // A callback can request another update by changing an already due timer.
                // Bound synchronous draining so a loop callback cannot keep this frame busy forever.
                for (var pass = 0; pass < 2; pass++)
                {
                    _updateRequested = false;
                    UpdateAllTimerCore();
                    if (!_updateRequested)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateAllTimerCore()
        {
            _loopCallbackBatch.Clear();
            foreach (var value in _timerDic.Values)
            {
                if (value.CheckExpiration())
                {
                    _loopCallbackBatch.Add(new LoopCallbackEntry(value.OnlyId, value));
                }
            }

            if (_loopCallbackBatch.Count > 0)
            {
                ProcessLoopCallbacks();
            }

            if (_deleteListNoCallBack.Count > 0)
            {
                ProcessDeleteListNoCallback();
            }

            if (_deleteListWithCallBack.Count > 0)
            {
                ProcessDeleteListWithCallback();
            }
        }

        private void RefreshServerTime(long ticks)
        {
            foreach (var data in _timerDic.Values)
            {
                data.IncreaseTime(ticks);
            }

            _serverTimeStamp += ticks;
        }

        #endregion
    }
}
