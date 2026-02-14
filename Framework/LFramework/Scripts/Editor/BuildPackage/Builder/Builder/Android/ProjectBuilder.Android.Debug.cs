using System;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Builder;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildType = LFramework.Editor.Builder.BuildType;

namespace LFramework.Editor
{
    public class ProjectBuilder_Android_Debug : BaseBuilder
    {
        public ProjectBuilder_Android_Debug(BuildSetting data) : base(data)
        {
        }

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
            options.locationPathName = Application.dataPath +
                                       $"/../Builds/Android_Debug_{MBuildData.appVersion}_{timeInfo}/{MAppName}";
#endif
            options.options = BuildOptions.Development | BuildOptions.AcceptExternalModificationsToPlayer;
            if (MBuildData.isDeepProfiler)
            {
                options.options |= BuildOptions.EnableDeepProfilingSupport;
            }

        }
        

        protected override void BuildInternal()
        {
            PlayerSettings.Android.bundleVersionCode = MBuildData.versionCode;
            /*
            PlayerSettings.Android.splitApplicationBinary =
                MBuildData.buildAndroidAppType == BuildAndroidAppType.AppBundle;
                */
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
    }
}