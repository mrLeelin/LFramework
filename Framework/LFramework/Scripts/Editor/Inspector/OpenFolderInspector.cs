using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Editor;

namespace LFramework.Editor
{
    public class OpenFolderInspector 
    {
        [ShowInInspector]
        public void OpenConsoleLogFolder()
        {
            OpenFolder.OpenFolderConsoleLogPath();
        }
        
        [ShowInInspector]
        public void OpenDataPathFolder()
        {
            OpenFolder.OpenFolderDataPath();
        }
    }

}
