using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LFramework.Runtime
{
    /// <summary>
    /// Window 子实体兼容扩展入口。
    /// </summary>
    public static class WindowChildExtensions
    {
        /// <summary>
        /// 获取当前 Window 已登记的子实体数量。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <returns>已登记的子实体数量。</returns>
        public static int GetChildCount(this Window window)
        {
            return window != null ? window.GetChildCount() : 0;
        }

        /// <summary>
        /// 获取当前 Window 已登记的子实体编号范围。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <returns>子实体编号范围；没有子实体时返回默认范围。</returns>
        public static IReadOnlyList<int> GetChildren(this Window window)
        {
            return window != null ? window.GetChildren() : null;
        }

        /// <summary>
        /// 同步加载并登记一个 Window 子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <param name="param">子实体加载参数。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>成功发起加载的实体编号；参数无效或 Window 无效时返回 0。</returns>
        public static int AddChild<TLogic>(this Window window, AddChildParam param)
            where TLogic : UIChildEntityLogic
        {
            return window != null ? window.AddChild<TLogic>(param) : 0;
        }

        /// <summary>
        /// 异步加载并登记一个 Window 子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <param name="param">子实体加载参数。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>加载完成后的子实体逻辑；参数无效、加载期间被卸载或实体不存在时返回 null。</returns>
        public static UniTask<TLogic> AddChildAsync<TLogic>(this Window window, AddChildParam param)
            where TLogic : UIChildEntityLogic
        {
            return window != null ? window.AddChildAsync<TLogic>(param) : default;
        }

        /// <summary>
        /// 异步加载并登记一个 Window 子实体。
        /// </summary>
        /// <remarks>
        /// 旧签名保留用于兼容历史调用。实际参数以 <paramref name="param"/> 为准。
        /// </remarks>
        /// <param name="window">所属 Window。</param>
        /// <param name="parent">兼容参数，已由 <paramref name="param"/> 承载。</param>
        /// <param name="assetsPath">兼容参数，已由 <paramref name="param"/> 承载。</param>
        /// <param name="param">子实体加载参数。</param>
        /// <param name="userData">兼容参数，已由 <paramref name="param"/> 承载。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>加载完成后的子实体逻辑；参数无效、加载期间被卸载或实体不存在时返回 null。</returns>
        public static UniTask<TLogic> AddChildAsync<TLogic>(this Window window, Transform parent, string assetsPath,
            AddChildParam param, object userData)
            where TLogic : UIChildEntityLogic
        {
            return window != null ? window.AddChildAsync<TLogic>(parent, assetsPath, param, userData) : default;
        }

        /// <summary>
        /// 卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <param name="id">子实体编号。</param>
        public static void RemoveChild(this Window window, int id)
        {
            window?.RemoveChild(id);
        }

        /// <summary>
        /// 批量卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <param name="id">子实体编号列表。</param>
        public static void RemoveChild(this Window window, List<int> id)
        {
            window?.RemoveChild(id);
        }

        /// <summary>
        /// 批量卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        /// <param name="id">子实体编号集合。</param>
        public static void RemoveChild(this Window window, HashSet<int> id)
        {
            window?.RemoveChild(id);
        }

        /// <summary>
        /// 卸载当前 Window 持有的全部子实体。
        /// </summary>
        /// <param name="window">所属 Window。</param>
        public static void RemoveChild(this Window window)
        {
            window?.RemoveChild();
        }
    }
}
