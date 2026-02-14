using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{


    public class ResourceDownloadStepEvent :  GameEventArgs<ResourcesDownloadSuccessfulEvent>
    {
      
        public ResourceDownloadStep Step;
        public object CustomData;
        public int SerialID;
        
        public static ResourceDownloadStepEvent Create(int serialID,ResourceDownloadStep step,object customData)
        {
            var arg = ReferencePool.Acquire<ResourceDownloadStepEvent>();
            arg.Step = step;
            arg.CustomData = customData;
            arg.SerialID = serialID; 
            return arg;
        }
  

        public override void Clear()
        {
            CustomData = null;
            SerialID = 0;
        }
    }

}
