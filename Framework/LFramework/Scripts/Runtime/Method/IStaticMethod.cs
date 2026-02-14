using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime.Method
{
    public interface IStaticMethod
    {
         void Run();
        
         void Run(object param1);
        
         void Run(object param1 ,object param2);
        
         void Run(object param1,object param2,object param3);
    }
}

