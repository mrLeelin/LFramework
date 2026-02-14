using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    
    public interface IWorldUpdate 
    {
        void OnUpdate(float elapseSeconds, float realElapseSeconds);
    }
    
    public interface IWorldLateUpdate
    {
        void LateUpdate();
    }
}

