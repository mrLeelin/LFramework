using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class TimerDataExtension
    {
        public static void AddListener(this TimerData timerData, OnTimerCallBack callBack)
        {
            if (timerData == null)
            {
                return;
            }

            timerData.TimerCallBack += callBack;
        }

        public static void RemoveListener(this TimerData timerData, OnTimerCallBack callBack)
        {
            if (timerData == null)
            {
                return;
            }

            timerData.TimerCallBack -= callBack;
        }
    }

    public delegate void OnTimerCallBack(object obj);

    public sealed class TimerData : IReference
    {
        private TimerComponent _timerComponent;
        private int _onlyId;
        private bool _loop;
        private bool _pause;
        private long _startTimeStamp;
        private long _endTimeStamp;
        private long _totalTime;
        private object _userData;
        private OnTimerCallBack _callBack;
        private bool _running;

        public int OnlyId => _onlyId;
        public bool Loop => _loop;

        public long StartTimeStamp => _startTimeStamp;
        public long EndTimeStamp => _endTimeStamp;
        public bool Running => _running;

        public long RemainTimeToLong => _endTimeStamp - _timerComponent.ServerTimeStamp;

        public float RemainTimeToFloat => (_endTimeStamp - _timerComponent.ServerTimeStamp).ToSeconds();

        public long PastTime => _timerComponent.ServerTimeStamp - _startTimeStamp;

        public float Progress =>  (PastTime / (float)(_endTimeStamp - _startTimeStamp));

        public bool Pause
        {
            get => _pause;
            set
            {
                if (!_running)
                {
                    return;
                }

                _pause = value;
            }
        }

        internal event OnTimerCallBack TimerCallBack
        {
            add => _callBack += value;
            remove => _callBack -= value;
        }

        public void InitWithSecond(int onlyId, float totalTime, OnTimerCallBack callBack, bool loop, object userData)
        {
            InitializeWithMillisecond(onlyId, totalTime.ToTicks(), callBack, loop, userData);
        }

        public void InitializeWithMillisecond(int onlyId, long totalTime, OnTimerCallBack callBack, bool loop,
            object userData)
        {
            this._onlyId = onlyId;
            this._totalTime = totalTime;
            TimerCallBack += callBack;
            this._pause = false;
            this._userData = userData;
            this._startTimeStamp = _timerComponent.ServerTimeStamp;
            this._endTimeStamp = _startTimeStamp + this._totalTime;
            this._loop = loop;
            RefreshRunning();
        }

        public void SetComponent(TimerComponent timerComponent)
        {
            _timerComponent = timerComponent;
        }


        public void OnUpdate()
        {
            if (_running)
            {
                Timing();
            }
        }

        public void IncreaseTime(long time)
        {
            if (_running)
            {
                _endTimeStamp += time;
            }
        }

        public void IncreaseTime(float time)
        {
            if (_running)
            {
                _endTimeStamp += time.ToTicks();
            }
        }

        public void DecreaseTime(float time)
        {
            if (_running)
            {
                _endTimeStamp -= time.ToTicks();
                Timing();
            }
        }

        public void Reset()
        {
            if (!_running)
            {
                return;
            }

            _startTimeStamp = _timerComponent.ServerTimeStamp;
            _endTimeStamp = _startTimeStamp + _totalTime;
            Timing();
        }


        public void Close()
        {
            _running = false;
        }

        public void TriggerCallBack()
        {
            if (_callBack != null)
            {
                try
                {
                    _callBack.Invoke(_userData);
                }
                catch (Exception e)
                {
                    Log.Error($"TimerData callback exception: {e}");
                }
            }
        }

        private void Timing()
        {
            RefreshRunning();
            if (!Running)
            {
                if (Loop)
                {
                    Reset();
                }
                else
                {
                    _timerComponent.JoinDeleteList(OnlyId, true);
                }
            }
        }

        private void RefreshRunning()
        {
            if (Pause)
            {
                return;
            }

            _running = _timerComponent.ServerTimeStamp < _endTimeStamp;
        }

        public void Clear()
        {
            Close();
            _timerComponent = null;
            _onlyId = 0;
            _loop = false;
            _pause = false;
            _startTimeStamp = 0;
            _endTimeStamp = 0;
            _totalTime = 0;
            _userData = null;
            _callBack = null;
            _running = false;
        }
    }
}