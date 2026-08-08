namespace LFramework.Runtime
{
    /// <summary>
    /// 泛型子窗口基类，提供类型安全的父窗口访问
    /// </summary>
    /// <typeparam name="T">父窗口类型</typeparam>
    public abstract class BaseSubWindow<T> : BaseSubWindow
        where T : Window
    {
        /// <summary>
        /// 类型安全的父窗口引用
        /// </summary>
        protected new T BaseWindow => base.BaseWindow as T;

        /// <summary>
        /// 便捷方法：获取父窗口的 ViewModel（如果父窗口是 Window&lt;TViewModel&gt;）
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel 类型</typeparam>
        /// <returns>ViewModel 实例，如果不匹配则返回 null</returns>
        protected TViewModel GetViewModel<TViewModel>()
            where TViewModel : class, IViewModel
        {
            if (BaseWindow is Window<TViewModel> window)
            {
                return window.ViewModel;
            }

#if UNITY_EDITOR || DEBUG
            UnityEngine.Debug.LogWarning(
                $"[{GetType().Name}] GetViewModel<{typeof(TViewModel).Name}> failed. " +
                $"Window type is {BaseWindow?.GetType().Name}. " +
                $"Make sure the Window is of type Window<{typeof(TViewModel).Name}>.");
#endif
            return null;
        }
    }
}