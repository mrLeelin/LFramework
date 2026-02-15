//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 19:08:15
//
//----------------------------------------------------------------

using System;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Builder;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildType = LFramework.Editor.Builder.BuildType;

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_Android_Release : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds";
    }

    protected override BuildTarget Target => BuildTarget.Android;

    protected override BuildTargetGroup TargetGroup => BuildTargetGroup.Android;

    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
#if CHANNEL_CN
        options.locationPathName = Application.dataPath + $"/../Builds/AndroidExport";
#else
        options.locationPathName = Application.dataPath + $"/../Builds/Android_Release_{MBuildData.appVersion}_{timeInfo}/{MAppName}";
#endif
        options.options = BuildOptions.AcceptExternalModificationsToPlayer;
    }
    

    protected override void BuildInternal()
    {
        //PlayerSettings.Android.splitApplicationBinary = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.buildAppBundle = MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        
        PlayerSettings.Android.bundleVersionCode = MBuildData.versionCode;

        if (MBuildData.buildType == BuildType.ResourcesUpdate)
        {
            return;
        }
        BuildPlayerOptions options = GetBuildPlayerOptions();
        
        BuildPipeline.BuildPlayer(options);
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
    }

    public ProjectBuilder_Android_Release(BuildSetting data) : base(data)
    {
    }
}