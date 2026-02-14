

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "FileSystemComponentSetting",
        menuName = "LFramework/Settings/FileSystemComponentSetting")]
    public sealed class FileSystemComponentSetting : ComponentSetting
    {
        
        [SerializeField]
        private string m_FileSystemHelperTypeName = "UnityGameFramework.Runtime.DefaultFileSystemHelper";

        [SerializeField]
        private FileSystemHelperBase m_CustomFileSystemHelper = null;
        
    }
}