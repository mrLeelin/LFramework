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
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                Log.Fatal($"StaticMethod: Type '{typeName}' not found in assembly '{assembly.FullName}'");
                return;
            }

            this._methodInfo = type.GetMethod(methodName);
            if (this._methodInfo == null)
            {
                Log.Fatal($"StaticMethod: Method '{methodName}' not found in type '{typeName}'");
                return;
            }

            this._param = new object[this._methodInfo.GetParameters().Length];
        }

        public void Run()
        {
            this._methodInfo?.Invoke(null, _param);
        }

        public void Run(object param1)
        {
            if (this._methodInfo == null) return;
            this._param[0] = param1;
            this._methodInfo.Invoke(null, _param);
        }

        public void Run(object param1, object param2)
        {
            if (this._methodInfo == null) return;
            this._param[0] = param1;
            this._param[1] = param2;
            this._methodInfo.Invoke(null, _param);
        }

        public void Run(object param1, object param2, object param3)
        {
            if (this._methodInfo == null) return;
            this._param[0] = param1;
            this._param[1] = param2;
            this._param[2] = param3;
            this._methodInfo.Invoke(null, _param);
        }
    }
}