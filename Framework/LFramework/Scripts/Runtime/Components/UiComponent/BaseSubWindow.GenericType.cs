namespace LFramework.Runtime
{
    public abstract class BaseSubWindow<T> : BaseSubWindow
        where T : Window
    {
        protected new T BaseWindow => base.BaseWindow as T;
        
    }
}