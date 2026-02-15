//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 17:51:37
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
using UnityEngine.iOS;
using BuildType = LFramework.Editor.Builder.BuildType;

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_IOS_Release : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds/IOS";
    }

    protected override BuildTarget Target => BuildTarget.iOS;

    protected override BuildTargetGroup TargetGroup => BuildTargetGroup.iOS;

    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        var folder = GetFolderPath();
        options.locationPathName = folder + $"/Project";
        options.options = BuildOptions.None;

    }
    
    protected override void BuildInternal()
    {
        
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.backgroundModes = iOSBackgroundMode.None;
        PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Custom;
        PlayerSettings.iOS.buildNumber = MBuildData.versionCode.ToString();
        PlayerSettings.iOS.targetOSVersionString = "12.0";
        PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.All;
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        PlayerSettings.iOS.iOSManualProvisioningProfileID = ProjectBuilder_IOS_Data.MobileProvisionUUid;
        PlayerSettings.iOS.appleDeveloperTeamID = ProjectBuilder_IOS_Data.AppleDevelopTeamId;
        PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
        PlayerSettings.iOS.hideHomeButton = false;

        
        
        if (MBuildData.buildType == BuildType.ResourcesUpdate) return;
     
        BuildPlayerOptions options = GetBuildPlayerOptions();
        BuildPipeline.BuildPlayer(options);
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
        ProjectBuilder_IOS_Basic.OnPreprocessBuild(report, false);
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
        ProjectBuilder_IOS_Basic.OnPostprocessBuild(report, false);
    }

    public ProjectBuilder_IOS_Release(BuildSetting data) : base(data)
    {
    }
}