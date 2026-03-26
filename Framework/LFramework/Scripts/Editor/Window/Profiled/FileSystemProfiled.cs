using GameFramework;
using GameFramework.FileSystem;
using LFramework.Editor;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class FileSystemProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private FileSystemComponent _fileSystemComponent;

        internal override void Draw()
        {
            GetComponent(ref _fileSystemComponent);
            if (_fileSystemComponent == null)
            {
                EditorGUILayout.HelpBox("FileSystemComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("File System Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("File System Count", _fileSystemComponent.Count.ToString());
            EditorGUILayout.EndVertical();

            IFileSystem[] fileSystems = _fileSystemComponent.GetAllFileSystems();
            GameWindowChrome.DrawCompactHeader("Mounted File Systems");
            EditorGUILayout.BeginVertical("box");
            if (fileSystems == null || fileSystems.Length == 0)
            {
                EditorGUILayout.LabelField("No file systems found.");
            }
            else
            {
                foreach (IFileSystem fileSystem in fileSystems)
                {
                    DrawFileSystem(fileSystem);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFileSystem(IFileSystem fileSystem)
        {
            EditorGUILayout.LabelField(
                fileSystem.FullPath,
                Utility.Text.Format("{0}, {1} / {2} Files", fileSystem.Access, fileSystem.FileCount, fileSystem.MaxFileCount));
        }
    }
}
