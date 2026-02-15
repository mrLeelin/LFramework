//-----------------------------------------------------------------
//
//              Maggic @  2021-07-09 13:43:26
//
//----------------------------------------------------------------

using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// <summary>
/// 
/// </summary>
public class ProjectBuilder_IOS_Basic
{
    protected static string ProjectEntitlementsPath = "";
    protected static string RelativeEntitlementPath = "game.entitlements";

    public static void OnPreprocessBuild(BuildReport report, bool isDevelopment)
    {
    }


    public static void OnPostprocessBuild(BuildReport report, bool isDevelopment)
    {
#if UNITY_IOS

        string path = report.summary.outputPath;
        Debug.Log($"outputPath = {path}");
        string projectPath = PBXProject.GetPBXProjectPath(path);
        Debug.Log($"projectPath = {projectPath}");
        ProjectEntitlementsPath = Path.Combine(path, RelativeEntitlementPath);

        // 确保 entitlements 文件存在
        if (!File.Exists(ProjectEntitlementsPath))
        {
            var emptyEntitlement = new PlistDocument();
            emptyEntitlement.WriteToFile(ProjectEntitlementsPath);
        }

        //项目编辑 开始 ------------------------------------------------------------------------>
        PBXProject pbxProject = new PBXProject();
        pbxProject.ReadFromFile(projectPath);
        OnGenerateProjectFile(pbxProject, isDevelopment);
        pbxProject.WriteToFile(projectPath);
        OnGenerateEntitlement(pbxProject, projectPath, isDevelopment);
     
        
        
        //plist 编辑 ------------------------------------------------------------------------>
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        OnGeneratePlistFile(plist.root, isDevelopment);
        SetURLSchemeByPlist(plist.root);
        SetFacebookMessenger(plist.root);
        File.WriteAllText(plistPath, plist.WriteToString());
        //其他sdk
        //Sign in with Apple
    

#endif
    }
#if UNITY_IOS

    private static void AddLibToProject(PBXProject inst, string targetGuid, string lib)
    {
        string fileGuid = inst.AddFile("usr/lib/" + lib, "Frameworks/" + lib, PBXSourceTree.Sdk);
        inst.AddFileToBuild(targetGuid, fileGuid);
    }


