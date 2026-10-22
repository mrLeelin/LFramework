namespace LFramework.Hotfix
{
    /// <summary>由流程入口驱动的跨 World 驻留对象切换。</summary>
    public interface IWorldResidentRegister
    {
        /// <summary>在 Provider 和 World 注入前准备目标流程的驻留对象。</summary>
        void PrepareWorldResidents(int procedureState);

        /// <summary>新流程进入成功后提交，释放不再适用的驻留对象；提交前失败时事务保留，由调用方调用 CancelWorldResidentTransition 回滚。</summary>
        void CompleteWorldResidentTransition();

        /// <summary>旧 World 和 Provider 清理前通知驻留对象退出；失败时可重试。</summary>
        void ExitWorldResidents();

        /// <summary>进入失败时清理本次新增对象，恢复原驻留服务。</summary>
        void CancelWorldResidentTransition();
    }
}
