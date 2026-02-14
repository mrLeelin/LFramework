using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LFramework.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.U2D;
using UnityGameFramework.Editor.ResourceTools;
using Log = UnityGameFramework.Runtime.Log;


/// <summary>
/// Bat 文件 打包
/// </summary>
public static class BuildPackage
{
    private static readonly string[] ColumnSplitSeparator = new string[] { "\t", "\n", "|" };
    private const int ColumnCount = 4;

    private static AddressableAssetSettings _addressableAssetSettings;

    private const string SettingAssets
        = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
    
    private const string PhotonSettings = "Assets/Resources/PhotonServerSettings.asset";


    private const string BuildScript
        = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";

    private const string ProfileName = "Default";

    public static string iOSCSBundleName;

    /*
     
   /// <summary>
   /// Bat 文件打包
   /// </summary>
   /// <returns></returns>
   public static void Build()
   {
       var buildSetting = GetBuildSetting();
       if (buildSetting == null)
       {
           return;
       }

       ChangedDefaultConfig(buildSetting);
       ChangedPhotonPlugins(buildSetting);
       var platform = Enum.Parse<Platform>(buildSetting.platform);
       if (platform == Platform.Undefined)
       {
           Debug.LogError("==========[Platform is Undefined error.]==========");
       }

       GetSettingsObject(SettingAssets);
       SetProfile(ProfileName);
       var builderScript
           = AssetDatabase.LoadAssetAtPath<ScriptableObject>(BuildScript) as IDataBuilder;
       if (builderScript == null)
       {
           Debug.LogError(BuildScript + " couldn't be found or isn't a build script.");
           return;
       }
       SetBuilder(builderScript);
       if (!BuildAddressableContent())
       {
           Debug.LogWarning("Build resources failure. error");
           return;
       }

       Debug.Log("Build resources success.");

       if (!buildSetting.isBuildPackage)
       {
           return;
       }

       GenerateBuildVersionHelper.Generate(buildSetting.versionCode);
       BuildPlatformPackage(platform, buildSetting);
   }

   private static void BuildPlatformPackage(Platform platform, BuildSetting buildSetting)
   {
       //下划线替换成空格
       //PlayerSettings.productName = buildSetting.productName.Replace("_", " ");


       var packageDirPath =
           (buildSetting.packageOutputDirectory + "/" + buildSetting.packageFileName).Replace("\\", "/");
       if (Directory.Exists(packageDirPath))
       {
           Directory.Delete(packageDirPath, true);
       }

       Directory.CreateDirectory(packageDirPath);
       Debug.Log($"=======[Package Output Url : {packageDirPath}]=======");
       Debug.Log($"=======[Package Platform {platform}]======");
       switch (platform)
       {
           case Platform.Android:
               buildSetting.packageFileName += ".apk";
               break;
           case Platform.IOS:
               break;
           default:
               throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
       }

       var playerOptions = new BuildPlayerOptions
       {
           locationPathName = packageDirPath + "/" + buildSetting.packageFileName,
           scenes = GetBuildingScenes()
       };

       if (playerOptions.scenes.Length <= 0)
       {
           throw new Exception("打包失败.");
       }

       UnityEditor.SceneManagement.EditorSceneManager.OpenScene(playerOptions.scenes[0]);
       var isDebugMode = false; //Debug的界面 Release的功能//buildSetting.isDebugMode;
       EditorUserBuildSettings.allowDebugging = isDebugMode;
       EditorUserBuildSettings.development = isDebugMode;
       EditorUserBuildSettings.connectProfiler = isDebugMode;
       EditorUserBuildSettings.buildWithDeepProfilingSupport = isDebugMode;

       if (isDebugMode)
       {
           playerOptions.options = BuildOptions.Development
                                   | BuildOptions.EnableDeepProfilingSupport
                                   | BuildOptions.ConnectWithProfiler
                                   | BuildOptions.AllowDebugging
               ;
       }

       Debug.Log($"=======[Package Platform {platform}]======");
       try
       {
           PlayerSettings.applicationIdentifier = buildSetting.packageName;
           PlayerSettings.SplashScreen.show = false;
           PlayerSettings.bundleVersion = buildSetting.appVersion;

           Debug.Log("开始打包");
           Debug.Log("输出路径：" + playerOptions.locationPathName);
           switch (platform)
           {
               case Platform.Android:
                   BuildAndroid(isDebugMode, ref playerOptions, buildSetting.isBuildObb,
                       buildSetting.versionCode);
                   break;
               case Platform.IOS:
                   BuildiOS(isDebugMode, ref playerOptions,buildSetting, buildSetting.versionCode);
                   break;
               default:
                   throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
           }

           Debug.Log($"Package Platform Type is '{playerOptions.target}'");
           BuildPackageEnd(packageDirPath, playerOptions, buildSetting.isBuildObb, buildSetting.appVersion);
       }
       catch (Exception e)
       {
           Debug.LogError($"Package Error : {e}");
           return;
       }

       Debug.Log("Package Successful");
   }

   private static void BuildAndroid(bool isDebug, ref BuildPlayerOptions playerOptions, bool useObb,
       int internalResourceVersion)
   {
       Debug.Log($"=======[Start Build Android]=======");
       playerOptions.target = BuildTarget.Android;
       playerOptions.targetGroup = BuildTargetGroup.Android;

       PlayerSettings.Android.bundleVersionCode =
           internalResourceVersion; //int.Parse(versionCode.Replace(".", "").Trim());
       EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
       ===
       PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
           ScriptingImplementation.IL2CPP);
       PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
       ===

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
            isDebug ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = isDebug ? AndroidArchitecture.ARMv7 : AndroidArchitecture.ARM64;

        //EditorUserBuildSettings.androidCreateSymbolsZip = !isDebug;
        //PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        ====
        PlayerSettings.Android.keystorePass = "egame123";
        PlayerSettings.Android.keyaliasPass = "egame123";
        PlayerSettings.Android.keystoreName = "./keystore/user.keystore";
        PlayerSettings.Android.keyaliasName = "pixieisland";
        ===
        PlayerSettings.Android.splitApplicationBinary = useObb;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        // PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
    }

   
    private static void BuildiOS(bool isDebug, ref BuildPlayerOptions playerOptions, BuildSetting buildSetting,
        int internalResourceVersion)
    {
        Debug.Log($"=======[Start Build iOS]=======");
        playerOptions.target = BuildTarget.iOS;
        playerOptions.targetGroup = BuildTargetGroup.iOS;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,
            ScriptingImplementation.IL2CPP);

        PlayerSettings.iOS.backgroundModes = iOSBackgroundMode.None;
        PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Custom;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.buildNumber = internalResourceVersion.ToString(); //versionCode.Replace(".", "").Trim();
        PlayerSettings.iOS.targetOSVersionString = "12.0";
        PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.All;
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        PlayerSettings.iOS.iOSManualProvisioningProfileID = buildSetting.mobileProvisionUUid;
        PlayerSettings.iOS.appleDeveloperTeamID = buildSetting.appleDevelopTeamId;
        PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
        PlayerSettings.iOS.hideHomeButton = false;
        PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.All;
        iOSCSBundleName = buildSetting.productName;
    }


    private static void BuildPackageEnd(string packageDirPath, BuildPlayerOptions playerOptions, bool useObb,
        string versionCode)
    {
        BuildPipeline.BuildPlayer(playerOptions);

        var report = BuildPipeline.BuildPlayer(playerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.Log("Build failed");
            return;
        }

        var dirInfo = new DirectoryInfo(packageDirPath);
        if (!dirInfo.Exists)
        {
            return;
        }

        foreach (var dirFileInfo in dirInfo.GetFiles())
        {
            if (dirFileInfo == null)
            {
                continue;
            }

            if (dirFileInfo.Extension.Contains("zip"))
            {
                dirFileInfo.Delete();
                continue;
            }

            if (!useObb)
            {
                continue;
            }

            if (!dirFileInfo.Extension.Equals(".obb"))
            {
                continue;
            }

            System.Diagnostics.Debug.Assert(dirFileInfo.Directory != null, "dirFileInfo.Directory != null");
            var newName = $"{dirFileInfo.Directory.FullName}/main.{versionCode}.{Application.identifier}.obb";
            File.Move(dirFileInfo.FullName, newName);
            break;
        }

        foreach (var directoryInfo in dirInfo.GetDirectories())
        {
            if (directoryInfo.Name.Contains("_BackUpThisFolder_ButDontShipItWithYourGame"))
            {
                directoryInfo.Delete();
                continue;
            }

            if (directoryInfo.Name.Contains("_BurstDebugInformation_DoNotShip"))
            {
                directoryInfo.Delete();
                continue;
            }
        }
    }


    private static void SetProfile(string profile)
    {
        string profileId = _addressableAssetSettings.profileSettings.GetProfileId(profile);
        if (String.IsNullOrEmpty(profileId))
            Debug.LogWarning($"Couldn't find a profile named, {profile}, " +
                             $"using current profile instead.");
        else
            _addressableAssetSettings.activeProfileId = profileId;
    }

    private static void SetBuilder(IDataBuilder builder)
    {
        int index = _addressableAssetSettings.DataBuilders.IndexOf((ScriptableObject)builder);

        if (index > 0)
            _addressableAssetSettings.ActivePlayerDataBuilderIndex = index;
        else
            Debug.LogWarning($"{builder} must be added to the " +
                             $"DataBuilders list before it can be made " +
                             $"active. Using last run builder instead.");
    }

    private static bool BuildAddressableContent()
    {
        AddressableAssetSettings
            .BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }

        return success;
    }

    private static void GetSettingsObject(string settingsAsset)
    {
        // This step is optional, you can also use the default settings:
        //settings = AddressableAssetSettingsDefaultObject.Settings;

        _addressableAssetSettings
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset)
                as AddressableAssetSettings;

        if (_addressableAssetSettings == null)
        {
            Debug.LogError($"{settingsAsset} couldn't be found or isn't " +
                           $"a settings object.");
        }
    }


    private static void ChangedDefaultConfig(BuildSetting buildSetting)
    {
        var configPath = "Assets/MagicWarrior/_Resources/Configs/LocalConfig.txt";
        ParseData(buildSetting, File.ReadAllText(configPath), out var lines);
        File.WriteAllLines(configPath, lines);
    }

    private static void ChangedPhotonPlugins(BuildSetting buildScript)
    {
        var serverSettings = AssetDatabase.LoadAssetAtPath<PhotonServerSettings>(PhotonSettings);
        serverSettings.AppSettings.AppIdQuantum = buildScript.photonAppIdQuantum;
        serverSettings.AppSettings.FixedRegion = buildScript.photonFixedRegion;
        serverSettings.AppSettings.Server = buildScript.photonServer;
        EditorUtility.SetDirty(serverSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static void ParseData(BuildSetting buildSetting, string configString, out List<string> line)
    {
        line = new List<string>();
        try
        {
            int position = 0;
            string configLineString = null;
            while ((configLineString = configString.ReadLine(ref position)) != null)
            {
                if (configLineString[0] == '#')
                {
                    line.Add(configLineString);
                    continue;
                }

                string[] splitedLine = configLineString.Split('|', StringSplitOptions.None);
                if (splitedLine.Length != 3)
                {
                    Log.Warning("Can not parse config line string '{0}' which column count is invalid.",
                        configLineString);
                    return;
                }

                string configName = splitedLine[0];
                SetDefaultConfig(configName, buildSetting, splitedLine);
                StringBuilder stringBuilder = new StringBuilder();
                for (var index = 0; index < splitedLine.Length; index++)
                {
                    var lineSplit = splitedLine[index];
                    stringBuilder.Append(lineSplit);
                    if (index != splitedLine.Length - 1)
                    {
                        stringBuilder.Append('|');
                    }
                }

                line.Add((stringBuilder.ToString()));
            }

            return;
        }
        catch (Exception exception)
        {
            Log.Warning("Can not parse config string with exception '{0}'.", exception);
        }
    }

    private static void SetDefaultConfig(string configName, BuildSetting buildSetting, string[] splitedLine)
    {
        if (configName.Contains("Game.Debug"))
        {
            splitedLine[2] = (!buildSetting.isRelease).ToString();
        }
        else if (configName.Contains("Game.Server.Ip"))
        {
            splitedLine[2] = buildSetting.ip;
        }
        else if (configName.Contains("	Game.Server.Debug"))
        {
            splitedLine[2] = buildSetting.debugServer.ToString();
        }
        else if (configName.Contains("Game.Server.Port"))
        {
            splitedLine[2] = buildSetting.port.ToString();
        }else if (configName.Contains("Game.Runtime.AppVersion"))
        {
            splitedLine[2] = buildSetting.appVersion + "." + buildSetting.versionCode;
        }
    }

    private static string[] GetBuildingScenes()
    {
        
        var result = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path.Contains("GuiAnimation"))
            {
                continue;
            }

            result.Add(scene.path);
        }

        return result.ToArray();
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

            return buildSetting;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }
    */
}