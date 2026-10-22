using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface ISingleton : IDisposable
    {
        void Register();
        void Destroy();
        bool IsDisposed();
    }

    // 注册前检查静态实例，避免失败回滚影响已经生效的直接注册。
    internal interface ISingletonRegistrationState
    {
        ISingleton RegisteredInstance { get; }
    }

    public abstract class Singleton<T> : ISingleton, ISingletonRegistrationState where T : Singleton<T>, new()
    {
        private bool _isDisposed;


        private static T _sInstance;


        public static T Instance => _sInstance;

        ISingleton ISingletonRegistrationState.RegisteredInstance => _sInstance;


        public virtual void Register()
        {
            if (Instance != null)
            {
                throw new Exception($"singleton register twice! {typeof(T).Name}");
            }

            _sInstance = (T)this;
        }

        protected void RollbackRegistration()
        {
            if (ReferenceEquals(_sInstance, this))
            {
                _sInstance = null;
            }
        }


        public void Destroy()
        {
            if (this._isDisposed)
            {
                return;
            }

            this._isDisposed = true;

            RollbackRegistration();
            Dispose();
        }

        public bool IsDisposed() => _isDisposed;


        public virtual void Dispose()
        {
        }
    }


    public abstract class SingletonMonoBehaviour<T> : UnityEngine.MonoBehaviour, ISingleton, ISingletonRegistrationState where T : SingletonMonoBehaviour<T>, new()
    {
        private bool _isDisposed;

        private static T _sInstance;


        public static T Instance => _sInstance;

        ISingleton ISingletonRegistrationState.RegisteredInstance => _sInstance == null ? null : _sInstance;
        
        
        public void Register()
        {
            if (Instance != null)
            {
                throw new Exception($"singleton register twice! {typeof(T).Name}");
            }

            _sInstance = (T)this;
        }
        
        public virtual void Dispose()
        {
         
        }

     

        public void Destroy()
        {
            if (this._isDisposed)
            {
                return;
            }

            this._isDisposed = true;

            if (ReferenceEquals(_sInstance, this))
            {
                _sInstance = null;
            }

            Dispose();
        }

        public bool IsDisposed()
        {
            return _isDisposed;
        }
    }
}
