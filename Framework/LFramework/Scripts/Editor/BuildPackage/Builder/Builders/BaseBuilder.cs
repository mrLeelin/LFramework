using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Editor.Builder.Pipeline;
using LFramework.Editor.Builder.Pipeline.Pipelines;
using LFramework.Runtime;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildPipelineContext = LFramework.Editor.Builder.Pipeline.BuildPipelineContext;

namespace LFramework.Editor.Builder.Builder
{
    public abstract class BaseBuilder
    {
        protected readonly BuildSetting MBuildData;

        protected readonly string MAppName;

        public abstract string GetFolderPath();

        protected BaseBuilder(BuildSetting data)
        {
            MBuildData = data;
            var isReleaseName = MBuildData.isRelease ? "Release" : "Debug";
            MAppName = $"Build_{isReleaseName}_{MBuildData.appVersion}_{MBuildData.versionCode}";
            if (MBuildData.isDeepProfiler)
            {
                MAppName += "_DeepProfiler";
            }
        }

        protected abstract BuildTarget Target { get; }

        protected abstract BuildTargetGroup TargetGroup { get; }

        protected virtual string[] GetScenes()
        {
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e == null)
                {
                    continue;
                }

                if (e.enabled)
                {
                    names.Add(e.path);
                }
            }

