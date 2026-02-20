using System;
using System.IO;
using UnityEngine;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建配置
    /// 封装所有可配置项，避免硬编码，提高代码灵活性和可维护性
    /// </summary>
    public class iOSBuildConfig
    {
        // ==================== 路径配置 ====================

        /// <summary>
        /// 构建输出路径
        /// Xcode 项目的根目录
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// 本地化文件夹路径
        /// 存放 *.lproj 本地化资源的目录
        /// </summary>
        public string LocalizationFolderPath { get; set; }

        // ==================== URL Scheme 配置 ====================

        /// <summary>
        /// 应用的 URL Scheme
        /// 用于 Deep Link 和第三方应用跳转
        /// </summary>
        public string URLScheme { get; set; }

        /// <summary>
        /// Bundle URL 名称
        /// CFBundleURLName 的值，通常是应用的域名
        /// </summary>
        public string BundleURLName { get; set; }

        // ==================== 自定义配置 ====================

        /// <summary>
        /// 自定义 AppController 类名
        /// 用于替换默认的 UnityAppController
        /// </summary>
        public string CustomAppControllerName { get; set; }

        // ==================== CocoaPods 配置 ====================

        /// <summary>
        /// CocoaPods 命令路径
        /// 自动检测或手动指定 pod 命令的完整路径
        /// </summary>
        public string PodCommandPath { get; set; }

        // ==================== 构建模式 ====================

        /// <summary>
        /// 是否为开发构建
        /// true = Development, false = Release
        /// </summary>
        public bool IsDevelopment { get; set; }

        // ==================== 工厂方法 ====================

        /// <summary>
        /// 从 BuildSetting 创建配置
        /// 自动填充默认值并检测系统环境
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <param name="outputPath">构建输出路径</param>
        /// <returns>iOS 构建配置实例</returns>
        public static iOSBuildConfig CreateFromBuildSetting(BuildSetting buildSetting, string outputPath)
        {
            var config = new iOSBuildConfig
            {
                // 使用传入的输出路径
                OutputPath = outputPath,

                // 本地化文件路径（可以从 BuildSetting 扩展读取）
                LocalizationFolderPath = Path.Combine(Application.dataPath, "../ExportData/IOS/InfoPlist"),

                // URL Scheme 配置（TODO: 应该从 BuildSetting 或配置文件读取）
                URLScheme = "partygame",
                BundleURLName = "applink.partygamesvc.com",

                // 自定义 AppController 名称
                CustomAppControllerName = "MyAppController",

                // 构建模式
                IsDevelopment = !buildSetting.isRelease
            };

            // 动态检测 pod 命令路径
            config.PodCommandPath = DetectPodCommandPath();

            return config;
        }

        // ==================== 验证方法 ====================

        /// <summary>
        /// 验证配置有效性
        /// 在构建前检查所有必需的配置项
        /// </summary>
        /// <exception cref="InvalidOperationException">配置无效时抛出异常</exception>
        public void Validate()
        {
            // 验证输出路径
            if (string.IsNullOrEmpty(OutputPath))
            {
                throw new InvalidOperationException("Output path is not configured");
            }

            // 验证 Apple Team ID
            if (string.IsNullOrEmpty(iOSBuildData.AppleDevelopTeamId))
            {
                throw new InvalidOperationException(
                    "Apple Team ID is not configured in iOSBuildData. " +
                    "Please set iOSBuildData.AppleDevelopTeamId.");
            }

            // 验证代码签名身份
            if (string.IsNullOrEmpty(iOSBuildData.CODE_SIGN_IDENTITY))
            {
                throw new InvalidOperationException(
                    "Code sign identity is not configured in iOSBuildData. " +
                    "Please set iOSBuildData.CODE_SIGN_IDENTITY.");
            }

            // 验证 CocoaPods 命令
            if (string.IsNullOrEmpty(PodCommandPath))
            {
                throw new InvalidOperationException(
                    "CocoaPods command not found. " +
                    "Please install CocoaPods: https://cocoapods.org/");
            }
        }

        // ==================== 私有辅助方法 ====================

        /// <summary>
        /// 检测 pod 命令路径
        /// 按优先级检查多个可能的路径
        /// </summary>
        /// <returns>pod 命令的完整路径，如果未找到则返回 null</returns>
        private static string DetectPodCommandPath()
        {
            // 可能的 pod 命令路径（按优先级排序）
            string[] possiblePaths = new[]
            {
                "/opt/homebrew/bin/pod",  // Apple Silicon Mac (M1/M2/M3)
                "/usr/local/bin/pod",     // Intel Mac
                "pod"                     // PATH 环境变量中的 pod
            };

            foreach (var path in possiblePaths)
            {
                // 如果是绝对路径，检查文件是否存在
                if (Path.IsPathRooted(path))
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                else
                {
                    // 如果是相对路径（如 "pod"），假设在 PATH 中
                    // 这里直接返回，让系统在执行时查找
                    return path;
                }
            }

            // 未找到 pod 命令
            return null;
        }
    }
}
