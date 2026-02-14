using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Method
{
    public class StaticMethod : IStaticMethod
    {
        private readonly MethodInfo _methodInfo;

        private readonly object[] _param;

        public StaticMethod(Assembly assembly, string typeName, string methodName)
        {
            this._methodInfo = assembly.GetType(typeName).GetMethod(methodName);
            var memberInfo = this._methodInfo;
            if (memberInfo != null)
            {
                this._param = new object[memberInfo.GetParameters().Length];
            }
            else
            {
                Log.Fatal($"The method '{typeName}' method name '{methodName}' is not found.");
            }
        }

        public void Run()
        {
            this._methodInfo.Invoke(null, _param);
        }

        public void Run(object param1)
        {
            this._param[0] = param1;
            this._methodInfo.Invoke(null, _param);
        }

        public void Run(object param1, object param2)
        {
            this._param[0] = param1;
            this._param[1] = param2;
            this._methodInfo.Invoke(null, _param);
        }

        public void Run(object param1, object param2, object param3)
        {
            this._param[0] = param1;
            this._param[1] = param2;
            this._param[2] = param3;
            this._methodInfo.Invoke(null, _param);
        }
    }
}