using System;
using UnityEditor;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 在构建期间同步 HybridCLR 使用的 Development 状态，并在成功或失败后恢复。
    /// </summary>
    internal sealed class PlayerDevelopmentBuildScope : IDisposable
    {
        private readonly bool previousDevelopment;

        internal PlayerDevelopmentBuildScope(BuildOptions options)
        {
            previousDevelopment = EditorUserBuildSettings.development;
            EditorUserBuildSettings.development = (options & BuildOptions.Development) != 0;
        }

        /// <summary>
        /// 恢复进入构建之前的编辑器状态。
        /// </summary>
        public void Dispose()
        {
            EditorUserBuildSettings.development = previousDevelopment;
        }
    }
}
