using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LFramework.Editor.Builder.Pipeline
{
    /// <summary>
    /// 构建管线运行器
    /// 负责执行管线中的所有任务,处理错误和日志
    /// </summary>
    public class BuildPipelineRunner
    {
        /// <summary>
        /// 执行管线
        /// </summary>
        /// <param name="pipeline">要执行的管线</param>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示执行成功,false 表示执行失败</returns>
        public static bool Run(IBuildPipeline pipeline, BuildPipelineContext context)
        {
            if (pipeline == null)
            {
                Debug.LogError("[BuildPipelineRunner] Pipeline is null.");
                return false;
            }

            if (context == null)
            {
                Debug.LogError("[BuildPipelineRunner] Context is null.");
                return false;
            }

            Debug.Log($"[BuildPipelineRunner] ========== Start Pipeline: {pipeline.PipelineName} ==========");
            Debug.Log($"[BuildPipelineRunner] Description: {pipeline.Description}");

            var stopwatch = Stopwatch.StartNew();
            var tasks = pipeline.GetTasks();

            if (tasks == null || tasks.Count == 0)
            {
                Debug.LogWarning("[BuildPipelineRunner] No tasks in pipeline.");
                return true;
            }

            Debug.Log($"[BuildPipelineRunner] Total tasks: {tasks.Count}");

            var executedTasks = new List<BuildTaskResult>();
            var success = true;

            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null)
                {
                    Debug.LogError($"[BuildPipelineRunner] Task at index {i} is null.");
                    success = false;
                    break;
                }

                Debug.Log($"[BuildPipelineRunner] ---------- Task {i + 1}/{tasks.Count}: {task.TaskName} ----------");
                Debug.Log($"[BuildPipelineRunner] Description: {task.Description}");

                BuildTaskResult result;

                try
                {
                    // 检查任务是否可以执行
                    if (!task.CanExecute(context))
                    {
                        result = BuildTaskResult.CreateSkipped(task.TaskName);
                        Debug.Log($"[BuildPipelineRunner] Task skipped: {task.TaskName}");
                        executedTasks.Add(result);
                        continue;
                    }

                    // 执行任务
                    var taskStopwatch = Stopwatch.StartNew();
                    result = task.Execute(context);
                    taskStopwatch.Stop();

                    if (result == null)
                    {
                        result = BuildTaskResult.CreateFailed(task.TaskName, "Task returned null result.");
                    }

                    executedTasks.Add(result);

                    // 记录任务执行结果
                    if (result.IsSuccess)
                    {
                        Debug.Log($"[BuildPipelineRunner] Task succeeded: {task.TaskName} (Time: {taskStopwatch.ElapsedMilliseconds}ms)");
                    }
                    else if (result.IsSkipped)
                    {
                        Debug.Log($"[BuildPipelineRunner] Task skipped: {task.TaskName}");
                    }
                    else if (result.IsFailed)
                    {
                        Debug.LogError($"[BuildPipelineRunner] Task failed: {task.TaskName}");
                        Debug.LogError($"[BuildPipelineRunner] Error: {result.ErrorMessage}");
                        success = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    result = BuildTaskResult.CreateFailed(task.TaskName, ex.ToString());
                    executedTasks.Add(result);
                    Debug.LogError($"[BuildPipelineRunner] Task exception: {task.TaskName}");
                    Debug.LogException(ex);
                    success = false;
                    break;
                }
            }

            stopwatch.Stop();

            // 输出执行摘要
            Debug.Log($"[BuildPipelineRunner] ========== Pipeline Summary ==========");
            Debug.Log($"[BuildPipelineRunner] Pipeline: {pipeline.PipelineName}");
            Debug.Log($"[BuildPipelineRunner] Total time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F2}s)");
            Debug.Log($"[BuildPipelineRunner] Total tasks: {tasks.Count}");
            Debug.Log($"[BuildPipelineRunner] Executed tasks: {executedTasks.Count}");

            var successCount = 0;
            var failedCount = 0;
            var skippedCount = 0;

            foreach (var result in executedTasks)
            {
                if (result.IsSuccess) successCount++;
                else if (result.IsFailed) failedCount++;
                else if (result.IsSkipped) skippedCount++;
            }

            Debug.Log($"[BuildPipelineRunner] Success: {successCount}, Failed: {failedCount}, Skipped: {skippedCount}");

            if (success)
            {
                Debug.Log($"[BuildPipelineRunner] ========== Pipeline Completed Successfully ==========");
            }
            else
            {
                Debug.LogError($"[BuildPipelineRunner] ========== Pipeline Failed ==========");
            }

            return success;
        }

        /// <summary>
        /// 执行单个任务(用于测试)
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <param name="context">构建上下文</param>
        /// <returns>任务执行结果</returns>
        public static BuildTaskResult RunTask(IBuildTask task, BuildPipelineContext context)
        {
            if (task == null)
            {
                Debug.LogError("[BuildPipelineRunner] Task is null.");
                return BuildTaskResult.CreateFailed("Unknown", "Task is null.");
            }

            if (context == null)
            {
                Debug.LogError("[BuildPipelineRunner] Context is null.");
                return BuildTaskResult.CreateFailed(task.TaskName, "Context is null.");
            }

            Debug.Log($"[BuildPipelineRunner] Executing task: {task.TaskName}");

            try
            {
                if (!task.CanExecute(context))
                {
                    return BuildTaskResult.CreateSkipped(task.TaskName);
                }

                var result = task.Execute(context);
                return result ?? BuildTaskResult.CreateFailed(task.TaskName, "Task returned null result.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return BuildTaskResult.CreateFailed(task.TaskName, ex.ToString());
            }
        }
    }
}
