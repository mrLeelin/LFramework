#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    public enum ProviderDiagnosticsStage
    {
        Created,
        Registered,
        Injected,
        Awakened,
        Subscribed,
        Ready,
        Stopped,
    }

    public sealed class ProviderDiagnosticsSnapshot
    {
        internal ProviderDiagnosticsSnapshot(
            SystemProviderBase instance,
            IReadOnlyList<Type> implementedInterfaces,
            int procedureState,
            ProviderLifeCycle lifeCycle,
            int sort,
            ProviderDiagnosticsStage stage,
            bool isSubscribed,
            bool overridesAwake,
            bool overridesSetUp,
            bool overridesUpdate,
            bool overridesOnStop,
            bool overridesSubscribe,
            bool overridesUnSubscribe)
        {
            Instance = instance;
            ProviderType = instance.GetType();
            ImplementedInterfaces = new List<Type>(implementedInterfaces).AsReadOnly();
            ProcedureState = procedureState;
            LifeCycle = lifeCycle;
            Sort = sort;
            Stage = stage;
            IsSubscribed = isSubscribed;
            OverridesAwake = overridesAwake;
            OverridesSetUp = overridesSetUp;
            OverridesUpdate = overridesUpdate;
            OverridesOnStop = overridesOnStop;
            OverridesSubscribe = overridesSubscribe;
            OverridesUnSubscribe = overridesUnSubscribe;
        }

        public SystemProviderBase Instance { get; }
        public Type ProviderType { get; }
        public IReadOnlyList<Type> ImplementedInterfaces { get; }
        public int ProcedureState { get; }
        public ProviderLifeCycle LifeCycle { get; }
        public int Sort { get; }
        public ProviderDiagnosticsStage Stage { get; }
        public bool IsSubscribed { get; }
        public bool OverridesAwake { get; }
        public bool OverridesSetUp { get; }
        public bool OverridesUpdate { get; }
        public bool OverridesOnStop { get; }
        public bool OverridesSubscribe { get; }
        public bool OverridesUnSubscribe { get; }
        public bool SupportsLateUpdate => false;
    }
}
#endif
