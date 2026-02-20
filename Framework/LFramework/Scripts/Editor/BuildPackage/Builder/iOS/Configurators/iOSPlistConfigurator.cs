using System;
using System.IO;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// iOS Info.plist 配置器
    /// 负责配置 Info.plist 文件，包括加密合规、ATT 权限、URL Schemes、Facebook 支持等
    /// </summary>
    public class iOSPlistConfigurator
    {
        private readonly iOSBuildConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">构建配置</param>
        public iOSPlistConfigurator(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 执行 Info.plist 配置
        /// </summary>
        public void Configure()
        {
#if UNITY_IOS
            iOSBuildLogger.LogStep("Info.plist configuration");

            string plistPath = Path.Combine(_config.OutputPath, iOSBuildConstants.INFO_PLIST_PATH);

            // 读取 plist 文件
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict rootDict = plist.root;

            // 配置各项设置
            ConfigureEncryption(rootDict);
            ConfigureATT(rootDict);
            ConfigureURLSchemes(rootDict);
            ConfigureFacebookSupport(rootDict);

            // 写回文件
            File.WriteAllText(plistPath, plist.WriteToString());

            iOSBuildLogger.LogSuccess("Info.plist configuration");
#else
            iOSBuildLogger.LogWarning("Skipping Info.plist configuration (not on iOS platform)");
#endif
        }

#if UNITY_IOS
        /// <summary>
        /// 配置加密合规声明
        /// 必填项：声明应用是否使用非豁免加密
        /// </summary>
        private void ConfigureEncryption(PlistElementDict rootDict)
        {
            rootDict.SetBoolean(iOSBuildConstants.PLIST_KEY_ENCRYPTION, false);
            iOSBuildLogger.LogInfo("Configured encryption compliance declaration");
        }

        /// <summary>
        /// 配置 ATT（App Tracking Transparency）权限说明
        /// iOS 14+ 必填项：说明为什么需要追踪用户
        /// </summary>
        private void ConfigureATT(PlistElementDict rootDict)
        {
            rootDict.SetString(
                iOSBuildConstants.PLIST_KEY_ATT,
                iOSBuildConstants.ATT_USAGE_DESCRIPTION);

            iOSBuildLogger.LogInfo("Configured ATT permission description");
        }

        /// <summary>
        /// 配置 URL Schemes
        /// 支持 Deep Link 和第三方应用跳转
        /// </summary>
        private void ConfigureURLSchemes(PlistElementDict rootDict)
        {
            // 创建 CFBundleURLTypes 数组
            PlistElementArray urlTypesArray = rootDict.CreateArray(iOSBuildConstants.PLIST_KEY_URL_TYPES);

            // 添加 URL Type
            PlistElementDict urlTypeDict = urlTypesArray.AddDict();
            urlTypeDict.SetString(iOSBuildConstants.PLIST_KEY_URL_NAME, _config.BundleURLName);

            // 添加 URL Schemes
            PlistElementArray urlSchemesArray = urlTypeDict.CreateArray(iOSBuildConstants.PLIST_KEY_URL_SCHEMES);
            urlSchemesArray.AddString("https");
            urlSchemesArray.AddString("http");
            urlSchemesArray.AddString(_config.URLScheme);

            iOSBuildLogger.LogInfo($"Configured URL Schemes: https, http, {_config.URLScheme}");
        }

        /// <summary>
        /// 配置 Facebook Messenger 支持
        /// 添加 LSApplicationQueriesSchemes 以支持 Facebook SDK
        /// </summary>
        private void ConfigureFacebookSupport(PlistElementDict rootDict)
        {
            // 创建 LSApplicationQueriesSchemes 数组
            var querySchemesArray = rootDict.CreateArray(iOSBuildConstants.PLIST_KEY_QUERIES_SCHEMES);

            // 添加 Facebook 相关的 Query Schemes
            foreach (var scheme in iOSBuildConstants.FACEBOOK_QUERY_SCHEMES)
            {
                querySchemesArray.AddString(scheme);
            }

            iOSBuildLogger.LogInfo($"Configured Facebook support with {iOSBuildConstants.FACEBOOK_QUERY_SCHEMES.Length} query schemes");
        }
#endif
    }
}
