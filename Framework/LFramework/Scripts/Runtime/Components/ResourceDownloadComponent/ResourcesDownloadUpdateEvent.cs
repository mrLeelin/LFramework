using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public class ResourcesDownloadUpdateEvent : GameEventArgs<ResourcesDownloadUpdateEvent>
    {

        public static ResourcesDownloadUpdateEvent Create(float progress)
        {
            var arg = ReferencePool.Acquire<ResourcesDownloadUpdateEvent>();
            arg.Progress = progress;
            return arg;
        }
        public static ResourcesDownloadUpdateEvent Create(float progress,string downloadSize,string totalDownloadSize,float downloadSpeed)
        {
            var arg = ReferencePool.Acquire<ResourcesDownloadUpdateEvent>();
            arg.Progress = progress;
            arg.DownloadSize = downloadSize;
            arg.TotalDownloadSize = totalDownloadSize;
            arg.DownloadSpeed = downloadSpeed;
            return arg;
        }

        public float Progress;
        public string DownloadSize;
        public string TotalDownloadSize;
        public float DownloadSpeed;
        
        
        public override void Clear()
        {
            Progress = 0F;
            DownloadSize = null;
            TotalDownloadSize = null;
            DownloadSpeed = 0F;
        }
    }

}
