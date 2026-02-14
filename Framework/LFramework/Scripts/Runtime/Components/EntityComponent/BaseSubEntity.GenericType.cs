namespace LFramework.Runtime
{

    public abstract class BaseSubEntity<T> : BaseSubEntity
        where T : NoParamEntityLogic
    {
        
        public new T BaseEntityLogic => base.BaseEntityLogic as T;
        
    }
}