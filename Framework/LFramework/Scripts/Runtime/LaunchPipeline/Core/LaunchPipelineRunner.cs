using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动管线运行器。
    /// 负责按顺序异步执行管线中的所有任务，处理错误和日志。
    /// 镜像 Editor 侧的 <c>BuildPipelineRunner</c>，使用 UniTask 实现异步执行，支持 Zenject 依赖注入。
    /// </summary>
    public class LaunchPipelineRunner
    {
        /// <summary>
        /// 异步执行管线（镜像 <c>BuildPipelineRunner.Run</c>）。
        /// </summary>
        /// <param name="pipeline">要执行的启动管线。</param>
        /// <param name="context">启动管线上下文。</param>
        /// <param name="diContainer">可选的 Zenject 依赖注入容器，用于在执行任务前注入依赖。</param>
        /// <returns><c>true</c> 表示管线执行成功，<c>false</c> 表示执行失败。</returns>
        public static async UniTask<bool> RunAsync(ILaunchPipeline pipeline, LaunchContext context, DiContainer diContainer = null)
        {
            if (pipeline == null)
            {
                Log.Error("[LaunchPipelineRunner] Pipeline is null.");
                return false;
            }

            if (context == null)
            {
                Log.Error("[LaunchPipelineRunner] Context is null.");
                return false;
            }

            Log.Info("[LaunchPipelineRunner] ========== 开始执行管线: {0} ==========", pipeline.PipelineName);
            Log.Info("[LaunchPipelineRunner] 描述: {0}", pipeline.Description);

            var stopwatch = Stopwatch.StartNew();
            var tasks = pipeline.GetTasks();

            if (tasks == null || tasks.Count == 0)
            {
                Log.Warning("[LaunchPipelineRunner] 管线中没有任务。");
                return true;
            }

            Log.Info("[LaunchPipelineRunner] 任务总数: {0}", tasks.Count);

            var executedTasks = new List<LaunchTaskResult>();
            var success = true;

            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null)
                {
                    Log.Error("[LaunchPipelineRunner] 任务索引 {0} 为 null。", i);
                    success = false;
                    break;
                }

                Log.Info("[LaunchPipelineRunner] ---------- 任务 {0}/{1}: {2} ----------", i + 1, tasks.Count, task.TaskName);

                LaunchTaskResult result;

                try
                {
                    // 如果提供了 DI 容器，在执行前注入依赖
                    if (diContainer != null)
                    {
                        diContainer.Inject(task);
                    }

                    // 检查任务是否可以执行
                    if (!task.CanExecute(context))
                    {
                        result = LaunchTaskResult.CreateSkipped(task.TaskName);
                        Log.Info("[LaunchPipelineRunner] 任务已跳过: {0}", task.TaskName);
                        executedTasks.Add(result);
                        continue;
                    }

                    // 异步执行任务
                    var taskStopwatch = Stopwatch.StartNew();
                    result = await task.ExecuteAsync(context);
                    taskStopwatch.Stop();

                    if (result == null)
                    {
                        result = LaunchTaskResult.CreateFailed(task.TaskName, "Task returned null result.");
                    }

                    executedTasks.Add(result);

                    // 记录任务执行结果
                    if (result.IsSuccess)
                    {
                        Log.Info("[LaunchPipelineRunner] 任务执行成功: {0} (耗时: {1}ms)", task.TaskName, taskStopwatch.ElapsedMilliseconds);
                    }
                    else if (result.IsSkipped)
                    {
                        Log.Info("[LaunchPipelineRunner] 任务已跳过: {0}", task.TaskName);
                    }
                    else if (result.IsFailed)
                    {
                        Log.Error("[LaunchPipelineRunner] 任务执行失败: {0}", task.TaskName);
                        Log.Error("[LaunchPipelineRunner] 错误: {0}", result.ErrorMessage);
                        success = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    result = LaunchTaskResult.CreateFailed(task.TaskName, ex.ToString());
                    executedTasks.Add(result);
                    Log.Error("[LaunchPipelineRunner] 任务异常: {0}", task.TaskName);
                    Log.Error("[LaunchPipelineRunner] 异常信息: {0}", ex);
                    success = false;
                    break;
                }
            }

            stopwatch.Stop();

            // 输出执行摘要
            Log.Info("[LaunchPipelineRunner] ========== 管线执行摘要 ==========");
            Log.Info("[LaunchPipelineRunner] 管线: {0}", pipeline.PipelineName);
            Log.Info("[LaunchPipelineRunner] 总耗时: {0}ms ({1:F2}s)", stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
            Log.Info("[LaunchPipelineRunner] 任务总数: {0}", tasks.Count);
            Log.Info("[LaunchPipelineRunner] 已执行任务: {0}", executedTasks.Count);

            var successCount = 0;
            var failedCount = 0;
            var skippedCount = 0;

            foreach (var result in executedTasks)
            {
                if (result.IsSuccess) successCount++;
                else if (result.IsFailed) failedCount++;
                else if (result.IsSkipped) skippedCount++;
            }

            Log.Info("[LaunchPipelineRunner] 成功: {0}, 失败: {1}, 跳过: {2}", successCount, failedCount, skippedCount);

            if (success)
            {
                Log.Info("[LaunchPipelineRunner] ========== 管线执行完成 ==========");
            }
            else
            {
                Log.Error("[LaunchPipelineRunner] ========== 管线执行失败 ==========");
            }

            return success;
        }

        /// <summary>
        /// 异步执行单个任务（镜像 <c>BuildPipelineRunner.RunTask</c>，便于测试）。
        /// </summary>
        /// <param name="task">要执行的启动任务。</param>
        /// <param name="context">启动管线上下文。</param>
        /// <param name="diContainer">可选的 Zenject 依赖注入容器，用于在执行任务前注入依赖。</param>
        /// <returns>任务执行结果。</returns>
        public static async UniTask<LaunchTaskResult> RunTaskAsync(ILaunchTask task, LaunchContext context, DiContainer diContainer = null)
        {
            if (task == null)
            {
                Log.Error("[LaunchPipelineRunner] Task is null.");
                return LaunchTaskResult.CreateFailed("Unknown", "Task is null.");
            }

            if (context == null)
            {
                Log.Error("[LaunchPipelineRunner] Context is null.");
                return LaunchTaskResult.CreateFailed(task.TaskName, "Context is null.");
            }

            Log.Info("[LaunchPipelineRunner] 执行任务: {0}", task.TaskName);

            try
            {
                // 如果提供了 DI 容器，在执行前注入依赖
                if (diContainer != null)
                {
                    diContainer.Inject(task);
                }

                if (!task.CanExecute(context))
                {
                    return LaunchTaskResult.CreateSkipped(task.TaskName);
                }

                var result = await task.ExecuteAsync(context);
                return result ?? LaunchTaskResult.CreateFailed(task.TaskName, "Task returned null result.");
            }
            catch (Exception ex)
            {
                Log.Error("[LaunchPipelineRunner] 任务异常: {0}", task.TaskName);
                Log.Error("[LaunchPipelineRunner] 异常信息: {0}", ex);
                return LaunchTaskResult.CreateFailed(task.TaskName, ex.ToString());
            }
        }
    }
}
