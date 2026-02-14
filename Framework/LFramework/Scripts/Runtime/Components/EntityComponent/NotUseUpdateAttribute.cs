using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    
    /// <summary>
    /// 标识不使用Update
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NotUseUpdateAttribute : GameAttribute
    {
       
    }
}


