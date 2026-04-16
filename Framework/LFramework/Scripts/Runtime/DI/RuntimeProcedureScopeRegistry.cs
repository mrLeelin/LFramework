using System;
using VContainer;

namespace LFramework.Runtime
{
    /// <summary>
    /// Owns the current runtime procedure scope and updates the active resolver context.
    /// </summary>
    public sealed class RuntimeProcedureScopeRegistry
    {
        private readonly FrameworkResolverContext _resolverContext;
        private IObjectResolver _currentProcedureScope;

        public RuntimeProcedureScopeRegistry(FrameworkResolverContext resolverContext)
        {
            _resolverContext = resolverContext ?? throw new ArgumentNullException(nameof(resolverContext));
        }

        public IObjectResolver EnterProcedureScope(object owner, Action<IContainerBuilder> installation = null)
        {
            ExitProcedureScope();

            var parentResolver = _resolverContext.HotfixResolver ?? _resolverContext.RootResolver;
            if (parentResolver == null)
            {
                throw new InvalidOperationException("Cannot enter a procedure scope without a root or hotfix resolver.");
            }

            _currentProcedureScope = parentResolver.CreateScope(installation ?? (_ => { }));
            _resolverContext.SetProcedure(_currentProcedureScope);
            return _currentProcedureScope;
        }

        public void ExitProcedureScope()
        {
            _currentProcedureScope?.Dispose();
            _currentProcedureScope = null;
            _resolverContext.ClearProcedure();
        }
    }
}
