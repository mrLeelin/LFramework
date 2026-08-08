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
    public abstract partial class Window
    {
        /// 子模块列表，可能会动态修改，序列化的item只是拼在prefab中的那些subModule
        [SerializeField, FoldoutGroup("View Setting"), LabelText("SubModule列表，点击放大镜自动填充")]
        [ListDrawerSettings(IsReadOnly = true, OnBeginListElementGUI = "SubModuleListItemBegin",
            OnEndListElementGUI = "SubModuleListItemEnd")]
        [InlineButton("FindSubModules", SdfIconType.Search, "")]
        private List<BaseSubWindow> subModuleList = new();

        private readonly GameFrameworkMultiDictionary<Type, BaseSubWindow> _subModuleKeyMap = new();

        /// <summary>
        /// 缓存需要每帧更新的子模块（实现了 ISubModuleUpdate 接口）
        /// </summary>
        private readonly List<ISubModuleUpdate> _updatableSubModules = new();

        /// <summary>
        /// 获取子模块列表（推荐使用此重载，复用 List 避免 GC）
        /// </summary>
        /// <param name="result">用于接收结果的 List</param>
        /// <typeparam name="T">子模块类型</typeparam>
        public void GetSubModules<T>(List<T> result) where T : BaseSubWindow
        {
            result.Clear();
            if (!_subModuleKeyMap.TryGetValue(typeof(T), out var subModules))
            {
                return;
            }

            foreach (var sm in subModules)
                if (sm is T subModule)
                    result.Add(subModule);
        }

        /// <summary>
        /// 获取子模块列表（会创建新 List，产生 GC 分配）
        /// 注意：频繁调用会产生 GC 压力，推荐使用 GetSubModules(List&lt;T&gt; result) 重载
        /// </summary>
        /// <typeparam name="T">子模块类型</typeparam>
        /// <returns>子模块列表</returns>
        public List<T> GetSubModules<T>() where T : BaseSubWindow
        {
            var result = new List<T>();
            GetSubModules(result);
            return result;
        }

        /// <summary>
        /// 获取子模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSubModule<T>() where T : BaseSubWindow
        {
            if (!_subModuleKeyMap.TryGetValue(typeof(T), out var subModules))
            {
                return null;
            }

            if (subModules.Count == 0)
            {
                return null;
            }

            return subModules.First.Value as T;
        }

        /// <summary>
        /// 获取子模块
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSubModule<T>(string key) where T : BaseSubWindow
        {
            if (!_subModuleKeyMap.TryGetValue(typeof(T), out var subModules))
            {
                return null;
            }

            foreach (var sm in subModules)
            {
                if (sm.GetKey().Equals(key))
                {
                    return (T)sm;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void FindSubModules()
        {
            subModuleList ??= new List<BaseSubWindow>();
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