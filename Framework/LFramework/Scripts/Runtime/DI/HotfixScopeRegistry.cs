using System;
using VContainer;

namespace LFramework.Runtime
{
    /// <summary>
    /// Owns the active hotfix scope and updates the resolver context.
    /// </summary>
    public sealed class HotfixScopeRegistry
    {
        private readonly FrameworkResolverContext _resolverContext;
        private IObjectResolver _currentHotfixScope;

        public HotfixScopeRegistry(FrameworkResolverContext resolverContext)
        {
            _resolverContext = resolverContext ?? throw new ArgumentNullException(nameof(resolverContext));
        }

        public IObjectResolver EnterHotfixScope(Action<IContainerBuilder> installation = null)
        {
            ExitHotfixScope();

            if (_resolverContext.RootResolver == null)
            {
                throw new InvalidOperationException("Cannot enter a hotfix scope without a root resolver.");
            }

            _currentHotfixScope = _resolverContext.RootResolver.CreateScope(installation);
            _resolverContext.SetHotfix(_currentHotfixScope);
            return _currentHotfixScope;
        }

        public void ExitHotfixScope()
        {
            _currentHotfixScope?.Dispose();
            _currentHotfixScope = null;
            _resolverContext.ClearHotfix();
        }
    }
}
