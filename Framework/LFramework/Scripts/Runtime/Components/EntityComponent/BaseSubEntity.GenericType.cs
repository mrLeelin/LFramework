namespace LFramework.Runtime
{
    /// <summary>
    /// 泛型子实体基类，提供类型安全的父实体访问
    /// </summary>
    /// <typeparam name="T">父实体逻辑类型</typeparam>
    public abstract class BaseSubEntity<T> : BaseSubEntity
        where T : NoParamEntityLogic
    {
        /// <summary>
        /// 类型安全的父实体引用
        /// </summary>
        public new T BaseEntityLogic => base.BaseEntityLogic as T;

    }
}