    /// <summary>
    /// 项目编辑
    /// </summary>
    /// <param name="pbxProject"></param>
    /// <param name="isDevelopment"></param>
    private static void OnGenerateProjectFile(PBXProject pbxProject, bool isDevelopment)
    {
        //项目编辑 开始 ------------------------------------------------------------------------>

        //unity framework 编辑
        string unityFrameWork = pbxProject.GetUnityFrameworkTargetGuid();
        
        
        pbxProject.SetBuildProperty(unityFrameWork, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
        pbxProject.SetBuildProperty(unityFrameWork, "ENABLE_BITCODE", "NO");
        pbxProject.AddFrameworkToProject(unityFrameWork, "GameKit.framework", false);
        pbxProject.AddFrameworkToProject(unityFrameWork, "AppTrackingTransparency.framework", false);

        //unity-iphone 操作
        string unityMainGuid = pbxProject.GetUnityMainTargetGuid();
        pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_ENTITLEMENTS", RelativeEntitlementPath);
        pbxProject.SetBuildProperty(unityMainGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_IDENTITY", ProjectBuilder_IOS_Data.CODE_SIGN_IDENTITY);
        //团队
        pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_STYLE", "Manual");
        pbxProject.SetBuildProperty(unityMainGuid, "DEVELOPMENT_TEAM", ProjectBuilder_IOS_Data.AppleDevelopTeamId);
        //必填-新规
        pbxProject.SetBuildProperty(unityMainGuid, "ARCHS", "arm64");
        // 关闭 BitCode 有些SDK 不支持 BitCode 只能关闭
        pbxProject.SetBuildProperty(unityMainGuid, "ENABLE_BITCODE", "NO");
        

        /*
        简介
         pbxProject.SetBuildProperty(unityMainGuid, "PROVISIONING_PROFILE_SPECIFIER",
             isDevelopment ? ProjectBuilder_IOS_Data.Profiles_Development : ProjectBuilder_IOS_Data.Profiles_AppStore);
         //证书
         pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_IDENTITY",
             isDevelopment ? ProjectBuilder_IOS_Data.Sign_Development : ProjectBuilder_IOS_Data.Sign_AppStore);
         pbxProject.SetBuildProperty(unityMainGuid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]",
             isDevelopment ? ProjectBuilder_IOS_Data.Sign_Development : ProjectBuilder_IOS_Data.Sign_AppStore);
        */

      
        pbxProject.AddFrameworkToProject(unityMainGuid, "GameKit.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "SystemConfiguration.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "CoreTelephony.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "Security.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "JavaScriptCore.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "Photos.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "iAd.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "WebKit.framework", true);
        pbxProject.AddFrameworkToProject(unityMainGuid, "AppTrackingTransparency.framework", true);
        //Firebase Cloud Messaging
        pbxProject.AddFrameworkToProject(unityMainGuid, "UserNotifications.framework", true);
        

        AddLibToProject(pbxProject, unityMainGuid, "libz.tbd");
        AddLibToProject(pbxProject, unityMainGuid, "libsqlite3.tbd");
        AddLibToProject(pbxProject, unityMainGuid, "libc++.tbd");

       

        OnInfoPlist(pbxProject, isDevelopment);
        //添加Flag
        //pbxProject.AddBuildProperty(unityMainGuid, "OTHER_LDFLAGS", "-ObjC");
        // pbxProject.AddFileToBuildWithFlags(unityMainGuid,"","-licucore");
    }

    private static void OnGenerateEntitlement(PBXProject pbxProject,string projectPath,bool isDevelopment)
    {
        var manager =
            new ProjectCapabilityManager(projectPath, RelativeEntitlementPath, null, pbxProject.GetUnityMainTargetGuid());
        
        manager.AddKeychainSharing(new []
        {
            $"$(AppIdentifierPrefix)" + Application.identifier,
        });
        manager.AddPushNotifications(isDevelopment);
        manager.AddGameCenter();
        manager.AddInAppPurchase();
        manager.AddSignInWithApple();
        /*
        manager.AddKeychainSharing(null);
        manager.AddGameCenter();
        manager.AddInAppPurchase();
        manager.AddPushNotifications(isDevelopment);
        */
        
        
        //设置苹果登录
        /*
        AddSignInWithAppleWithCompatibility(manager, pbxProject);
        */
        manager.WriteToFile();
    }
    private static void OnGenerateEntitlement(PBXProject pbxProject, bool isDevelopment)
    {
        string unityMainGuid = pbxProject.GetUnityMainTargetGuid();

        PlistDocument entitlements = new PlistDocument();

        string key_KeychainSharing = "keychain-access-groups";
        var arrayValue = new PlistElementArray();
        entitlements.root[key_KeychainSharing] = arrayValue;
        arrayValue.values.Add(new PlistElementString("$(AppIdentifierPrefix)" + Application.identifier)); // sharekey

        string key_PushNotifications = "aps-environment";

        if (isDevelopment)
        {
            entitlements.root[key_PushNotifications] = new PlistElementString("development");
        }
        else
        {
            entitlements.root[key_PushNotifications] = new PlistElementString("production");
        }

        string key_gameCenter = "com.apple.developer.game-center";
        entitlements.root[key_gameCenter] = new PlistElementBoolean(true);

        
        pbxProject.AddCapability(unityMainGuid, PBXCapabilityType.KeychainSharing, RelativeEntitlementPath);
        pbxProject.AddCapability(unityMainGuid, PBXCapabilityType.PushNotifications, RelativeEntitlementPath);
        pbxProject.AddCapability(unityMainGuid, PBXCapabilityType.GameCenter,RelativeEntitlementPath);
        pbxProject.AddCapability(unityMainGuid, PBXCapabilityType.InAppPurchase,RelativeEntitlementPath);
        pbxProject.AddCapability(unityMainGuid, PBXCapabilityType.SignInWithApple, RelativeEntitlementPath);
        
        entitlements.WriteToFile(ProjectEntitlementsPath);

        StreamReader reader = new StreamReader(ProjectEntitlementsPath);
        var content = reader.ReadToEnd().Trim();
        reader.Close();
        var original = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        var target =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?> \n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">";
        content = content.Replace(original, target);

        StreamWriter writer = new StreamWriter(new FileStream(ProjectEntitlementsPath, FileMode.Create));
        writer.WriteLine(content);
        writer.Flush();
        writer.Close();
    }

    /// <summary>
    /// plist 编辑
    /// </summary>
    /// <param name="rootDict"></param>
    /// <param name="path"></param>
    private static void OnGeneratePlistFile(PlistElementDict rootDict, bool isDevelopment)
    {
        // rootDict.SetString("CFBundleIdentifier", "com.gekko.rok");
        //必填 -新规
        rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        //ATT
        rootDict.SetString("NSUserTrackingUsageDescription",
            "Your data will be used to deliver personalized ads to you.");
    }

    private static void OnInfoPlist(PBXProject pbxProject, bool isDevelopment)
    {
        string folderPath = Application.dataPath + "/../ExportData/IOS/InfoPlist";
        DirectoryInfo dir = new DirectoryInfo(folderPath);
        if (!dir.Exists) return;
        //unity-iphone 操作
        string unityMainGuid = pbxProject.GetUnityMainTargetGuid();

        List<string> locales = new List<string>();
        var localeDirs = dir.GetDirectories("*.lproj", SearchOption.TopDirectoryOnly);
        foreach (var locale in localeDirs)
        {
            string s = Path.GetFileNameWithoutExtension(locale.Name);
            locales.Add(s);
        }

        foreach (var locale in locales)
        {
            string fileName = $"{locale}.lproj";
            var guid = pbxProject.AddFolderReference($"{folderPath}/{fileName}", $"{fileName}");
            pbxProject.AddFileToBuild(unityMainGuid, guid);
        }
    }

    private static void SetURLSchemeByPlist(PlistElementDict rootDict)
    {
        //先注释掉 使用Facebook自己的测试
        PlistElementArray urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
        PlistElementDict urlTypeDict = urlTypesArray.AddDict();
        urlTypeDict.SetString("CFBundleURLName", "applink.partygamesvc.com");
        PlistElementArray urlSchemesArray = urlTypeDict.CreateArray("CFBundleURLSchemes");
        urlSchemesArray.AddString("https");
        urlSchemesArray.AddString("http");
        urlSchemesArray.AddString("partygame");
    }

    private static void SetFacebookMessenger(PlistElementDict rootDict)
    {
        var urlTypesArray = rootDict.CreateArray("LSApplicationQueriesSchemes");
        urlTypesArray.AddString("fbapi");
        urlTypesArray.AddString("fb-messenger-api");
        urlTypesArray.AddString("fbshareextension");
        urlTypesArray.AddString("fbauth2");
    }
    

    /// <summary>
    /// 苹果登录插入 
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="manager"></param>
    /// <param name="pbxProject"></param>
    private static void AddSignInWithAppleWithCompatibility(ProjectCapabilityManager manager,PBXProject pbxProject)
    {
        manager.AddSignInWithAppleWithCompatibility(pbxProject.GetUnityFrameworkTargetGuid());
    }

#endif
}