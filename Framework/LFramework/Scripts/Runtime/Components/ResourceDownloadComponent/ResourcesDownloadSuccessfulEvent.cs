using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public class ResourcesDownloadSuccessfulEvent : GameEventArgs<ResourcesDownloadSuccessfulEvent>
    {

        public static ResourcesDownloadSuccessfulEvent Create(int id)
        {
            var arg = ReferencePool.Acquire<ResourcesDownloadSuccessfulEvent>();
            arg.SerialID = id;
            return arg;
        }

        public int SerialID;
        
        public override void Clear()
        {
            SerialID = 0;
        }
    }

}
