using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 用于忽视 Provider引用
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class IgnoreInterfaceAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class BindInterfaceAttribute : Attribute
    {
        
        public Type InterfaceType { get; private set; }

        public BindInterfaceAttribute(Type interfaceType)
        {
            InterfaceType = interfaceType;
            if (!interfaceType.IsInterface)
            {
                Log.Fatal("BindInterfaceAttribute InterfaceType is not interface");
            }
        }
    }
}

