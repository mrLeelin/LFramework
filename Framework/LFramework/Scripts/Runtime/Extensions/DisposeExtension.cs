using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class DisposeExtension 
    {

        public static T AddTo<T>(this T disposable, NoParamEntityLogic logic)
            where T : IDisposable
        {
            if (logic == null)
            {
                disposable.Dispose();
                return disposable;
            }

            return AddTo(disposable,logic.CompositeDisposable);
        }
        public static T AddTo<T>(this T disposable, ViewBehaviour viewBehaviour)
            where T : IDisposable
        {
            if (viewBehaviour == null)
            {
                disposable.Dispose();
                return disposable;
            }

            return AddTo(disposable,viewBehaviour.CompositeDisposable);
        }
        
        private static T AddTo<T>(this T disposable, ICollection<IDisposable> container)
            where T : IDisposable
        {
            if (disposable == null) throw new ArgumentNullException("disposable");
            if (container == null) throw new ArgumentNullException("container");

            container.Add(disposable);

            return disposable;
        }
        
    }
 
}

