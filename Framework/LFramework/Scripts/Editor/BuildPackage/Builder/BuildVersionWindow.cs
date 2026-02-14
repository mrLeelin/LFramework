using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFramework;
using LFramework.Editor.Builder;
using LFramework.Runtime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LFramework.Editor
{
    public partial class BuildVersionWindow
    {
        public BuildVersionWindow()
        {
            BuilderTarget = BuilderTarget.Windows;
#if UNITY_ANDROID
            BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            BuilderTarget = BuilderTarget.iOS;
#endif
        }


        public BuilderTarget BuilderTarget;

        [ShowIf("BuilderTarget", BuilderTarget.iOS)]
        public BuildIOSChannel IOSChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Windows)]
        public BuildWindowsChannel WindowsChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Android)]
        public BuildAndroidChannel AndroidChannel;

        public CdnType CdnType;

        public string AppVersion = "1.0.0.1";
        public string DownloadPackageUrl = "";
        [Header("默认设置")]
        public GameVersionConfig DefaultConfig;
        [Header("白名单设置")]
        public GameVersionConfig WhiteListConfig;
        public string WhiteList  = "";
        
        [OnInspectorGUI]
        private void Space3()
        {
            GUILayout.Space(20);
        }

        [Button("设置默认152Debug服务器")]
        [ResponsiveButtonGroup]
        public void DefaultDebugServer()
        {
            DefaultConfig.logicIp = "https://test-game.partygamesvc.com";
            DefaultConfig.webSocketIp = "wss://test-im.partygamesvc.com/ws";
            DefaultConfig.cdnUrl = "https://test-dataupdate.partygamesvc.com/cdn/";
            DownloadPackageUrl = "https://www.baidu.com";
            
            CdnType = CdnType.Debug;
        }

        [Button("设置默认152Release服务器")]
        [ResponsiveButtonGroup]
        public void DefaultReleaseServer()
        {
        }

        [Button("上传")]
        public void UploadServer(bool isDebugServer)
        {
            var setting = BuildGameVersion();
            var originJson = JsonUtility.ToJson(setting);
            var json = originJson
                .Replace("\\", "\\\\") // 先转义已经存在的反斜杠
                .Replace("\"", "\\\"");
            Debug.Log("Json:" + json);
            var upLoadSuccessful = false;
            if (isDebugServer)
            {
                upLoadSuccessful = PythonRunner.RunPythonScript("upload_version_to_s3.py",
                    $"\"{json}\" {GetVersionFilePath()} test-dataupdate.partygamesvc.com");
            }

            if (upLoadSuccessful)
            {
                EditorUtility.DisplayDialog("成功", "上传版本文件成功", "确定");
            }
        }


        private string GetVersionFilePath()
        {
            return $"{GetChannelName()}_{CdnType}/Version_{AppVersion}";
        }

        private string GetChannelName()
        {
            return BuildResourcesData.GetChannelName(new BuildResourcesData()
            {
                BuilderTarget = BuilderTarget,
                WindowsChannel = WindowsChannel,
                AndroidChannel = AndroidChannel,
                IOSChannel = IOSChannel
            });
        }

        private GameVersion BuildGameVersion()
        {
            return new GameVersion()
            {
                appVersion = AppVersion,
                downloadPackage = DownloadPackageUrl,
                defaultConfig = DefaultConfig,
                whiteListConfig = WhiteListConfig,
                userList = WhiteList,
            };
        }
    }
}