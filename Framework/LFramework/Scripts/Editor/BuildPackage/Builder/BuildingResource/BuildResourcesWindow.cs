using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LFramework.Runtime;
using Sirenix.OdinInspector.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder.BuildingResource
{
    public class BuildResourcesWindow : OdinEditorWindow
    {
        /*

        [MenuItem("Tools/Build/Build Resources Window", false, 52)]
        public static void OnOpen()
        {
            EditorWindow.GetWindow<BuildResourcesWindow>();
        }


        protected override void Initialize()
        {
            base.Initialize();
            _resourcesData.BuilderTarget = BuilderTarget.Windows;
#if UNITY_ANDROID
            _resourcesData.BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            _resourcesData.BuilderTarget = BuilderTarget.iOS;
#endif
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();


            //平台
            _resourcesData.BuilderTarget =
                (BuilderTarget)EditorGUILayout.EnumPopup("Build Target", _resourcesData.BuilderTarget);
            //渠道
            switch (_resourcesData.BuilderTarget)
            {
                case BuilderTarget.Windows:
                    _resourcesData.WindowsChannel =
                        (BuildWindowsChannel)EditorGUILayout.EnumPopup("Channel", _resourcesData.WindowsChannel);
                    break;
                case BuilderTarget.Android:
                    _resourcesData.AndroidChannel =
                        (BuildAndroidChannel)EditorGUILayout.EnumPopup("Channel", _resourcesData.AndroidChannel);
                    break;
                case BuilderTarget.iOS:
                    _resourcesData.IOSChannel =
                        (BuildIOSChannel)EditorGUILayout.EnumPopup("Channel", _resourcesData.IOSChannel);
                    break;
            }

            //版本
            _resourcesData.AppVersion = EditorGUILayout.TextField("App Version(母包版本)", _resourcesData.AppVersion);
            _resourcesData.ResourcesVersion = EditorGUILayout.TextField("Version", _resourcesData.ResourcesVersion);
            //是否打到包内
            _resourcesData.IsResourcesBuildIn = EditorGUILayout.Toggle("Is BuildIn", _resourcesData.IsResourcesBuildIn);
            _resourcesData.IsBuildDll = EditorGUILayout.Toggle("是否打包热更Dll", _resourcesData.IsBuildDll);
            if (!_resourcesData.IsResourcesBuildIn)
            {
                //是否增量包
                _isResourceUpdate = EditorGUILayout.Toggle("Is Resource Update", _isResourceUpdate);
                _resourcesData.BuildType = _isResourceUpdate ? BuildType.ResourcesUpdate : BuildType.APP;
                //选择服务器
                _resourcesData.BuildResourcesSeverModel =
                    (BuildResourcesSeverModel)EditorGUILayout.EnumPopup("ServerModel",
                        _resourcesData.BuildResourcesSeverModel);
            }


            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                Build(_resourcesData);
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("build Path", GetBuildPath(_resourcesData));
            EditorGUILayout.LabelField("load Path", GetLoadPath(_resourcesData));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("export Path", GetExportPath());
            EditorGUILayout.LabelField("export ads Path", GetExportAdsPath());
            EditorGUILayout.LabelField("export ads bin Path", GetExportAdsBinPath(_resourcesData));
            EditorGUILayout.LabelField("export build Path", GetExportBuildPath(_resourcesData));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Backup Path", GetBackupPath(_resourcesData));
            EditorGUILayout.LabelField("Backup ads Path", GetBackupAdsBuildPath(_resourcesData));
            EditorGUILayout.LabelField("Backup ads Bin Path", GetBackupAdsBinPath(_resourcesData));
            // EditorGUILayout.LabelField("Backup Build Path", GetBackupBuildPath(_resourcesData));
            EditorGUILayout.LabelField("Backup SeverData Path", GetBackupSeverDataBuildPath(_resourcesData));
            EditorGUILayout.Space();
            if (_resourcesData.BuildResourcesSeverModel != BuildResourcesSeverModel.LocalHost)
            {
                EditorGUILayout.LabelField("Backup Version Folder Path", GetBackupVersionFolderPath(_resourcesData));
                EditorGUILayout.LabelField("Backup Version Path", GetBackupVersionPath(_resourcesData));
            }

            EditorGUILayout.Space();

            // EditorGUILayout.LabelField("Last Backup ads Path", GetLastBackupAdsPath(_resourcesData));
            // EditorGUILayout.LabelField("Last Backup ads Bin Path", GetLastBackupAdsBinPath(_resourcesData));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("asset ads Bin Path", GetAssetAdsBinPath(_resourcesData));
            EditorGUILayout.LabelField("asset ads Bin File Path", GetAssetAdsBinFilePath(_resourcesData));
            EditorGUILayout.EndVertical();
            this.Repaint();
        }


        private BuildResourcesData _resourcesData = new BuildResourcesData();
        private bool _isResourceUpdate;

    */
    }
}