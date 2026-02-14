using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Hotfix
{
    public interface ISystemProviderRegister
    {
        void TryRegisterProvider(int procedureState);

        void TryUnRegisterProvider(int procedureState);
    }
}