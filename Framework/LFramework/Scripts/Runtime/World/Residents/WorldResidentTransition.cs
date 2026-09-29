using System;

namespace LFramework.Runtime
{
    /// <summary>World 切换事务；提交保留目标集合，未提交释放则回滚本次新增对象。</summary>
    public sealed class WorldResidentTransition : IDisposable
    {
        private WorldResidentHost _host;

        /// <summary>是否已完成提交或回滚；清理回调抛错时也可据此区分状态是否已切换。</summary>
        public bool IsCompleted => _host == null;

        internal WorldResidentTransition(WorldResidentHost host) { _host = host; }

        /// <summary>提交前检查目标服务仍归属本次驻留对象；失败时仍可回滚。</summary>
        public void Validate()
        {
            if (_host == null) throw new InvalidOperationException("Transition has already ended.");
            _host.ValidateTransition(this);
        }

        /// <summary>World 初始化及流程注入成功后、提交前，通知目标驻留对象进入实际 World。</summary>
        public void EnterWorld(WorldBase world)
        {
            if (_host == null) throw new InvalidOperationException("Transition has already ended.");
            _host.EnterWorld(this, world);
        }

        /// <summary>在目标 World 成功进入后提交；清理异常不会撤销已提交状态。</summary>
        public void Commit()
        {
            if (_host == null) throw new InvalidOperationException("Transition has already ended.");
            _host.CompleteTransition(this, true);
        }

        /// <summary>若尚未提交，回滚新增对象并恢复原服务绑定。</summary>
        public void Dispose()
        {
            _host?.CompleteTransition(this, false);
        }

        internal void Detach() { _host = null; }
    }
}
