using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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


        public void Build(List<IBuildEventHandler> handlers)
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

        protected virtual void BuildBeforeInternal(){}
        protected abstract void BuildInternal();
        protected abstract void GetBuildPlayerOptionsInternal(ref BuildPlayerOptions options);
        public abstract void OnPreprocessBuild(BuildReport report);
        public abstract void OnPostprocessBuild(BuildReport report);

        protected virtual void BuildDll()
        {
            if (!MBuildData.isBuildResources)
            {
                return;
            }
        }

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


        private BuildResourcesData GetBuildResourceData()
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

        private void CreateBuildDirectory()
        {
            var folder = GetFolderPath();
            DeleteDirectory(folder);
            CreateDirectory(folder);
        }

        private void AddOrRemoveSymbolsForGroup(List<IBuildEventHandler> handlers)
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


        private void BuildGameSetting(BuildSetting buildSetting, BuildResourcesData buildResourcesData)
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