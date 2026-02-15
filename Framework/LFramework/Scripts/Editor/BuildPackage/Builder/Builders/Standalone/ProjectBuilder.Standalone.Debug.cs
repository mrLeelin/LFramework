//-----------------------------------------------------------------
//
//              Maggic @  2021-07-06 17:05:54
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
public class ProjectBuilder_Standalone_Debug : BaseBuilder
{
    public override string GetFolderPath()
    {
        return Application.dataPath + "/../Builds";
    }

    protected override BuildTarget Target => BuildTarget.StandaloneWindows64;

    protected override BuildTargetGroup TargetGroup => BuildTargetGroup.Standalone;

    protected override void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options)
    {
        string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
        options.locationPathName = Application.dataPath +
                                   $"/../Builds/Window_Debug_{MBuildData.appVersion}_{timeInfo}/{MAppName}.exe";
        options.options = BuildOptions.AllowDebugging | BuildOptions.Development | BuildOptions.ConnectWithProfiler |
                          BuildOptions.WaitForPlayerConnection;

        if (MBuildData.isDeepProfiler)
        {
            options.options |= BuildOptions.EnableDeepProfilingSupport;
        }
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

    public ProjectBuilder_Standalone_Debug(BuildSetting data) : base(data)
    {
    }
}