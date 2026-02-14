using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class Define
    {
        public const string BuildOutputDir = "./Temp/Bin/Debug/Assembly-CSharp/Player/";

#if UNITY_EDITOR
		public static bool IsEditor = true;
#else
        public static bool IsEditor = false;
#endif
        
#if !ENABLE_CODES
		public static bool EnableCodes = true;
#else
        public static bool EnableCodes = false;
#endif
        
        
    }
}