using LFramework.Editor.Builder;
using LFramework.Editor.Builder.PlatformConfig;
using UnityEditor;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    internal static class TestPlatformConfigRegistryProviders
    {
        public static bool HighPriorityEnabled { get; set; }
        public static bool LowPriorityEnabled { get; set; }
        public static bool ConflictProviderAEnabled { get; set; }
        public static bool ConflictProviderBEnabled { get; set; }
        public static bool NullReturningProviderEnabled { get; set; }

        public static void Reset()
        {
            HighPriorityEnabled = false;
            LowPriorityEnabled = false;
            ConflictProviderAEnabled = false;
            ConflictProviderBEnabled = false;
            NullReturningProviderEnabled = false;
        }
    }

    public sealed class HighPriorityProvider : IPlatformConfigRegistryProvider
    {
        public string ProviderName => nameof(HighPriorityProvider);
        public int Priority => 100;
        public bool IsActive => TestPlatformConfigRegistryProviders.HighPriorityEnabled;

        public bool Supports(BuilderTarget builderTarget)
        {
            return TestPlatformConfigRegistryProviders.HighPriorityEnabled && builderTarget == BuilderTarget.Android;
        }

        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            return new HighPriorityAndroidPlatformConfig(buildSetting);
        }
    }

    public sealed class LowPriorityProvider : IPlatformConfigRegistryProvider
    {
        public string ProviderName => nameof(LowPriorityProvider);
        public int Priority => 10;
        public bool IsActive => TestPlatformConfigRegistryProviders.LowPriorityEnabled;

        public bool Supports(BuilderTarget builderTarget)
        {
            return TestPlatformConfigRegistryProviders.LowPriorityEnabled && builderTarget == BuilderTarget.Android;
        }

        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            return new LowPriorityAndroidPlatformConfig(buildSetting);
        }
    }

    public sealed class ConflictProviderA : IPlatformConfigRegistryProvider
    {
        public string ProviderName => nameof(ConflictProviderA);
        public int Priority => 200;
        public bool IsActive => TestPlatformConfigRegistryProviders.ConflictProviderAEnabled;

        public bool Supports(BuilderTarget builderTarget)
        {
            return TestPlatformConfigRegistryProviders.ConflictProviderAEnabled && builderTarget == BuilderTarget.Android;
        }

        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            return new HighPriorityAndroidPlatformConfig(buildSetting);
        }
    }

    public sealed class ConflictProviderB : IPlatformConfigRegistryProvider
    {
        public string ProviderName => nameof(ConflictProviderB);
        public int Priority => 200;
        public bool IsActive => TestPlatformConfigRegistryProviders.ConflictProviderBEnabled;

        public bool Supports(BuilderTarget builderTarget)
        {
            return TestPlatformConfigRegistryProviders.ConflictProviderBEnabled && builderTarget == BuilderTarget.Android;
        }

        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            return new HighPriorityAndroidPlatformConfig(buildSetting);
        }
    }

    public sealed class NullReturningProvider : IPlatformConfigRegistryProvider
    {
        public string ProviderName => nameof(NullReturningProvider);
        public int Priority => 300;
        public bool IsActive => TestPlatformConfigRegistryProviders.NullReturningProviderEnabled;

        public bool Supports(BuilderTarget builderTarget)
        {
            return TestPlatformConfigRegistryProviders.NullReturningProviderEnabled &&
                   builderTarget == BuilderTarget.Android;
        }

        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            return null;
        }
    }

    public sealed class HighPriorityAndroidPlatformConfig : IPlatformConfig
    {
        public HighPriorityAndroidPlatformConfig(BuildSetting buildSetting)
        {
        }

        public BuildTarget GetBuildTarget()
        {
            return BuildTarget.Android;
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return BuildTargetGroup.Android;
        }

        public BuildPlayerOptions GetBuildPlayerOptions(BuildSetting buildSetting)
        {
            return new BuildPlayerOptions();
        }

        public void ConfigurePlatformSettings(BuildSetting buildSetting)
        {
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            return "HighPriority";
        }

        public string GetBuildFolderPath()
        {
            return "HighPriority";
        }
    }

    public sealed class LowPriorityAndroidPlatformConfig : IPlatformConfig
    {
        public LowPriorityAndroidPlatformConfig(BuildSetting buildSetting)
        {
        }

        public BuildTarget GetBuildTarget()
        {
            return BuildTarget.Android;
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return BuildTargetGroup.Android;
        }

        public BuildPlayerOptions GetBuildPlayerOptions(BuildSetting buildSetting)
        {
            return new BuildPlayerOptions();
        }

        public void ConfigurePlatformSettings(BuildSetting buildSetting)
        {
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            return "LowPriority";
        }

        public string GetBuildFolderPath()
        {
            return "LowPriority";
        }
    }
}
