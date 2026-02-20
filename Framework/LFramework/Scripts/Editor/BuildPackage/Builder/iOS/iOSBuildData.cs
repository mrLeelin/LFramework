using System;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建签名配置数据
    /// 存储 iOS 构建所需的证书、Team ID、Provisioning Profile 等信息
    /// </summary>
    public static class iOSBuildData
    {
        /// <summary>
        /// Mobile Provision UUID
        /// 用于指定使用的 Provisioning Profile
        /// </summary>
        public static string MobileProvisionUUid = "6c3a8ee3-33db-4d93-bc7a-617d6fb9bfdd";

        /// <summary>
        /// Apple 开发团队 ID
        /// 从 Apple Developer 账号中获取
        /// </summary>
        public static string AppleDevelopTeamId = "8UGM8WH7U3";

        /// <summary>
        /// 代码签名身份
        /// 格式：证书类型 + 证书持有者名称 + Team ID
        /// 可从钥匙串 -> 证书 -> 双击证书 -> 常用名称字段获取
        /// </summary>
        public static string CODE_SIGN_IDENTITY = "Apple Distribution: nan wu (8UGM8WH7U3)";

        /// <summary>
        /// 开发环境 Provisioning Profile 名称
        /// 用于 Debug 构建
        /// </summary>
        public static string Profiles_Development = "iOS Team Provisioning Profile";

        /// <summary>
        /// AppStore Provisioning Profile 名称
        /// 用于 Release 构建
        /// </summary>
        public static string Profiles_AppStore = "AppStore Distribution Profile";
    }
}
