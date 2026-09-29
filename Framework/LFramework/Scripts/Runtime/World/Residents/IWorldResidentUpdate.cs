namespace LFramework.Runtime
{
    /// <summary>按需实现的常驻对象逐帧更新接口。</summary>
    public interface IWorldResidentUpdate
    {
        void Update(float elapseSeconds, float realElapseSeconds);
    }
}
