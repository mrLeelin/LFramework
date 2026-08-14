#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;

namespace LFramework.Runtime
{
    public abstract partial class SystemProviderBase
    {
        private int _diagnosticsProcedureState;
        private ProviderLifeCycle _diagnosticsLifeCycle;
        private int _diagnosticsSort;
        private ProviderDiagnosticsStage _diagnosticsStage;

        public ProviderDiagnosticsSnapshot GetDiagnosticsSnapshot()
        {
            Type providerType = GetType();
            Type[] interfaces = providerType.GetInterfaces()
                .Where(type => type != typeof(ISystemProvider))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            return new ProviderDiagnosticsSnapshot(
                this,
                interfaces,
                _diagnosticsProcedureState,
                _diagnosticsLifeCycle,
                _diagnosticsSort,
                _diagnosticsStage,
                _isSubscribed,
                Overrides(providerType, nameof(AwakeComponent), BindingFlags.Instance | BindingFlags.Public),
                Overrides(providerType, nameof(SetUp), BindingFlags.Instance | BindingFlags.Public),
                Overrides(
                    providerType,
                    nameof(UpdateComponent),
                    BindingFlags.Instance | BindingFlags.Public,
                    typeof(float),
                    typeof(float)),
                Overrides(providerType, nameof(OnStop), BindingFlags.Instance | BindingFlags.Public),
                Overrides(
                    providerType,
                    nameof(Subscribe),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    typeof(UnityGameFramework.Runtime.EventComponent)),
                Overrides(
                    providerType,
                    nameof(UnSubscribe),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    typeof(UnityGameFramework.Runtime.EventComponent)));
        }

        public void SetDiagnosticsRegistration(int procedureState, ProviderLifeCycle lifeCycle, int sort)
        {
            _diagnosticsProcedureState = procedureState;
            _diagnosticsLifeCycle = lifeCycle;
            _diagnosticsSort = sort;
        }

        public void SetDiagnosticsStage(ProviderDiagnosticsStage stage)
        {
            _diagnosticsStage = stage;
        }

        private static bool Overrides(
            Type providerType,
            string methodName,
            BindingFlags bindingFlags,
            params Type[] parameterTypes)
        {
            MethodInfo method = providerType.GetMethod(
                methodName,
                bindingFlags,
                null,
                parameterTypes ?? Type.EmptyTypes,
                null);
            return method != null && method.DeclaringType != typeof(SystemProviderBase);
        }
    }
}
#endif
