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

    public abstract class Singleton<T> : ISingleton where T : Singleton<T>, new()
    {
        private bool _isDisposed;


        private static T _sInstance;


        public static T Instance => _sInstance;


        public virtual void Register()
        {
            if (Instance != null)
            {
                throw new Exception($"singleton register twice! {typeof(T).Name}");
            }

            _sInstance = (T)this;
        }


        public void Destroy()
        {
            if (this._isDisposed)
            {
                return;
            }

            this._isDisposed = true;

            T t = _sInstance;
            _sInstance = null;
            t.Dispose();
        }

        public bool IsDisposed() => _isDisposed;


        public virtual void Dispose()
        {
        }
    }


    public abstract class SingletonMonoBehaviour<T> : UnityEngine.MonoBehaviour, ISingleton where T : SingletonMonoBehaviour<T>, new()
    {
        private bool _isDisposed;

        private static T _sInstance;


        public static T Instance => _sInstance;
        
        
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

            T t = _sInstance;
            _sInstance = null;
            t.Dispose();
        }

        public bool IsDisposed()
        {
            return _isDisposed;
        }
    }
}