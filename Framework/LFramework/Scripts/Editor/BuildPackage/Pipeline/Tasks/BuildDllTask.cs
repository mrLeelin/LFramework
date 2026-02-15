using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建 DLL 任务
    /// 调用 BaseBuilder 的 BuildDll 方法构建热更新 DLL
    /// </summary>
    public class BuildDllTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Build DLL";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Build hot-fix DLL files";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在需要构建资源时执行
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在需要构建资源时执行
            return context.BuildSetting.isBuildResources;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>任务执行结果</returns>
        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildDllTask] Building DLL files...");

                // 通过反射调用 BaseBuilder 的 BuildDll 方法
                var builder = context.Builder;
                var method = builder.GetType().GetMethod("BuildDll",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (method != null)
                {
                    method.Invoke(builder, null);
                    Debug.Log($"[BuildDllTask] DLL files built successfully.");
                }
                else
                {
                    Debug.LogWarning($"[BuildDllTask] BuildDll method not found, skipping.");
                }

                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build DLL failed: {ex.Message}");
            }
        }
    }
}
