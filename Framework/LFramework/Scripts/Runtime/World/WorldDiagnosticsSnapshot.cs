#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    public sealed class WorldDiagnosticsSnapshot
    {
        internal WorldDiagnosticsSnapshot(
            WorldBase instance,
            Type linkedProcedureType,
            IReadOnlyList<WorldHelperDiagnostics> helpers)
        {
            Instance = instance;
            WorldType = instance.GetType();
            LinkedProcedureType = linkedProcedureType;
            Helpers = new List<WorldHelperDiagnostics>(helpers).AsReadOnly();
        }

        public WorldBase Instance { get; }
        public Type WorldType { get; }
        public Type LinkedProcedureType { get; }
        public IReadOnlyList<WorldHelperDiagnostics> Helpers { get; }
    }

    public sealed class WorldHelperDiagnostics
    {
        internal WorldHelperDiagnostics(
            IWorldHelper instance,
            Type serviceInterface,
            IReadOnlyList<Type> registrationTypes,
            IReadOnlyList<Type> implementedInterfaces,
            bool receivesUpdate,
            bool receivesLateUpdate,
            bool isAutoRegistered)
        {
            Instance = instance;
            HelperType = instance.GetType();
            ServiceInterface = serviceInterface;
            RegistrationTypes = new List<Type>(registrationTypes).AsReadOnly();
            ImplementedInterfaces = new List<Type>(implementedInterfaces).AsReadOnly();
            ReceivesUpdate = receivesUpdate;
            ReceivesLateUpdate = receivesLateUpdate;
            IsAutoRegistered = isAutoRegistered;
        }

        public IWorldHelper Instance { get; }
        public Type HelperType { get; }
        public Type ServiceInterface { get; }
        public IReadOnlyList<Type> RegistrationTypes { get; }
        public IReadOnlyList<Type> ImplementedInterfaces { get; }
        public bool ReceivesUpdate { get; }
        public bool ReceivesLateUpdate { get; }
        public bool IsAutoRegistered { get; }
    }
}
#endif
