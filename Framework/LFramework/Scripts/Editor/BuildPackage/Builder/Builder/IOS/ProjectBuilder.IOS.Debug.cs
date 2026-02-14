//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 16:30:34
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
using UnityGameFramework.Runtime;
using BuildType = LFramework.Editor.Builder.BuildType;

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_IOS_Debug : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds/IOS";
    }

    protected override BuildTarget Target
    {
        get { return BuildTarget.iOS; }
    }

    protected override BuildTargetGroup TargetGroup
    {
        get { return BuildTargetGroup.iOS; }
    }

    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        var folder = GetFolderPath();
        options.locationPathName = folder + $"/Project";
        //options.options = BuildOptions.Development;
        if (MBuildData.isDeepProfiler)
        {
            options.options |= BuildOptions.EnableDeepProfilingSupport;
        }
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
      

        if (MBuildData.buildType == BuildType.ResourcesUpdate)
        {
            return;
        }
      
        BuildPlayerOptions options = GetBuildPlayerOptions();
        try
        {
            BuildPipeline.BuildPlayer(options);
        }
        catch (Exception e)
        {
            Log.Error("Build IOS Error: {0}", e);
        }
     
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
        ProjectBuilder_IOS_Basic.OnPreprocessBuild(report, true);
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
        ProjectBuilder_IOS_Basic.OnPostprocessBuild(report, true);
    }

    public ProjectBuilder_IOS_Debug(BuildSetting data) : base(data)
    {
    }
}