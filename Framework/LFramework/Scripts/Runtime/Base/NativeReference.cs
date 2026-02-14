using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract class NativeReference<T> : IReference
        where T : NativeReference<T>, new()
    {
        private bool _isUse;

        public static T Allocate()
        {
            T reference = ReferencePool.Acquire<T>();
            if (reference._isUse)
            {
                Log.Error("The reference is already in use.");
            }

            reference._isUse = true;
            return reference;
        }

        public static T Allocate(Action<T> result)
        {
            var r = Allocate();
            result?.Invoke(r);
            return r;
        }

        public abstract void Clear();

        public void Release()
        {
            _isUse = false;
            ReferencePool.Release(this);
        }
    }
}