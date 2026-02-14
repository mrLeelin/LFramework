using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    
    public struct HotfixCodeResult 
    {
        public LoadAssemblyResultType ResultType;
        public string Message;
    }

    public enum LoadAssemblyResultType
    {
        Successful,
        LoadAotError,
        HotfixError,
    }

}
