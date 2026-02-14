using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 自定义接口实现类
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface ,Inherited = false)]
    public class ManagerImplementorAttribute : Attribute
    {
      
        public ManagerImplementorAttribute(string implementorTypeName)
        {
            ImplementorTypeName = implementorTypeName;
        }

        /// <summary>
        /// 实现类的类型名称
        /// </summary>
        public string ImplementorTypeName { get; }
    }

}
