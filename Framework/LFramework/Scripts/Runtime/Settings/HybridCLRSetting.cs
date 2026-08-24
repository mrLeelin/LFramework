using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    public enum HybridCLRLoadMode
    {
        Standard,
        DifferentialHybridMetaVersion
    }

    /// <summary>
    /// The setting for HybridClr
    /// </summary>
    [CreateAssetMenu(order = 1, fileName = "HybridCLRSetting",
        menuName = "LFramework/HybridCLR/HybridCLRSetting")]
    public sealed class HybridCLRSetting : BaseSetting
    {
        [FoldoutGroup("Runtime Mode")]
        [Tooltip("Selects the standard interpreter loader or the commercial DHE MetaVersion loader.")]
        public HybridCLRLoadMode loadMode = HybridCLRLoadMode.Standard;

#if UNITY_EDITOR
        [FoldoutGroup("Editor Settings")]
        /// <summary>
        /// Aot code folder path, this is the path where the AOT code will be generated.
        /// </summary>
        [Tooltip("Aot code folder path, this is the path where the AOT code will be generated.")]
        [FolderPath(ParentFolder = "Assets/", AbsolutePath = false, RequireExistingPath = true)]
        public string aotDllFolderPath;

        [FoldoutGroup("Editor Settings")]
        [Tooltip("Hotfix code folder path, this is the path where the hotfix code will be generated.")]
        [FolderPath(ParentFolder = "Assets/", AbsolutePath = false, RequireExistingPath = true)]
        public string hotfixDllFolderPath;

        [FoldoutGroup("Editor Settings/Addressable Groups")]
        [Tooltip("Aot code folder path, this is the path where the AOT code will be generated.")]
        public string aotAddressableGroupName = "Update_Aot";

        [FoldoutGroup("Editor Settings/Addressable Groups")]
        [Tooltip("Hotfix code folder path, this is the path where the hotfix code will be generated.")]
        public string codeAddressableGroupName = "Update_Code";
#endif

        [FoldoutGroup("Default Labels")]
        [Tooltip("Download aot assembly label, this is the label used to mark the AOT assemblies for download.")]
        public string defaultAotDllLabel = "aot_dll";
        [FoldoutGroup("Default Labels")]
        [Tooltip("Download code assembly label, this is the label used to mark the code assemblies for download.")]
        public string defaultCodeDllLabel = "code_dll";
        [Tooltip("Default label for init assets, this is the label used to mark the initial assets that need to be loaded at the start of the game.")]
        [FoldoutGroup("Default Labels")]
        public string defaultInitLabel = "init_assets";

        [FoldoutGroup("DHE Labels")]
        [Tooltip("Label for current DHE MetaVersion files.")]
        public string dheCurrentMetaLabel = "dhe_current_meta";

        [FoldoutGroup("DHE Labels")]
        [Tooltip("Label for the original MetaVersion files generated from the player baseline.")]
        public string dheOriginalMetaLabel = "dhe_original_meta";
        
        [Tooltip("Sort order of AOT assemblies, this is the order in which the AOT assemblies will be loaded.")]
        /// <summary>
        /// Sort order of AOT assemblies.
        /// </summary>
        public List<string> hotfixAssembliesSort;

        [Tooltip("DHE assembly load order. Referenced DHE assemblies must appear before their dependants.")]
        public List<string> differentialHybridAssembliesSort;

        /// <summary>
        /// Main Hotfix assembly name
        /// </summary>
        public string logicMainDllName = "LFramework.Hotfix";
    }
}
