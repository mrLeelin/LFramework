using System;
using System.IO;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder
{
    public class BuildWindow : EditorWindow
    {
        /*
        [MenuItem("Tools/Build/Build Window", false, 51)]
        public static void OnOpen()
        {
            EditorWindow.GetWindow<BuildWindow>();
        }

        private BuildSetting _buildData;

        private void OnEnable()
        {
            _buildData = new BuildSetting
            {
                //builderTarget = ConvertToBuilderTarget(EditorUserBuildSettings.activeBuildTarget)
            };
        }

        public void OnGUI()
        {
            if (_buildData == null) return;
            //平台
            _buildData.builderTarget = (BuilderTarget)EditorGUILayout.EnumPopup("Build Target", _buildData.builderTarget);
            //渠道
            switch (_buildData.builderTarget)
            {
                case BuilderTarget.Windows:
                    _buildData.windowsChannel =
                        (BuildWindowsChannel)EditorGUILayout.EnumPopup("Channel", _buildData.windowsChannel);
                    break;
                case BuilderTarget.Android:
                    _buildData.androidChannel =
                        (BuildAndroidChannel)EditorGUILayout.EnumPopup("Channel", _buildData.androidChannel);
                    _buildData.buildAndroidAppType =
                        (BuildAndroidAppType)EditorGUILayout.EnumPopup("AndroidAppType", _buildData.buildAndroidAppType);
                    break;
                case BuilderTarget.iOS:
                    _buildData.iosChannel = (BuildIOSChannel)EditorGUILayout.EnumPopup("Channel", _buildData.iosChannel);
                    break;
            }
            

            _buildData.buildType = (BuildType)EditorGUILayout.EnumPopup("Build Type", _buildData.buildType);
            _buildData.isBuildDll = EditorGUILayout.Toggle("打包热更新dll",_buildData.isBuildDll);
            _buildData.isDeepProfiler = EditorGUILayout.Toggle("Deep Profiler", _buildData.isDeepProfiler);
            _buildData.isRelease = EditorGUILayout.Toggle("Is Release", _buildData.isRelease);

            //版本
            _buildData.appVersion = EditorGUILayout.TextField("AppVersion", _buildData.appVersion);
            _buildData.versionCode = EditorGUILayout.IntField("VersionCode", _buildData.versionCode);

            if (_buildData.buildType == BuildType.APP)
            {
                _buildData.isBuildResources = EditorGUILayout.Toggle("Is Build Resources", _buildData.isBuildResources);
                if (_buildData.isBuildResources)
                {
                    //是否打到包内
                    _buildData.isResourcesBuildIn =
                        EditorGUILayout.Toggle("Is Resources BuildIn", _buildData.isResourcesBuildIn);
                    if (!_buildData.isResourcesBuildIn)
                    {
                        //上个版本
                        _buildData.resourcesVersion =
                            EditorGUILayout.TextField("Resources Version", _buildData.resourcesVersion);
                        //选择服务器
                        _buildData.cdnType = (CdnType)EditorGUILayout.EnumPopup("Cdn Type", _buildData.cdnType);
                    }
                }
            }
            else
            {
                //上个版本
                _buildData.resourcesVersion = EditorGUILayout.TextField("Resources Version", _buildData.resourcesVersion);
                //选择服务器
                _buildData.cdnType = (CdnType)EditorGUILayout.EnumPopup("Cdn Type", _buildData.cdnType);
                // 是否强制更新
                _buildData.isForceUpdate = EditorGUILayout.Toggle("Force Update", _buildData.isForceUpdate);
            }
            

            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                ProjectBuilder.Build(_buildData);
            }

            if (GUILayout.Button("Open Build Folder"))
            {
                var path = Path.Combine(Application.dataPath, "..", "Builds");
                EditorUtility.RevealInFinder(path);
            }

            this.Repaint();
        }
        */
       
    }
}