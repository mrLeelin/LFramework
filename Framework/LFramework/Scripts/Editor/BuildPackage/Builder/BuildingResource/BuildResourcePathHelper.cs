using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源构建通用路径辅助类
    /// 负责渠道、CDN、导出目录和备份目录等与具体资源系统无关的路径拼装
    /// </summary>
    public static class BuildResourcePathHelper
    {
        public const string ServerDataFolderName = "ServerData";
        public const string BackupFolderName = "BackUp_BuildResource";
        public const string BackupLastName = "LastBuild";
        public const string BackupFileName = "Version";
        public const string ReplaceRemote = "remote_";
        public const string ReplaceVersion = "_resource_version_";

        public static string GetChannelName(BuildSetting data)
        {
            string name = string.Empty;
            if (data == null)
            {
                return name;
            }

            switch (data.builderTarget)
            {
                case BuilderTarget.Windows:
                    name = data.windowsChannel.ToString();
                    break;
                case BuilderTarget.Android:
                    name = data.androidChannel.ToString();
                    break;
                case BuilderTarget.iOS:
                    name = data.iosChannel.ToString();
                    break;
            }

            return name;
        }

        public static string GetUrl(CdnType cdnType)
        {
            switch (cdnType)
            {
                case CdnType.Local:
                    return "http://[PrivateIpAddress]:[HostingServicePort]";
                default:
                    return ReplaceRemote;
            }
        }

        public static string GetFolderName(BuildSetting data)
        {
            return $"{GetChannelName(data)}_{data.resourcesVersion}_{data.cdnType}";
        }

        public static string GetReplaceVersionName(BuildSetting data)
        {
            return $"{GetChannelName(data)}_{ReplaceVersion}_{data.cdnType}";
        }

        public static string GetFolderNameBasedOnAppVersion(BuildSetting data)
        {
            return $"{GetChannelName(data)}_{data.appVersion}_{data.cdnType}";
        }

        public static string GetExportPath()
        {
            return $"{Application.dataPath}/../{ServerDataFolderName}";
        }

        public static string GetBuildPath(BuildSetting data)
        {
            return $"{ServerDataFolderName}/{GetFolderNameBasedOnAppVersion(data)}/{GetFolderName(data)}";
        }

        public static string GetExportBuildPath(BuildSetting data)
        {
            return $"{Application.dataPath}/../{GetBuildPath(data)}";
        }

        public static string GetExportVersionPath(BuildSetting data)
        {
            return $"{GetExportBuildPath(data)}/{BackupFileName}";
        }

        public static string GetTempDebugExportVersionPath(BuildSetting data)
        {
            return $"{GetExportPath()}/{GetChannelName(data)}_{data.cdnType}/{BackupFileName}";
        }

        public static string GetBackupPath(BuildSetting data)
        {
            return $"{Application.dataPath}/../{BackupFolderName}/{GetFolderNameBasedOnAppVersion(data)}";
        }

        public static string GetBackupSeverDataBuildPath(BuildSetting data)
        {
            return $"{GetBackupPath(data)}/{GetFolderName(data)}";
        }

        public static string GetBackupLastBuildPath(BuildSetting data)
        {
            return $"{GetBackupPath(data)}/{GetChannelName(data)}_{BackupLastName}_{data.cdnType}";
        }

        public static string GetDllBackupPath(BuildSetting data)
        {
            return $"{GetExportPath()}/{BackupFolderName}/{GetChannelName(data)}/{GetFolderName(data)}";
        }
    }
}
