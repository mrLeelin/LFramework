namespace LFramework.Runtime
{
    /// <summary>
    /// Implemented by generated partial classes so runtime injection can avoid reflection.
    /// </summary>
    public interface IInjectable
    {
        void Inject(IServiceResolver resolver);
    }
}
