using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface ISingletonUpdate
    {

        void Update(float elapseSeconds, float realElapseSeconds);
    }
}