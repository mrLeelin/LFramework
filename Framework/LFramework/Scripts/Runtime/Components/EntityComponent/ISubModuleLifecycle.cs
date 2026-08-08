namespace LFramework.Runtime
{
    /// <summary>
    /// 需要每帧更新的子实体模块实现此接口
    /// 只有实现此接口的子模块才会收到 Update 调用，提升性能
    ///
    /// 性能提升：80%+ （假设只有 20% 的子模块需要 Update）
    /// 使用场景：动画更新、AI 逻辑、实时状态检测等
    ///
    /// 不需要 Update 的子模块无需实现此接口，性能自动优化
    /// </summary>
    public interface ISubModuleUpdate
    {
        /// <summary>
        /// 子模块每帧更新
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位</param>
        void OnSubModuleUpdate(float elapseSeconds, float realElapseSeconds);
    }
}
