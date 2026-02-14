using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class NotUseUpdateHelper
    {
        private static readonly Dictionary<Type, bool> CacheUseUpdateTypes = new Dictionary<Type, bool>();

        /// <summary>
        /// 是否可以使用Update
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsCanUseUpdate(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (CacheUseUpdateTypes.TryGetValue(type,out var result))
            {
                return result;
            }
            result =  type.GetCustomAttribute(typeof(NotUseUpdateAttribute),false) == null;
            CacheUseUpdateTypes.Add(type,result);
            return result;
        }
        
    }

}
