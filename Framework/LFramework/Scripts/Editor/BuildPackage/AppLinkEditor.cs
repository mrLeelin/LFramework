using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif


public class AppLinkEditor
{
#if UNITY_IOS 
    [PostProcessBuild(200)]
    public static void AddIOSDeeplink(BuildTarget buildTarget, string path)
    {
    }
#endif
}
