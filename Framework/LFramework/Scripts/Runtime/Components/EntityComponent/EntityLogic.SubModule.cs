using System;
using System.Collections.Generic;
using GameFramework;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

using UnityEngine;

namespace LFramework.Runtime
{
    public abstract partial class NoParamEntityLogic
    {
        /// 子模块列表，可能会动态修改，序列化的item只是拼在prefab中的那些subModule
        [SerializeField, FoldoutGroup("SubModule Setting"), LabelText("SubModule列表，点击放大镜自动填充")]
        [ListDrawerSettings(IsReadOnly = true, OnBeginListElementGUI = "SubModuleListItemBegin",
            OnEndListElementGUI = "SubModuleListItemEnd")]
        [InlineButton("FindSubModules", SdfIconType.Search, "")]
        private List<BaseSubEntity> subModuleList = new();

        private readonly GameFrameworkMultiDictionary<Type, BaseSubEntity> _subModuleKeyMap = new();

        /// <summary>
        /// 获取第一个SubModule 根据类型
        /// </summary>
        /// <typeparam name="TSubModule"></typeparam>
        /// <returns></returns>
        public TSubModule GetSubModule<TSubModule>() where TSubModule : BaseSubEntity
        {
            var type = typeof(TSubModule);
            if (!_subModuleKeyMap.TryGetValue(type, out var subModules))
            {
                return null;
            }

            if (subModules.Count == 0)
            {
                return null;
            }
            return subModules.First.Value as TSubModule;
        }

        /// <summary>
        /// 获取所有的SubModule 根据类型
        /// </summary>
        /// <typeparam name="TSubModule"></typeparam>
        /// <returns></returns>
        public List<TSubModule> GetSubModules<TSubModule>() where TSubModule : BaseSubEntity
        {
            var result = new List<TSubModule>();
            GetSubModules(result);
            return result;
        }

        /// <summary>
        /// 获取所有的SubModule 根据类型
        /// </summary>
        /// <param name="result"></param>
        /// <typeparam name="TSubModule"></typeparam>
        public void GetSubModules<TSubModule>(List<TSubModule> result) where TSubModule : BaseSubEntity
        {
            var type = typeof(TSubModule);
            result.Clear();
            if (!_subModuleKeyMap.TryGetValue(type, out var subModules))
            {
                return;
            }

            foreach (var baseSubEntity in subModules)
            {
                result.Add((TSubModule)baseSubEntity);
            }
        }

        /// <summary>
        /// 获取单个SubModule 根据类型和Key
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="TSubModule"></typeparam>
        /// <returns></returns>
        public TSubModule GetSubModule<TSubModule>(string key) where TSubModule : BaseSubEntity
        {
            var type = typeof(TSubModule);
            if (!_subModuleKeyMap.TryGetValue(type, out var subModules))
            {
                return null;
            }

            foreach (var subModule in subModules)
            {
                // 这个Key我需要随时修改故此不存储
                if (subModule.GetKey().Equals(key))
                {
                    return (TSubModule)subModule;
                }
            }

            return null;
        }

#if UNITY_EDITOR

        public void FindSubModules()
        {
            subModuleList ??= new List<BaseSubEntity>();
            subModuleList.Clear();
            GetComponentsInChildren(true, subModuleList);
            EditorUtility.SetDirty(this);
        }

        private void SubModuleListItemBegin()
        {
            GUIHelper.PushGUIEnabled(false);
        }

        private void SubModuleListItemEnd()
        {
            GUIHelper.PopGUIEnabled();
        }

#endif
    }
}