            return names.ToArray();
        }

        protected BuildPlayerOptions GetBuildPlayerOptions()
        {
            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                target = Target,
                targetGroup = TargetGroup
            };
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(options.scenes[0]);
            GetBuildPlayerOptionsInternal(ref options);
            return options;
        }


        /// <summary>
        /// 构建方法 - 使用 Pipeline 架构
        /// 新架构: 通过 BuildPipeline 和 BuildTask 实现流式构建
        /// 旧架构: 原有的 protected 方法(BuildBeforeInternal, BuildInternal 等)仍然保留,通过反射调用
        /// </summary>
        /// <param name="handlers">构建事件处理器列表</param>
        public void Build(List<IBuildEventHandler> handlers)
        {
            Debug.Log("[BaseBuilder] Starting build with Pipeline architecture...");

            // 创建构建上下文
            var context = new BuildPipelineContext(MBuildData, handlers, this);
            context.BuildTarget = Target;
            context.BuildTargetGroup = TargetGroup;

            // 根据构建类型选择管线
            IBuildPipeline pipeline;
            if (MBuildData.buildType == BuildType.APP)
            {
                Debug.Log("[BaseBuilder] Using AppBuildPipeline for APP build");
                pipeline = new AppBuildPipeline();
            }
            else if (MBuildData.buildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("[BaseBuilder] Using ResourceBuildPipeline for resource update");
                pipeline = new ResourceBuildPipeline();
            }
            else
            {
                Debug.Log("[BaseBuilder] Using DefaultBuildPipeline");
                pipeline = new DefaultBuildPipeline();
            }

            // 执行管线
            bool success = pipeline.Execute(context);

            if (success)
            {
                Debug.Log("[BaseBuilder] Build completed successfully with Pipeline architecture.");
            }
            else
            {
                Debug.LogError("[BaseBuilder] Build failed with Pipeline architecture.");
                throw new Exception("Build failed. Check the logs for details.");
            }
        }

        /// <summary>
        /// 旧版构建方法 - 保留用于向后兼容
        /// 如果需要使用旧的构建流程,可以调用此方法
        /// </summary>
        /// <param name="handlers">构建事件处理器列表</param>
        [Obsolete("Use Build() method with Pipeline architecture instead. This method is kept for backward compatibility.")]
        public void BuildLegacy(List<IBuildEventHandler> handlers)
        {
            if (MBuildData.buildType == BuildType.APP)
            {
                CreateBuildDirectory();
                IBuildEventHandler.HandleList(handlers, (handler) =>
                {
                    handler.OnPreprocessBuildApp(MBuildData);
                });
                //宏设置
                AddOrRemoveSymbolsForGroup(handlers);
                BuildBeforeInternal();
            }

            BuildDll();
            var buildResourcesData = GetBuildResourceData();
            BuildResources(handlers,buildResourcesData);
            if (MBuildData.buildType == BuildType.APP)
            {
                BuildGameSetting(MBuildData, buildResourcesData);
                BuildInternal();
                IBuildEventHandler.HandleList(handlers, (handler) =>
                {
                    handler.OnPostprocessBuildApp(MBuildData);
                });
            }
        }

        /// <summary>
        /// 构建前处理 - 由 BuildBeforeTask 通过反射调用
        /// 子类可以重写此方法实现平台特定的构建前处理
        /// </summary>
        protected virtual void BuildBeforeInternal(){}

        /// <summary>
        /// 构建内部逻辑 - 由 BuildPlayerTask 通过反射调用
        /// 子类必须实现此方法执行实际的玩家构建
        /// </summary>
        protected abstract void BuildInternal();

        /// <summary>
        /// 获取构建玩家选项 - 由子类实现
        /// </summary>
        /// <param name="options">构建选项</param>
        protected abstract void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options);

        /// <summary>
        /// 预处理构建回调 - Unity 构建系统调用
        /// </summary>
        /// <param name="report">构建报告</param>
        public abstract void OnPreprocessBuild(BuildReport report);

        /// <summary>
        /// 后处理构建回调 - Unity 构建系统调用
        /// </summary>
        /// <param name="report">构建报告</param>
        public abstract void OnPostprocessBuild(BuildReport report);

        /// <summary>
        /// 构建 DLL - 由 BuildDllTask 通过反射调用
        /// 子类可以重写此方法实现 DLL 构建逻辑
        /// </summary>
        protected virtual void BuildDll()
        {
            if (!MBuildData.isBuildResources)
            {
                return;
            }
        }

        /// <summary>
        /// 构建资源 - 由 BuildResourcesTask 调用
        /// 此方法保留用于向后兼容,新架构中由 BuildResourcesTask 直接调用 BuildResourcesData.Build()
        /// </summary>
        /// <param name="handlers">事件处理器列表</param>
        /// <param name="buildResourcesData">构建资源数据</param>
        protected virtual void BuildResources(List<IBuildEventHandler> handlers, BuildResourcesData buildResourcesData)
        {
            if (!MBuildData.isBuildResources)
            {
                return;
            }
            IBuildEventHandler.HandleList(handlers, (handler) =>
            {
                handler.OnPreprocessBuildResources(buildResourcesData);
            });
            BuildResourcesData.Build(buildResourcesData);

            IBuildEventHandler.HandleList(handlers, (handler) =>
            {
                handler.OnPostprocessBuildResources(buildResourcesData);
            });
        }


        /// <summary>
        /// 获取构建资源数据 - 由 BuildResourcesTask 调用
        /// 此方法保留为 internal,供 Pipeline 任务访问
        /// </summary>
        /// <returns>构建资源数据</returns>
        internal BuildResourcesData GetBuildResourceData()
        {
            var buildResourcesData = new BuildResourcesData
            {
                BuilderTarget = MBuildData.builderTarget,
                IOSChannel = MBuildData.iosChannel,
                WindowsChannel = MBuildData.windowsChannel,
                AndroidChannel = MBuildData.androidChannel,
                IsResourcesBuildIn = MBuildData.isResourcesBuildIn,
                ResourcesVersion = MBuildData.resourcesVersion,
                BuildResourcesServerModel = (BuildResourcesServerModel)MBuildData.cdnType,
                BuildType = MBuildData.buildType,
                IsForceUpdate = MBuildData.isForceUpdate,
                IsBuildDll = MBuildData.isBuildDll,
                AppVersion = MBuildData.appVersion + "." + MBuildData.versionCode
            };
            return buildResourcesData;
        }

        /// <summary>
        /// 创建构建目录 - 由 CreateDirectoryTask 调用
        /// 此方法保留为 internal,供 Pipeline 任务访问
        /// </summary>
        internal void CreateBuildDirectory()
        {
            var folder = GetFolderPath();
            DeleteDirectory(folder);
            CreateDirectory(folder);
        }

        /// <summary>
        /// 添加或移除宏定义 - 由 SetScriptingDefineSymbolsTask 调用
        /// 此方法保留为 internal,供 Pipeline 任务访问
        /// </summary>
        /// <param name="handlers">事件处理器列表</param>
        internal void AddOrRemoveSymbolsForGroup(List<IBuildEventHandler> handlers)
        {

            //Release 移除 log 宏
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(TargetGroup));
            var defineList = new List<string>(defines.Split(';'));
            if (MBuildData.isRelease)
            {
                defineList.Remove("ENABLE_LOG"); // 移除指定的宏
            }
            IBuildEventHandler.HandleList(handlers, (handler) =>
            {
                handler.OnProcessScriptingDefineSymbols(MBuildData,defineList);
            });
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(TargetGroup), string.Join(";", defineList));

        }
        
        /// <summary>
        /// 创建文件夹根据有后缀的目录
        /// </summary>
        /// <param name="path">有后缀的目录</param>
        private void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void DeleteDirectory(string path)
        {
            //如果存在目录文件，就将其目录文件删除 
            if (Directory.Exists(path))
            {
                string[] _entries = Directory.GetFileSystemEntries(path);
                for (int e = 0; e < _entries.Length; e++)
                {
                    if (File.Exists(_entries[e]))
                    {
                        FileInfo file = new FileInfo(_entries[e]);
                        if (file.Attributes.ToString().IndexOf("ReadOnly") != -1)
                        {
                            file.Attributes = FileAttributes.Normal; //去掉文件属性 
                        }

                        file.Delete(); //直接删除其中的文件 
                    }
                    else
                    {
                        DeleteDirectory(_entries[e]); //递归删除 
                    }
                }

                //删除顶级文件夹
                DirectoryInfo DirInfo = new DirectoryInfo(path);

                if (DirInfo.Exists)
                {
                    //Debug.Log(path);
                    DirInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory; //去掉文件夹属性  
                    DirInfo.Delete(true);
                }
            }
        }


        /// <summary>
        /// 构建游戏设置 - 由 BuildGameSettingTask 调用
        /// 此方法保留为 internal,供 Pipeline 任务访问
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <param name="buildResourcesData">构建资源数据</param>
        internal void BuildGameSetting(BuildSetting buildSetting, BuildResourcesData buildResourcesData)
        {
            var allSettings = AssetUtilities.GetAllAssetsOfType<GameSetting>();
            var setting = allSettings.FirstOrDefault();
            setting.isRelease = buildSetting.isRelease;
            if (!string.IsNullOrEmpty(buildSetting.ip))
            {
                setting.versionUrl = buildSetting.ip;
            }

            setting.isResourcesBuildIn = buildSetting.isResourcesBuildIn;
            if (!setting.isResourcesBuildIn)
            {
                setting.appVersion = buildSetting.appVersion + "." + buildSetting.versionCode;
                setting.resourceVersion = buildSetting.resourcesVersion;
                setting.cdnType = buildSetting.cdnType;
            }

            setting.channel = GetBuildChannel(buildSetting);
            EditorUtility.SetDirty(setting);
        }

        private string GetBuildChannel(BuildSetting buildSetting)
        {
            switch (buildSetting.builderTarget)
            {
                case BuilderTarget.Windows:
                    return buildSetting.windowsChannel.ToString();
                case BuilderTarget.Android:
                    return buildSetting.androidChannel.ToString();
                case BuilderTarget.iOS:
                    return buildSetting.iosChannel.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected int GetVersionCode()
        {
            var version = MBuildData.appVersion;
            var versionCode = MBuildData.versionCode;
            // 去掉字符串中的点
            string cleanedString = version.Replace(".", "");
            // 组合字符串并转换为整数
            int result = int.Parse(cleanedString + versionCode.ToString());
            Debug.Log($"[BaseBuilder] The Version Code is '{result}'");
            return result;
        }
    }
}