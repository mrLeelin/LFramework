//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 17:05:54
//
//----------------------------------------------------------------

using System;
using System.IO;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Builder;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_AndroidAPK_Debug : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds/AndroidAPK";
    }

    protected override BuildTarget Target => BuildTarget.Android;

    protected override BuildTargetGroup TargetGroup => BuildTargetGroup.Android;


    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        var folder = GetFolderPath();
        var ext = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle ? "aab" : "apk";
        options.locationPathName = folder + $"/{MAppName}.{ext}";
        
        
         //这里会造成登录卡死 暂时屏蔽掉
         //把开发者模式打开
        //options.options = BuildOptions.Development;
        
        if (MBuildData.isDeepProfiler)
        {
            options.options |= BuildOptions.EnableDeepProfilingSupport;
        }
    }


    protected override void BuildBeforeInternal()
    {
        EditorUserBuildSettings.buildAppBundle = false;
    }

    protected override void BuildInternal()
    {
        BuildPlayerOptions options = GetBuildPlayerOptions();
        PlayerSettings.Android.bundleVersionCode = GetVersionCode();

        // PlayerSettings.companyName = "xxx";
        // PlayerSettings.productName = "xxx";
        //需要把签名文件放在Assets同一级的目录中
        
        //PlayerSettings.Android.splitApplicationBinary = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.buildAppBundle = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ScriptingImplementation.IL2CPP);
        
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        //放到外面去设置
        //PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        //PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        
        
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Path.GetFullPath(Path.Combine(Application.dataPath, "../BuildBat/keystore/partygo.keystore"));
        PlayerSettings.Android.keystorePass = "123456";
        PlayerSettings.Android.keyaliasName = "partygo";
        PlayerSettings.Android.keyaliasPass = "123456";

        // var folder = GetFolderPath();
        // DeleteDirectory(folder);
        // CreateDirectory(folder);

        BuildPipeline.BuildPlayer(options);

        // BackupBuildProject();
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
    }

    public ProjectBuilder_AndroidAPK_Debug(BuildSetting data) : base(data)
    {
    }
}