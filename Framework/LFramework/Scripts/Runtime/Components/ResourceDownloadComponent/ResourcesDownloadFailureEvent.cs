using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public class ResourcesDownloadFailureEvent : GameEventArgs<ResourcesDownloadFailureEvent>
    {
        public UpdateResultType UpdateResultType;
        public int SerialID;

        public static ResourcesDownloadFailureEvent Create(int serialID,UpdateResultType updateResultType)
        {
            var arg = ReferencePool.Acquire<ResourcesDownloadFailureEvent>();
            arg.UpdateResultType = updateResultType;
            arg.SerialID = serialID; 
            return arg;
        }

        public override void Clear()
        {
            UpdateResultType = UpdateResultType.NoneDownload;
        }
    }

}
