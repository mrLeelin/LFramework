using System.Collections.Generic;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 基于 YooAsset 的 DLL 资源注册器实现
    /// YooAsset 通过 Collector 自动收集资源目录下的文件，不需要手动注册条目
    /// </summary>
    public class YooAssetDllRegistrar : IDllResourceRegistrar
    {
        public bool RegisterAotDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
#if YOOASSET_SUPPORT
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }

        public bool RegisterHotfixDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
#if YOOASSET_SUPPORT
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }

        public bool EnsureGroupExists(string groupName)
        {
#if YOOASSET_SUPPORT
            return true;
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }
    }
}
