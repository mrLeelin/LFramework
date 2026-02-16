using System;
using System.Collections.Generic;
using LFramework.Editor.Builder.PlatformConfig;
using LFramework.Editor.Builder.Pipeline;
using LFramework.Editor.Builder.Pipeline.Pipelines;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder
{
    /// <summary>
    /// 构建编排器
    /// 作为构建系统的入口点，协调整个构建流程
    /// 替代原有的 BaseBuilder 和 ProjectBuilder 系统
    /// </summary>
    public class BuildOrchestrator
    {
        private readonly BuildSetting _buildSetting;
        private readonly List<IBuildEventHandler> _eventHandlers;

        public BuildOrchestrator(BuildSetting buildSetting, List<IBuildEventHandler> eventHandlers = null)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
            _eventHandlers = eventHandlers ?? new List<IBuildEventHandler>();
        }

        /// <summary>
        /// 执行构建
        /// </summary>
        public void Build()
        {
            Debug.Log("[BuildOrchestrator] Starting build process...");
            Debug.Log($"[BuildOrchestrator] Build Type: {_buildSetting.buildType}");
            Debug.Log($"[BuildOrchestrator] Target Platform: {_buildSetting.builderTarget}");
            Debug.Log($"[BuildOrchestrator] Release Mode: {_buildSetting.isRelease}");

            try
            {
                // 创建平台配置
                var platformConfig = PlatformConfigFactory.CreateConfig(
                    _buildSetting.builderTarget,
                    _buildSetting);

                Debug.Log($"[BuildOrchestrator] Created platform config: {platformConfig.GetType().Name}");

                // 创建构建上下文
                var context = new BuildPipelineContext(_buildSetting, _eventHandlers, platformConfig);

                // 根据构建类型选择管线
                IBuildPipeline pipeline = SelectPipeline(_buildSetting.buildType);

                Debug.Log($"[BuildOrchestrator] Selected pipeline: {pipeline.PipelineName}");

                // 执行管线
                bool success = pipeline.Execute(context);

                if (success)
                {
                    Debug.Log("[BuildOrchestrator] Build completed successfully!");
                }
                else
                {
                    throw new Exception("Build failed. Check the logs for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildOrchestrator] Build failed: {ex.Message}");
                throw;
            }
        }

        private IBuildPipeline SelectPipeline(BuildType buildType)
        {
            switch (buildType)
            {
                case BuildType.APP:
                    return new AppBuildPipeline();

                case BuildType.ResourcesUpdate:
                    return new ResourceBuildPipeline();
                
            }

            throw new Exception($"The SelectPipeline error. '{buildType}'");
        }

        /// <summary>
        /// 静态构建方法（供 Jenkins 等 CI 系统调用）
        /// </summary>
        public static void BuildFromCommandLine()
        {
            var buildSetting = GetBuildSetting();
            if (buildSetting == null)
            {
                Debug.LogError("[BuildOrchestrator] Failed to get build setting from command line.");
                return;
            }
            var handlers = new List<IBuildEventHandler>();
            AppendBuilderDict(ref handlers);
            var orchestrator = new BuildOrchestrator(buildSetting,handlers);
            orchestrator.Build();
        }

        public static void BuildFromSetting(BuildSetting buildSetting)
        {
            var handlers = new List<IBuildEventHandler>();
            AppendBuilderDict(ref handlers);
            var orchestrator = new BuildOrchestrator(buildSetting,handlers);
            orchestrator.Build();
        }

        
        /// <summary>
        /// 添加BuilderSourceType的字典
        /// </summary>
        private static void AppendBuilderDict(ref List<IBuildEventHandler> handlers)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.FullName.Contains("Editor"))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    TryAppendBuildHandler(type, ref handlers);
                }
            }
        }
        

        private static void TryAppendBuildHandler(Type type, ref List<IBuildEventHandler> handlers)
        {
            if (!typeof(IBuildEventHandler).IsAssignableFrom(type))
            {
                return;
            }

            if (type.IsAbstract || type.IsInterface)
            {
                return;
            }

            handlers.Add((IBuildEventHandler)Activator.CreateInstance(type));
        }
        
        /// <summary>
        /// 获取打包配置文件
        /// </summary>
        /// <returns></returns>
        private static BuildSetting GetBuildSetting()
        {
            Debug.Log("==========[Start parse build setting]==========");
            var parameters = Environment.GetCommandLineArgs();
            var buildSettingJson = "";
            foreach (var parameter in parameters)
            {
                if (parameter.StartsWith("BuildSetting"))
                {
                    var tempParam = parameter.Split(new string[] { "=" },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (tempParam.Length == 2)
                    {
                        Debug.Log("TempParam: " + parameter);
                        buildSettingJson = tempParam[1].Trim();
                    }

                    break;
                }
            }

            if (string.IsNullOrEmpty(buildSettingJson))
            {
                Debug.LogError($"==========[BuildSettingJson is null error. json '{buildSettingJson}']==========");
                return null;
            }

            Debug.Log("origin json: " + buildSettingJson);
            buildSettingJson = buildSettingJson.TrimStart('"').TrimEnd('"').Replace("\\", "/");
            Debug.Log($"======[BuildingSetting: {buildSettingJson}]======");
            try
            {
                var buildSetting = JsonUtility.FromJson<BuildSetting>(buildSettingJson);
                if (buildSetting == null)
                {
                    Debug.LogError($"==========[BuildSettingJson parse error. json '{buildSettingJson}']==========");
                    return null;
                }

                Debug.Log("Unity BuildSetting : " + buildSetting);
                return buildSetting;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }
}
