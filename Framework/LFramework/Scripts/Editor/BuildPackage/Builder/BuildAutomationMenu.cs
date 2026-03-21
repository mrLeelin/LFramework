using LFramework.Runtime;
using UnityEditor;

namespace LFramework.Editor.Builder
{
    public static class BuildAutomationMenu
    {
        [MenuItem("Tools/LFramework/Build Android Debug APK")]
        public static void BuildAndroidDebugApk()
        {
            BuildAndroidDebugApkBatch();
        }

        public static void BuildAndroidDebugApkBatch()
        {
            var buildSetting = new BuildSetting
            {
                builderTarget = BuilderTarget.Android,
                androidChannel = BuildAndroidChannel.GoogleStore,
                buildAndroidAppType = BuildAndroidAppType.APK,
                buildType = BuildType.APP,
                isDeepProfiler = false,
                isRelease = false,
                appVersion = PlayerSettings.bundleVersion,
                versionCode = 1,
                isBuildResources = true,
                isResourcesBuildIn = true,
                resourcesVersion = PlayerSettings.bundleVersion,
                cdnType = CdnType.Debug,
                isForceUpdate = false,
                isBuildDll = true,
            };

            BuildOrchestrator.BuildFromSetting(buildSetting);
        }
    }
}
