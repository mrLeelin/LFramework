using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zenject
{
    public class MonoBehaviourValidationAttribute : Attribute
    {
       
    }
    
    //这个标签不会被注入
    public class NotInjectAttribute : Attribute
    {
       
    }
}

