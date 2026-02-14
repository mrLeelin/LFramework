using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class BuildVersionHelperExtensions 
    {

        public static string GetVersion(this Version.IVersionHelper versionHelper)
        {
            return $"{versionHelper.GameVersion}.{versionHelper.InternalGameVersion}";
        }
    }

}
