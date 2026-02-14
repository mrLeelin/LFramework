using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LFramework.Editor;
using UnityEditor;
using UnityEngine;

namespace Luban.Editor
{
    [InitializeOnLoad]
    public class ToolBarMenu
    {  
        static ToolBarMenu()
        {
            if (Application.isBatchMode) return;
            ToolbarExtender.AddLeftToolbarGUI(OnDrawUpdateTable,10);
            ToolbarExtender.AddLeftToolbarGUI(OnDrawUpdateProto,20);
        }


        static void OnDrawUpdateTable()
        {
            if (GUILayout.Button("Update Table", GUILayout.Width(130)))
            {
                UpdateTables();
            }
        }

        static void OnDrawUpdateProto()
        {
            if (GUILayout.Button("Update Proto", GUILayout.Width(130)))
            {
                UpdateProto();
            }
        }


        private static void UpdateTables()
        {
            UnityEngine.Debug.Log("Start Update Table ...");
            string path = string.Empty;
#if UNITY_EDITOR_WIN
            path = Application.dataPath + "/../Tools/Gen.bat";
            Process process = new Process();
            process.StartInfo.WorkingDirectory = Application.dataPath + "/../Tools";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = path;
            process.Start();
            process.WaitForExit();
            process.Close();

#else
        path = "file://" + Application.dataPath + "/../Tools/Gen.command";
        Application.OpenURL(path);
#endif

            UnityEngine.Debug.Log("End Update Table ...");
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static void UpdateProto()
        {
            UnityEngine.Debug.Log("Start Update Table ...");
            string path = string.Empty;
#if UNITY_EDITOR_WIN
            path = Application.dataPath + "/../ToolsProtoBuf/build.bat";
            Process process = new Process();
            process.StartInfo.WorkingDirectory = Application.dataPath + "/../ToolsProtoBuf";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = path;
            process.Start();
            process.WaitForExit();
            process.Close();

#else
      
#endif

            UnityEngine.Debug.Log("End Update Table ...");
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        
    }
    

}
