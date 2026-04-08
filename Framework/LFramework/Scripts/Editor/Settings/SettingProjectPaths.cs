using System.IO;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// 工程侧 Setting 资产标准目录。
    /// </summary>
    public static class SettingProjectPaths
    {
        public const string Root = "Assets/Game/Settings";
        public const string SelectorFolder = Root + "/Selector";
        public const string BaseFolder = Root + "/Base";
        public const string ComponentFolder = Root + "/Components";
        public const string SyncFolder = Root + "/Sync";
        public const string SnapshotFolder = SyncFolder + "/Snapshots";

        public const string SelectorAssetPath = SelectorFolder + "/ProjectSettingSelector.asset";
        public const string SyncStateAssetPath = SyncFolder + "/SettingSyncState.asset";

        public static string GetBaseSettingAssetPath(string fileName)
        {
            return $"{BaseFolder}/{SanitizeFileName(fileName)}.asset";
        }

        public static string GetComponentSettingAssetPath(string fileName)
        {
            return $"{ComponentFolder}/{SanitizeFileName(fileName)}.asset";
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}
