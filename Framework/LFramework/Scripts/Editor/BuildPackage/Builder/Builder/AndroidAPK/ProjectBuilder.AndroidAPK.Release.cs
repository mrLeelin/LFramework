//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 17:16:08
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
public class ProjectBuilder_AndroidAPK_Release : BaseBuilder
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
        options.options = BuildOptions.None;
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
        PlayerSettings.Android.keystoreName = Path.GetFullPath(Path.Combine(Application.dataPath, "../BuildBat/keystore/partygo.keystore"));
        PlayerSettings.Android.keystorePass = "123456";
        PlayerSettings.Android.keyaliasName = "partygo";
        PlayerSettings.Android.keyaliasPass = "123456";
        
        EditorUserBuildSettings.buildAppBundle = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        //放到外面去设置
        //PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        //PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        PlayerSettings.Android.useCustomKeystore = true;
        
    

        //PlayerSettings.Android. = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        BuildPipeline.BuildPlayer(options);
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
    }

    public ProjectBuilder_AndroidAPK_Release(BuildSetting data) : base(data)
    {
    }
}