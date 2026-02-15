using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 设置脚本宏定义任务
    /// 根据构建配置设置或移除脚本宏定义符号
    /// </summary>
    public class SetScriptingDefineSymbolsTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Set Scripting Define Symbols";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Set or remove scripting define symbols based on build settings";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要设置宏定义
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时设置宏定义
            return context.BuildSetting.buildType == BuildType.APP;
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
                var targetGroup = context.BuildTargetGroup;
                var buildSetting = context.BuildSetting;

                // 获取当前宏定义
                var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
                var defineList = new List<string>(defines.Split(';'));

                Debug.Log($"[SetScriptingDefineSymbolsTask] Current defines: {defines}");

                // Release 模式移除 ENABLE_LOG 宏
                if (buildSetting.isRelease)
                {
                    if (defineList.Remove("ENABLE_LOG"))
                    {
                        Debug.Log($"[SetScriptingDefineSymbolsTask] Removed ENABLE_LOG for Release build");
                    }
                }
                else
                {
                    // Debug 模式确保有 ENABLE_LOG 宏
                    if (!defineList.Contains("ENABLE_LOG"))
                    {
                        defineList.Add("ENABLE_LOG");
                        Debug.Log($"[SetScriptingDefineSymbolsTask] Added ENABLE_LOG for Debug build");
                    }
                }

                // 调用事件处理器,允许自定义宏定义
                var handlers = context.EventHandlers;
                if (handlers != null && handlers.Count > 0)
                {
                    Debug.Log($"[SetScriptingDefineSymbolsTask] Calling {handlers.Count} event handler(s) for custom defines...");
                    IBuildEventHandler.HandleList(handlers, (handler) =>
                    {
                        Debug.Log($"[SetScriptingDefineSymbolsTask] Calling {handler.GetType().Name}.OnProcessScriptingDefineSymbols");
                        handler.OnProcessScriptingDefineSymbols(buildSetting, defineList);
                    });
                }

                // 设置新的宏定义
                var newDefines = string.Join(";", defineList);
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), newDefines);

                Debug.Log($"[SetScriptingDefineSymbolsTask] New defines: {newDefines}");
                Debug.Log($"[SetScriptingDefineSymbolsTask] Scripting define symbols set successfully.");

                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Failed to set scripting define symbols: {ex.Message}");
            }
        }
    }
}
