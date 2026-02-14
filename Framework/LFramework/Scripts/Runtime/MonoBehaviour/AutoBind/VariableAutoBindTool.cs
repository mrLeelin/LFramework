using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LFramework.Runtime
{
    public class VariableAutoBindTool : UnityEngine.MonoBehaviour
    {

        [SerializeField] private VariableArray variableArray;

        /// <summary>
        /// 可能特别费性能
        /// </summary>
        /// <param name="monoBehaviour"></param>
        public static void Inject(UnityEngine.MonoBehaviour monoBehaviour)
        {
            var bindTool = monoBehaviour.GetComponent<VariableAutoBindTool>();
            if (bindTool == null)
            {
                return;
            }

            var types = monoBehaviour.GetType()
                .GetFields(BindingFlags.Instance 
                           | BindingFlags.Public 
                           | BindingFlags.NonPublic);
            foreach (var fieldInfo in types)
            {
                var value = bindTool.variableArray.Get(fieldInfo.Name);
                if (value == null)
                {
                    continue;
                }

                fieldInfo.SetValue(monoBehaviour, value);
            }
        }
    }
}

