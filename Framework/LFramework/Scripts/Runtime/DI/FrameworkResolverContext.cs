using VContainer;

namespace LFramework.Runtime
{
    /// <summary>
    /// Tracks the currently active VContainer resolvers for framework runtime flows.
    /// </summary>
    public sealed class FrameworkResolverContext
    {
        public IObjectResolver RootResolver { get; private set; }
        public IObjectResolver HotfixResolver { get; private set; }
        public IObjectResolver ProcedureResolver { get; private set; }

        public IObjectResolver ActiveResolver => ProcedureResolver ?? HotfixResolver ?? RootResolver;

        public void SetRoot(IObjectResolver resolver)
        {
            RootResolver = resolver;
        }

        public void SetHotfix(IObjectResolver resolver)
        {
            HotfixResolver = resolver;
        }

        public void SetProcedure(IObjectResolver resolver)
        {
            ProcedureResolver = resolver;
        }

        public void ClearProcedure()
        {
            ProcedureResolver = null;
        }

        public void ClearHotfix()
        {
            HotfixResolver = null;
        }
    }
}
