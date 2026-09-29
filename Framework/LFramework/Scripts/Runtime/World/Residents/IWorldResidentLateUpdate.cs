namespace LFramework.Runtime
{
    /// <summary>按需实现的常驻对象帧末更新接口。</summary>
    public interface IWorldResidentLateUpdate
    {
        void LateUpdate();
    }
}
