using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace LFramework.Editor
{
    /// <summary>
    /// 窗口自定义扩展
    /// </summary>
    public interface IGameWindowExtend
    {
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string FoldName { get; }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        IEnumerable<OdinMenuItem> Handle(OdinMenuTree tree);


    }
}