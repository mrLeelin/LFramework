using System.IO;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 创建构建目录任务
    /// 负责创建构建输出目录,如果目录已存在则先删除
    /// </summary>
    public class CreateDirectoryTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Create Build Directory";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Create build output directory and clean old files";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要创建目录
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时创建目录
            return context.BuildSetting.buildType == BuildType.APP;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>任务执行结果</returns>
        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                var folderPath = context.Builder.GetFolderPath();
                context.OutputFolder = folderPath;

                Debug.Log($"[CreateDirectoryTask] Output folder: {folderPath}");

                // 删除旧目录
                if (Directory.Exists(folderPath))
                {
                    Debug.Log($"[CreateDirectoryTask] Deleting old directory: {folderPath}");
                    DeleteDirectory(folderPath);
                }

                // 创建新目录
                Debug.Log($"[CreateDirectoryTask] Creating directory: {folderPath}");
                Directory.CreateDirectory(folderPath);

                Debug.Log($"[CreateDirectoryTask] Directory created successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Failed to create directory: {ex.Message}");
            }
        }

        /// <summary>
        /// 递归删除目录
        /// </summary>
        /// <param name="path">目录路径</param>
        private void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            // 递归删除所有文件和子目录
            string[] entries = Directory.GetFileSystemEntries(path);
            foreach (var entry in entries)
            {
                if (File.Exists(entry))
                {
                    FileInfo file = new FileInfo(entry);
                    if (file.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    {
                        file.Attributes = FileAttributes.Normal;
                    }
                    file.Delete();
                }
                else
                {
                    DeleteDirectory(entry);
                }
            }

            // 删除顶级目录
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                dirInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;
                dirInfo.Delete(true);
            }
        }
    }
}
