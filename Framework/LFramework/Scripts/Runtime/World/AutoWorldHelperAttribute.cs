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

        public System.Type RegisterConditionType { get; set; }

        public bool CanRegister()
        {
            if (RegisterConditionType != null)
            {
                if (!typeof(IAutoWorldHelperRegisterCondition).IsAssignableFrom(RegisterConditionType))
                {
                    throw new InvalidOperationException(
                        $"The register condition type '{RegisterConditionType.FullName}' must implement '{typeof(IAutoWorldHelperRegisterCondition).FullName}'.");
                }

                var condition = (IAutoWorldHelperRegisterCondition)Activator.CreateInstance(RegisterConditionType, true);
                return condition.CanRegister();
            }

            return true;
        }
    }

    public interface IAutoWorldHelperRegisterCondition
    {
        bool CanRegister();
    }
}
