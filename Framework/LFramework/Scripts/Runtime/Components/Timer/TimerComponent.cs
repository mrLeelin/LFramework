using System;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public sealed class TimerComponent : GameFrameworkComponent
    {
        private int _onlyId = 0;

        private readonly Dictionary<int, TimerData> _timerDic = new Dictionary<int, TimerData>();

        private readonly HashSet<int> _deleteListWithCallBack = new HashSet<int>();

        private readonly HashSet<int> _deleteListNoCallBack = new HashSet<int>();


        private long _serverTimeStamp;

        public long ServerTimeStamp => _serverTimeStamp;
        
        
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

            _onlyId = 0;
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

            if (!data.Running)
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
            if (!_timerDic.TryGetValue(onlyId, out var data))
            {
                return;
            }

            if (!data.Running)
            {
                return;
            }

            if (isTrigger)
            {
                data.TriggerCallBack();
            }

            JoinDeleteList(onlyId, false);
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
            if (totalTime <= 0)
            {
                return 0;
            }
            
            _onlyId++;
            if (!_timerDic.TryGetValue(_onlyId, out var data))
            {
                data = ReferencePool.Acquire<TimerData>();
                data.SetComponent(this);
                _timerDic.Add(_onlyId, data);
            }

            data.InitWithSecond(_onlyId, totalTime, callBack, loop, userData);
            return _onlyId;
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
            if (totalTime <= 0)
            {
                return 0;
            }
            
            _onlyId++;
            if (!_timerDic.TryGetValue(_onlyId, out var data))
            {
                data = ReferencePool.Acquire<TimerData>();
                data.SetComponent(this);
                _timerDic.Add(_onlyId, data);
            }

            data.InitializeWithMillisecond(_onlyId, totalTime, callBack, loop, userData);
            return _onlyId;
        }
        
        #endregion


        #region Private Mathod

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

        private void RecycleTimer(int onlyID)
        {
            if (!_timerDic.TryGetValue(onlyID, out var data))
            {
                return;
            }

            data.Close();
            _timerDic.Remove(onlyID);
            ReferencePool.Release(data);
        }

        private void ProcessDeleteListWithCallback()
        {
            if (_deleteListWithCallBack == null)
            {
                return;
            }

            foreach (int item in _deleteListWithCallBack)
            {
                var timerData = _timerDic[item];
                timerData?.TriggerCallBack();
                RecycleTimer(item);
            }

            _deleteListWithCallBack.Clear();
        }

        private void ProcessDeleteListNoCallback()
        {
            foreach (int item in _deleteListNoCallBack)
            {
                RecycleTimer(item);
            }

            _deleteListNoCallBack.Clear();
        }


        private void UpdateAllTimer()
        {
            foreach (var value in _timerDic.Values)
            {
                value.OnUpdate();
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