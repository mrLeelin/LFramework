using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.Builder;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildType = LFramework.Editor.Builder.BuildType;
using Log = UnityGameFramework.Runtime.Log;

public class ProjectBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string PhotonSettings = "Assets/Resources/PhotonServerSettings.asset";


    private static BaseBuilder _currentBuilder;
    private DateTime _startTime;

    private static readonly Dictionary<BuilderSourceType, Type> SourceTypeDict =
        new Dictionary<BuilderSourceType, Type>();

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (_currentBuilder == null)
        {
            return;
        }

        _startTime = DateTime.Now;
        Debug.Log($"打包开始：{_startTime.ToString("HH:mm:ss")} outpPath = {report.summary.outputPath}");
        _currentBuilder.OnPreprocessBuild(report);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (_currentBuilder == null)
        {
            return;
        }

        if (report.summary.result == BuildResult.Cancelled || report.summary.result == BuildResult.Failed)
        {
            Debug.LogError("打包失败");
            return;
        }

        _currentBuilder.OnPostprocessBuild(report);
        var endTime = DateTime.Now;
        var deltaTime = endTime - _startTime;
        var hours = deltaTime.Hours.ToString("00");
        var minutes = deltaTime.Minutes.ToString("00");
        var seconds = deltaTime.Seconds.ToString("00");
        Debug.LogFormat("打包结束：{0}", endTime.ToString("HH:mm:ss"));
        Debug.LogFormat("打包用时：{0}:{1}:{2}", hours, minutes, seconds);
    }


    /// <summary>
    /// The  jenkins call this method to build the project
    /// </summary>
    public static void Build()
    {
        var buildSetting = GetBuildSetting();
        if (buildSetting == null)
        {
            return;
        }

        Build(buildSetting);
    }

    public static void Build(BuildSetting data)
    {
        if (data == null)
        {
            return;
        }

        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.bundleVersion = data.appVersion;

        /*
        var isDebugMode = !data.isRelease;
        EditorUserBuildSettings.allowDebugging = isDebugMode;
        EditorUserBuildSettings.development = isDebugMode;
        EditorUserBuildSettings.connectProfiler = isDebugMode;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = isDebugMode;
        */

        //如果是打的资源包
        if (data.buildType == BuildType.ResourcesUpdate)
        {
            data.isBuildResources = true;
            data.isResourcesBuildIn = false;
        }

        Debug.Log(data.ToString());
        List<IBuildEventHandler> handlers = new List<IBuildEventHandler>();
        AppendBuilderDict(ref handlers);

        var targetGroup = BuildTargetGroup.Unknown;
        switch (data.builderTarget)
        {
            case BuilderTarget.Windows:
                targetGroup = BuildTargetGroup.Standalone;
                if (data.isRelease)
                {
                    _currentBuilder = CreateBuilder(BuilderSourceType.WindowsRelease,
                        data,
                        new ProjectBuilder_Standalone_Release(data));
                }
                else
                {
                    _currentBuilder = CreateBuilder(BuilderSourceType.WindowsDebug,
                        data,
                        new ProjectBuilder_Standalone_Debug(data));
                }

                break;
            case BuilderTarget.Android:
                targetGroup = BuildTargetGroup.Android;
                EditorUserBuildSettings.exportAsGoogleAndroidProject =
                    data.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject;
                if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
                {
                    if (data.isRelease)
                    {
                        _currentBuilder = new ProjectBuilder_Android_Release(data);
                    }
                    else
                    {
                        _currentBuilder = new ProjectBuilder_Android_Debug(data);
                    }
                }
                else
                {
                    if (data.isRelease)
                    {
                        _currentBuilder = CreateBuilder(BuilderSourceType.AndroidRelease,
                            data,
                            new ProjectBuilder_AndroidAPK_Release(data));
                    }
                    else
                    {
                        _currentBuilder = CreateBuilder(BuilderSourceType.AndroidDebug,
                            data,
                            new ProjectBuilder_AndroidAPK_Debug(data));
                    }
                }

                break;
            case BuilderTarget.iOS:
                targetGroup = BuildTargetGroup.iOS;
                if (data.isRelease)
                {
                    _currentBuilder = CreateBuilder(BuilderSourceType.iOSRelease,
                        data,
                        new ProjectBuilder_IOS_Release(data));
                }
                else
                {
                    _currentBuilder = CreateBuilder(BuilderSourceType.iOSDebug,
                        data,
                        new ProjectBuilder_IOS_Debug(data));
                }

                break;
        }

        var defineSymbols =
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
        Debug.Log($"Build, target = {targetGroup} defineSymbols = {defineSymbols}");
        _currentBuilder.Build(handlers);
    }

    private static BaseBuilder CreateBuilder(BuilderSourceType sourceType, BuildSetting buildData,
        BaseBuilder defaultValue)
    {
        if (!SourceTypeDict.TryGetValue(sourceType, out var type))
        {
            return defaultValue;
        }

        if (!typeof(BaseBuilder).IsAssignableFrom(type))
        {
            Log.Error("The type is not BaseBuilder! type = {0}", type.FullName);
            return defaultValue;
        }

        var result = Activator.CreateInstance(type, buildData) as BaseBuilder;
        return result;
    }

    /// <summary>
    /// 添加BuilderSourceType的字典
    /// </summary>
    private static void AppendBuilderDict(ref List<IBuildEventHandler> handlers)
    {
        SourceTypeDict.Clear();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.FullName.Contains("Editor"))
            {
                continue;
            }

            foreach (var type in assembly.GetTypes())
            {
                TryAppendSource(type);
                TryAppendBuildHandler(type, ref handlers);
            }
        }
    }

    /// <summary>
    /// 尝试添加BuilderSourceType
    /// </summary>
    /// <param name="type"></param>
    private static void TryAppendSource(Type type)
    {
        var attribute = type.GetCustomAttribute<BuilderSourceAttribute>();
        if (attribute == null)
        {
            return;
        }

        if (!SourceTypeDict.TryAdd(attribute.BuilderSourceType, type))
        {
            Log.Fatal("The BuilderSourceType is already exist! BuilderSourceType = {0}",
                attribute.BuilderSourceType);
            return;
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