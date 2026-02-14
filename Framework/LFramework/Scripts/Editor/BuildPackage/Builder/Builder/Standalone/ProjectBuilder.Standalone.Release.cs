//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 17:16:08
//
//----------------------------------------------------------------

using System;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Builder;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildType = LFramework.Editor.Builder.BuildType;

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_Standalone_Release : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds";
    }

    protected override BuildTarget Target
    {
        get { return BuildTarget.StandaloneWindows; }
    }

    protected override BuildTargetGroup TargetGroup
    {
        get { return BuildTargetGroup.Standalone; }
    }

    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
        options.locationPathName = Application.dataPath +
                                   $"/../Builds/Window_Release_{MBuildData.appVersion}_{timeInfo}/{MAppName}.exe";
        options.options = BuildOptions.None;
    }


    protected override void BuildInternal()
    {
        // CreateDirectory(GetFolderPath());

        
        PlayerSettings.macOS.buildNumber = MBuildData.versionCode.ToString();

        if (MBuildData.buildType == BuildType.ResourcesUpdate) return;
        BuildPlayerOptions options = GetBuildPlayerOptions();
        BuildPipeline.BuildPlayer(options);
    }

    public override void OnPreprocessBuild(BuildReport report)
    {
    }

    public override void OnPostprocessBuild(BuildReport report)
    {
    }

    public ProjectBuilder_Standalone_Release(BuildSetting data) : base(data)
    {
    }
}