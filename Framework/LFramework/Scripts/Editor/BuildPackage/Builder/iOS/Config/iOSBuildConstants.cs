namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建常量定义
    /// 集中管理所有魔法字符串和路径，避免硬编码
    /// </summary>
    public static class iOSBuildConstants
    {
        // ==================== 文件路径常量 ====================

        /// <summary>
        /// Entitlements 文件名
        /// </summary>
        public const string ENTITLEMENTS_FILE_NAME = "game.entitlements";

        /// <summary>
        /// UnityAppController.mm 文件路径
        /// </summary>
        public const string UNITY_APP_CONTROLLER_PATH = "Classes/UnityAppController.mm";

        /// <summary>
        /// main.mm 文件路径
        /// </summary>
        public const string MAIN_MM_PATH = "Classes/main.mm";

        /// <summary>
        /// Info.plist 文件路径
        /// </summary>
        public const string INFO_PLIST_PATH = "Info.plist";

        /// <summary>
        /// Podfile 文件路径
        /// </summary>
        public const string PODFILE_PATH = "Podfile";

        /// <summary>
        /// Pod 安装脚本名称
        /// </summary>
        public const string POD_INSTALL_SCRIPT_NAME = "podinstall.sh";

        // ==================== Framework 名称常量 ====================

        /// <summary>
        /// UnityFramework Target 需要的系统框架
        /// </summary>
        public static readonly string[] UNITY_FRAMEWORK_FRAMEWORKS = new[]
        {
            "GameKit.framework",
            "AppTrackingTransparency.framework"
        };

        /// <summary>
        /// UnityMain Target 需要的系统框架
        /// </summary>
        public static readonly string[] UNITY_MAIN_FRAMEWORKS = new[]
        {
            "GameKit.framework",
            "SystemConfiguration.framework",
            "CoreTelephony.framework",
            "Security.framework",
            "JavaScriptCore.framework",
            "Photos.framework",
            "iAd.framework",
            "WebKit.framework",
            "AppTrackingTransparency.framework",
            "UserNotifications.framework"
        };

        /// <summary>
        /// 需要添加的系统库文件
        /// </summary>
        public static readonly string[] REQUIRED_LIBRARIES = new[]
        {
            "libz.tbd",
            "libsqlite3.tbd",
            "libc++.tbd"
        };

        // ==================== Info.plist 键名常量 ====================

        /// <summary>
        /// 加密合规声明键名
        /// </summary>
        public const string PLIST_KEY_ENCRYPTION = "ITSAppUsesNonExemptEncryption";

        /// <summary>
        /// ATT（App Tracking Transparency）权限说明键名
        /// </summary>
        public const string PLIST_KEY_ATT = "NSUserTrackingUsageDescription";

        /// <summary>
        /// URL Types 键名
        /// </summary>
        public const string PLIST_KEY_URL_TYPES = "CFBundleURLTypes";

        /// <summary>
        /// URL Name 键名
        /// </summary>
        public const string PLIST_KEY_URL_NAME = "CFBundleURLName";

        /// <summary>
        /// URL Schemes 键名
        /// </summary>
        public const string PLIST_KEY_URL_SCHEMES = "CFBundleURLSchemes";

        /// <summary>
        /// LSApplicationQueriesSchemes 键名
        /// </summary>
        public const string PLIST_KEY_QUERIES_SCHEMES = "LSApplicationQueriesSchemes";

        /// <summary>
        /// 自定义 AppController 类名键名
        /// </summary>
        public const string PLIST_KEY_APP_CONTROLLER = "UnityAppControllerClass";

        /// <summary>
        /// AppsFlyer Swizzle 开关键名
        /// </summary>
        public const string PLIST_KEY_APPSFLYER_SWIZZLE = "AppsFlyerShouldSwizzle";

        // ==================== Facebook 相关常量 ====================

        /// <summary>
        /// Facebook SDK 需要的 Query Schemes
        /// </summary>
        public static readonly string[] FACEBOOK_QUERY_SCHEMES = new[]
        {
            "fbapi",
            "fb-messenger-api",
            "fbshareextension",
            "fbauth2"
        };

        // ==================== 正则表达式常量 ====================

        /// <summary>
        /// 匹配 isBackgroundLaunchOptions 方法返回值的正则表达式
        /// 用于修复 Facebook Deep Link 冷启动崩溃问题
        /// 匹配模式：isBackgroundLaunchOptions...return YES;
        /// 替换为：isBackgroundLaunchOptions...return NO;
        /// </summary>
        public const string REGEX_BACKGROUND_LAUNCH_OPTIONS =
            @"(?x)(isBackgroundLaunchOptions.+(?:.*\n)+?\s*return\ )YES(\;\n\})# }";

        /// <summary>
        /// 匹配 AppControllerClassName 定义的正则表达式
        /// 用于修改 main.mm 中的 AppController 类名
        /// </summary>
        public const string REGEX_APP_CONTROLLER_NAME =
            @"const char\* AppControllerClassName\s*=\s*""UnityAppController"";";

        /// <summary>
        /// 匹配 Podfile 中 use_frameworks! :linkage => :static 的正则表达式
        /// 用于修复 Firebase + Facebook Podfile 冲突问题
        /// </summary>
        public const string REGEX_USE_FRAMEWORKS =
            @"use_frameworks!\s*:linkage\s*=>\s*:static";

        // ==================== 文本内容常量 ====================

        /// <summary>
        /// ATT 权限说明文本
        /// </summary>
        public const string ATT_USAGE_DESCRIPTION =
            "Your data will be used to deliver personalized ads to you.";

        /// <summary>
        /// CocoaPods 警告抑制配置
        /// 添加到 Podfile 末尾，抑制未使用 Master Specs 仓库的警告
        /// </summary>
        public const string COCOAPODS_WARN_SUPPRESSION =
            "\n# 在安装 CocoaPods 依赖时，抑制未使用 Master Specs 仓库的警告\n" +
            "install! 'cocoapods', :warn_for_unused_master_specs_repo => false\n";

        // ==================== 路径前缀常量 ====================

        /// <summary>
        /// 系统库路径前缀
        /// </summary>
        public const string SYSTEM_LIB_PATH = "usr/lib/";

        /// <summary>
        /// Frameworks 路径前缀
        /// </summary>
        public const string FRAMEWORKS_PATH = "Frameworks/";
    }
}

