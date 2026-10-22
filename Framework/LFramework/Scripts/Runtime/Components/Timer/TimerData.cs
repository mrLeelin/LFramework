using System;
using GameFramework;
using UnityGameFramework.Runtime;

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
        private long _pauseStartedTimestamp;
        private object _userData;
        private OnTimerCallBack _callBack;
        private bool _running;

        public int OnlyId => _onlyId;
        public bool Loop => _loop;

        public long StartTimeStamp => _startTimeStamp;
        public long EndTimeStamp => _endTimeStamp;
        public bool Running => _running;

        public long RemainTimeToLong => _endTimeStamp - CurrentTimestamp;

        public float RemainTimeToFloat => (_endTimeStamp - CurrentTimestamp).ToSeconds();

        public long PastTime => CurrentTimestamp - _startTimeStamp;

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

                if (_pause == value)
                {
                    return;
                }

                if (value)
                {
                    _pauseStartedTimestamp = _timerComponent.ServerTimeStamp;
                }
                else
                {
                    var pausedDuration = _timerComponent.ServerTimeStamp - _pauseStartedTimestamp;
                    _startTimeStamp += pausedDuration;
                    _endTimeStamp = pausedDuration > long.MaxValue - _endTimeStamp
                        ? long.MaxValue
                        : _endTimeStamp + pausedDuration;
                    _pauseStartedTimestamp = 0;
                }

                _pause = value;
            }
        }

        private long CurrentTimestamp => _pause ? _pauseStartedTimestamp : _timerComponent.ServerTimeStamp;

        internal event OnTimerCallBack TimerCallBack
        {
            add => _callBack += value;
            remove => _callBack -= value;
        }

        public void InitWithSecond(int onlyId, float totalTime, OnTimerCallBack callBack, bool loop, object userData)
        {
            if (!TimerExtension.TryGetPositiveTicks(totalTime, out var ticks))
            {
                throw new ArgumentOutOfRangeException(nameof(totalTime), "Timer duration must fit a positive tick count.");
            }

            InitializeWithMillisecond(onlyId, ticks, callBack, loop, userData);
        }

        public void InitializeWithMillisecond(int onlyId, long totalTime, OnTimerCallBack callBack, bool loop,
            object userData)
        {
            if (totalTime <= 0 || _timerComponent.ServerTimeStamp > long.MaxValue - totalTime)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTime), "Timer duration must fit a future timestamp.");
            }

            _onlyId = onlyId;
            _totalTime = totalTime;
            _callBack = callBack;
            _pause = false;
            _pauseStartedTimestamp = 0;
            _userData = userData;
            _startTimeStamp = _timerComponent.ServerTimeStamp;
            _endTimeStamp = _startTimeStamp + _totalTime;
            _loop = loop;
            _running = true;
        }

        public void SetComponent(TimerComponent timerComponent)
        {
            _timerComponent = timerComponent;
        }


        public void OnUpdate()
        {
            if (_timerComponent == null)
            {
                return;
            }

            if (_timerComponent.IsUpdating)
            {
                _timerComponent.RequestUpdate();
                return;
            }

            if (CheckExpiration())
            {
                RearmLoop();
                TriggerCallBack();
            }
        }

        internal bool CheckExpiration()
        {
            if (!_running || _pause || _timerComponent.ServerTimeStamp < _endTimeStamp)
            {
                return false;
            }

            if (_loop)
            {
                return true;
            }

            _timerComponent.JoinDeleteList(_onlyId, true);
            return false;
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
                OnUpdate();
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
            if (_pause)
            {
                _pauseStartedTimestamp = _startTimeStamp;
            }
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

        internal void RearmLoop()
        {
            if (!_running || !_loop)
            {
                return;
            }

            var now = _timerComponent.ServerTimeStamp;
            // 直接跳过已经错过的周期，避免极短周期在一帧内逐次累加。
            var elapsed = unchecked((ulong)now - (ulong)_endTimeStamp);
            var remaining = _totalTime - (long)(elapsed % (ulong)_totalTime);
            if (now > long.MaxValue - remaining)
            {
                _timerComponent.JoinDeleteList(_onlyId, false);
                return;
            }

            _endTimeStamp = now + remaining;
            _startTimeStamp = _endTimeStamp - _totalTime;
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
            _pauseStartedTimestamp = 0;
            _userData = null;
            _callBack = null;
            _running = false;
        }
    }
}
