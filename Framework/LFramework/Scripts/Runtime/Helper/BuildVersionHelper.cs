using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public class BuildVersionHelper : Version.IVersionHelper
    {
        public string GameVersion => Application.dataPath;
        public int InternalGameVersion => 0;
        
    }
}
