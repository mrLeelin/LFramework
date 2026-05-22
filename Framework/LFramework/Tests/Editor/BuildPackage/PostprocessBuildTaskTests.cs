using System.Collections.Generic;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Pipeline;
using LFramework.Editor.Builder.Pipeline.Tasks;
using LFramework.Editor.Builder.PlatformConfig;
using NUnit.Framework;
using UnityEditor;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class PostprocessBuildTaskTests
    {
        [Test]
        public void Execute_ShouldPassPlayerOutputPathToPostprocessHandlers()
        {
            var buildSetting = new BuildSetting
            {
                buildType = BuildType.App,
                builderTarget = BuilderTarget.iOS,
                isRelease = true,
                appVersion = "1.0.0",
                versionCode = 1
            };
            var handler = new CapturingBuildEventHandler();
            var platformConfig = new StubPlatformConfig("Builds/IOS", "Builds/IOS/Project");
            var context = new BuildPipelineContext(
                buildSetting,
                new List<IBuildEventHandler> { handler },
                platformConfig)
            {
                OutputFolder = platformConfig.GetBuildFolderPath()
            };

            BuildTaskResult result = new PostprocessBuildTask().Execute(context);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(handler.PostprocessOutputFolder, Is.EqualTo("Builds/IOS/Project"));
        }

        [Test]
        public void Execute_ShouldPreferResolvedPlayerOutputPathFromContext()
        {
            var buildSetting = new BuildSetting
            {
                buildType = BuildType.App,
                builderTarget = BuilderTarget.iOS,
                isRelease = true,
                appVersion = "1.0.0",
                versionCode = 1
            };
            var handler = new CapturingBuildEventHandler();
            var platformConfig = new StubPlatformConfig("Builds/IOS", "Builds/IOS/Project");
            var context = new BuildPipelineContext(
                buildSetting,
                new List<IBuildEventHandler> { handler },
                platformConfig);

            context.SetCustomData(PostprocessBuildTask.PlayerOutputPathKey, "Builds/IOS/ProjectFromBuildPlayer");

            BuildTaskResult result = new PostprocessBuildTask().Execute(context);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(handler.PostprocessOutputFolder, Is.EqualTo("Builds/IOS/ProjectFromBuildPlayer"));
        }

        private sealed class CapturingBuildEventHandler : IBuildEventHandler
        {
            public string PostprocessOutputFolder { get; private set; }

            public void OnPreprocessBuildApp(BuildSetting mBuildData)
            {
            }

            public void OnProcessScriptingDefineSymbols(BuildSetting mBuildData, List<string> defineList)
            {
            }

            public void OnPreprocessBuildResources(BuildSetting buildSetting)
            {
            }

            public void OnPostprocessBuildResources(BuildSetting buildSetting)
            {
            }

            public void OnPostprocessBuildApp(BuildSetting mBuildData, string outPutFolder)
            {
                PostprocessOutputFolder = outPutFolder;
            }
        }

        private sealed class StubPlatformConfig : IPlatformConfig
        {
            private readonly string _buildFolderPath;
            private readonly string _outputPath;

            public StubPlatformConfig(string buildFolderPath, string outputPath)
            {
                _buildFolderPath = buildFolderPath;
                _outputPath = outputPath;
            }

            public BuildTarget GetBuildTarget()
            {
                return BuildTarget.iOS;
            }

            public BuildTargetGroup GetBuildTargetGroup()
            {
                return BuildTargetGroup.iOS;
            }

            public BuildPlayerOptions GetBuildPlayerOptions(BuildSetting buildSetting)
            {
                return new BuildPlayerOptions
                {
                    locationPathName = GetOutputPath(buildSetting)
                };
            }

            public void ConfigurePlatformSettings(BuildSetting buildSetting)
            {
            }

            public string GetOutputPath(BuildSetting buildSetting)
            {
                return _outputPath;
            }

            public string GetBuildFolderPath()
            {
                return _buildFolderPath;
            }
        }
    }
}
