using System.IO;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LFramework.Samples.ProcedureFlow.Editor
{
    public static class ProcedureFlowSampleInstaller
    {
        [MenuItem("LFramework/Samples/Procedure Flow/Install Sample")]
        public static void InstallSample()
        {
            string sampleRoot = GetSampleRoot();
            string scenesFolder = EnsureFolder(sampleRoot, "Scenes");
            string settingsFolder = EnsureFolder(sampleRoot, "Settings");

            GameSetting gameSetting = LoadOrCreateGameSetting($"{settingsFolder}/ProcedureFlowGameSetting.asset");
            ProcedureComponentSetting procedureSetting = LoadOrCreateProcedureSetting(
                $"{settingsFolder}/ProcedureFlowProcedureComponentSetting.asset");
            ProjectSettingSelector selector = LoadOrCreateSelector(
                $"{settingsFolder}/ProcedureFlowProjectSettingSelector.asset",
                gameSetting,
                procedureSetting);

            CreateScene($"{scenesFolder}/ProcedureFlow.unity", selector);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Procedure Flow", "Sample scene and settings created successfully.", "OK");
        }

        private static string GetSampleRoot()
        {
            string[] guids = AssetDatabase.FindAssets("ProcedureFlowSampleInstaller t:Script");
            if (guids.Length == 0)
            {
                throw new FileNotFoundException("Could not locate ProcedureFlowSampleInstaller script.");
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string editorFolder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(editorFolder))
            {
                throw new DirectoryNotFoundException("Could not resolve ProcedureFlow sample folder.");
            }

            return editorFolder[..editorFolder.LastIndexOf("/Editor", System.StringComparison.Ordinal)];
        }

        private static string EnsureFolder(string parent, string name)
        {
            string folder = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parent, name);
            }

            return folder;
        }

        private static GameSetting LoadOrCreateGameSetting(string assetPath)
        {
            GameSetting asset = AssetDatabase.LoadAssetAtPath<GameSetting>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<GameSetting>();
            asset.name = "ProcedureFlowGameSetting";
            asset.isRelease = false;
            asset.isResourcesBuildIn = true;
            asset.versionUrl = "http://127.0.0.1/version";
            asset.ip = "http://127.0.0.1:8080";
            asset.webSocketIp = "ws://127.0.0.1:8080/ws";
            asset.appVersion = "1.0.0.0";
            asset.resourceVersion = "1.0.0.0";
            asset.cdnType = CdnType.FullPackage;
            asset.channel = "Sample";
            asset.cdnUrl = "http://127.0.0.1/cdn";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static ProcedureComponentSetting LoadOrCreateProcedureSetting(string assetPath)
        {
            ProcedureComponentSetting asset = AssetDatabase.LoadAssetAtPath<ProcedureComponentSetting>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ProcedureComponentSetting>();
                asset.name = "ProcedureFlowProcedureComponentSetting";
                asset.bindTypeName = "UnityGameFramework.Runtime.ProcedureComponent";
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject serializedObject = new(asset);
            SerializedProperty available = serializedObject.FindProperty("m_AvailableProcedureTypeNames");
            available.arraySize = 2;
            available.GetArrayElementAtIndex(0).stringValue = typeof(ProcedureFlowLaunchProcedure).FullName;
            available.GetArrayElementAtIndex(1).stringValue = typeof(ProcedureFlowHomeProcedure).FullName;
            serializedObject.FindProperty("m_EntranceProcedureTypeName").stringValue = typeof(ProcedureFlowLaunchProcedure).FullName;
            serializedObject.FindProperty("m_EntranceHotfixProcedureTypeName").stringValue = string.Empty;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ProjectSettingSelector LoadOrCreateSelector(string assetPath, GameSetting gameSetting, ProcedureComponentSetting procedureSetting)
        {
            ProjectSettingSelector asset = AssetDatabase.LoadAssetAtPath<ProjectSettingSelector>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ProjectSettingSelector>();
                asset.name = "ProcedureFlowProjectSettingSelector";
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.SetSetting(gameSetting);
            asset.SetComponentSetting(procedureSetting);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateScene(string scenePath, ProjectSettingSelector selector)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var app = new GameObject("App");
            var behaviour = app.AddComponent<ProcedureFlowApplicationBehaviour>();

            SerializedObject serializedObject = new(behaviour);
            serializedObject.FindProperty("sampleProjectSettingSelector").objectReferenceValue = selector;

            string[] componentTypes =
            {
                "UnityGameFramework.Runtime.BaseComponent",
                "UnityGameFramework.Runtime.EventComponent",
                "UnityGameFramework.Runtime.FsmComponent",
                "UnityGameFramework.Runtime.ProcedureComponent",
                "UnityGameFramework.Runtime.ReferencePoolComponent"
            };

            SerializedProperty allComponents = serializedObject.FindProperty("allComponentTypes");
            allComponents.arraySize = componentTypes.Length;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                allComponents.GetArrayElementAtIndex(i).stringValue = componentTypes[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}
