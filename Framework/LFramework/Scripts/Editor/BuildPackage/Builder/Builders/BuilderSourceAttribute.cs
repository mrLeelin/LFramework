using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Editor
{
    public enum BuilderSourceType
    {
        AndroidDebug,
        AndroidRelease,
        iOSDebug,
        iOSRelease,
        WindowsRelease,
        WindowsDebug,
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
    public class BuilderSourceAttribute : System.Attribute
    {
        public BuilderSourceAttribute(BuilderSourceType sourceType)
        {
            BuilderSourceType = sourceType;
        }

        public BuilderSourceType BuilderSourceType { get; }
    }
}