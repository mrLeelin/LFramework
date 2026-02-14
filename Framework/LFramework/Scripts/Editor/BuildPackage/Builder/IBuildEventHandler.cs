using System;
using System.Collections.Generic;
using LFramework.Editor.Builder;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// 生成资源事件处理函数。
    /// </summary>
    public interface IBuildEventHandler
    {
        /// <summary>
        /// 执行多个
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="action"></param>
        public static void HandleList(List<IBuildEventHandler> handlers, Action<IBuildEventHandler> action)
        {
            if (handlers == null || handlers.Count <= 0)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                action?.Invoke(handler);
            }
        }

        /// <summary>
        /// 预处理打包资源事件。
        /// </summary>
        /// <param name="mBuildData">数据</param>
        void OnPreprocessBuildApp(BuildSetting mBuildData);

        /// <summary>
        /// 执行自定义宏事件
        /// </summary>
        /// <param name="mBuildData"></param>
        /// <param name="defineList">宏的list</param>
        void OnProcessScriptingDefineSymbols(BuildSetting mBuildData, List<string> defineList);

        /// <summary>
        /// 执行打包前事件。
        /// </summary>
        /// <param name="buildResourcesData"></param>
        void OnPreprocessBuildResources(BuildResourcesData buildResourcesData);

        /// <summary>
        ///     执行打包后事件。
        /// </summary>
        /// <param name="buildResourcesData"></param>
        void OnPostprocessBuildResources(BuildResourcesData buildResourcesData);
        
        /// <summary>
        ///  执行打包后事件。
        /// </summary>
        /// <param name="mBuildData"></param>
        void OnPostprocessBuildApp(BuildSetting mBuildData);
    }
}