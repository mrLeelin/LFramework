using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LFramework.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoWorldHelperAttribute : GameAttribute
    {
        public AutoWorldHelperAttribute(params System.Type[] bindWorldType)
        {
            BindWorldType = bindWorldType;
        }

        public System.Type[] BindWorldType { get; }
    }
}