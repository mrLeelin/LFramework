using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract class NativeReference : IReference
    {
        private bool _isUse;

        private protected NativeReference()
        {
        }

        /// <summary>
        /// 是否被使用了
        /// </summary>
        public bool IsUse => _isUse;

        /// <summary>
        /// 设置使用了
        /// </summary>
        public void SetIsUse() => _isUse = true;


        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            if (!_isUse)
            {
                Log.Error("[NativeReference] The reference is already in use.");
            }
            _isUse = false;
            ReferencePool.Release(this);
        }

        public abstract void Clear();
    }


    public abstract class NativeReference<T> : NativeReference
        where T : NativeReference<T>, new()
    {
        /// <summary>
        /// 实例化
        /// </summary>
        /// <returns></returns>
        public static T Allocate()
        {
            T reference = ReferencePool.Acquire<T>();
            if (reference.IsUse)
            {
                Log.Error("[NativeReference] The reference is already in use.");
            }

            reference.SetIsUse();
            return reference;
        }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static T Allocate(Action<T> result)
        {
            var r = Allocate();
            result?.Invoke(r);
            return r;
        }
    }
}
