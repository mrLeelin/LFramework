
using GameFramework;
using GameFramework.FileSystem;
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
            EditorGUILayout.LabelField("File System Count", _fileSystemComponent.Count.ToString());

            IFileSystem[] fileSystems = _fileSystemComponent.GetAllFileSystems();
            foreach (IFileSystem fileSystem in fileSystems)
            {
                DrawFileSystem(fileSystem);
            }
        }
        
        private void DrawFileSystem(IFileSystem fileSystem)
        {
            EditorGUILayout.LabelField(fileSystem.FullPath,
                Utility.Text.Format("{0}, {1} / {2} Files", fileSystem.Access, fileSystem.FileCount,
                    fileSystem.MaxFileCount));
        }
    }
}