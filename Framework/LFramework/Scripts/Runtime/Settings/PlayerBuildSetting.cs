using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 按平台保存独立的 Dev 和 Release 构建选项。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBuildSetting", menuName = "LFramework/Settings/PlayerBuildSetting")]
    public sealed class PlayerBuildSetting : BaseSetting
    {
        // Unity BuildOptions.Development (1) | AllowDebugging (512)，仅作为新配置初始值。
        // 使用整数保存，避免 Runtime 程序集依赖 UnityEditor。
        private const int DefaultDevOptions = 1 | 512;

        [SerializeField, DrawWithUnity, FoldoutGroup("Android"), LabelText("Dev Options")]
        private BuildOptionsConfig android = new BuildOptionsConfig(DefaultDevOptions);

        [SerializeField, DrawWithUnity, FoldoutGroup("Android"), LabelText("Release Options")]
        private BuildOptionsConfig androidRelease = new BuildOptionsConfig();

        [SerializeField, DrawWithUnity, FoldoutGroup("iOS"), LabelText("Dev Options")]
        private BuildOptionsConfig ios = new BuildOptionsConfig(DefaultDevOptions);

        [SerializeField, DrawWithUnity, FoldoutGroup("iOS"), LabelText("Release Options")]
        private BuildOptionsConfig iosRelease = new BuildOptionsConfig();

        [SerializeField, DrawWithUnity, FoldoutGroup("Windows"), LabelText("Dev Options")]
        private BuildOptionsConfig windows = new BuildOptionsConfig(DefaultDevOptions);

        [SerializeField, DrawWithUnity, FoldoutGroup("Windows"), LabelText("Release Options")]
        private BuildOptionsConfig windowsRelease = new BuildOptionsConfig();

        /// <summary>
        /// 获取目标平台的 Dev 构建选项。
        /// </summary>
        public BuildOptionsConfig GetConfig(RuntimePlatform platform)
        {
            BuildOptionsConfig config = platform switch
            {
                RuntimePlatform.Android => android,
                RuntimePlatform.IPhonePlayer => ios,
                RuntimePlatform.WindowsPlayer => windows,
                _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported player build platform.")
            };
            return config ?? throw new InvalidOperationException($"PlayerBuildSetting: missing {platform} configuration.");
        }

        /// <summary>
        /// 获取目标平台的 Release 构建选项；默认全部关闭。
        /// </summary>
        public BuildOptionsConfig GetReleaseConfig(RuntimePlatform platform)
        {
            BuildOptionsConfig config = platform switch
            {
                RuntimePlatform.Android => androidRelease,
                RuntimePlatform.IPhonePlayer => iosRelease,
                RuntimePlatform.WindowsPlayer => windowsRelease,
                _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported player build platform.")
            };
            return config ?? throw new InvalidOperationException($"PlayerBuildSetting: missing {platform} Release configuration.");
        }

        /// <summary>
        /// 序列化 BuildOptions 位掩码；由 Editor 绘制原生枚举，避免运行时依赖 UnityEditor。
        /// </summary>
        [Serializable]
        public sealed class BuildOptionsConfig
        {
            [SerializeField] private int buildOptions;

            /// <summary>
            /// 设置初始选项；已保存的配置以及用户之后的修改不会被默认值覆盖。
            /// </summary>
            public BuildOptionsConfig(int options = 0)
            {
                buildOptions = options;
            }

            /// <summary>
            /// Unity BuildOptions 的序列化数值。
            /// </summary>
            public int OptionsValue => buildOptions;
        }
    }
}
