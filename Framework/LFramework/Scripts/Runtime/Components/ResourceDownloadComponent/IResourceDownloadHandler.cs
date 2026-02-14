using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IResourceDownloadHandler
    {
        /// <summary>
        /// 下载器名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 唯一Id
        /// </summary>
        public int SerialID { get; }

        /// <summary>
        /// 下载速度
        /// </summary>
        public float DownloadSpeed { get; }

        /// <summary>
        /// 下载器状态
        /// </summary>
        event EventHandler<ResourceDownloadStepEvent> DownloadStepEventHandler;

        /// <summary>
        /// 失败回调
        /// </summary>
        event EventHandler<ResourcesDownloadFailureEvent> DownloadFailureEventHandler;

        /// <summary>
        /// 成功回调
        /// </summary>
        event EventHandler<ResourcesDownloadSuccessfulEvent> DownloadSuccessfulEventHandler;

        /// <summary>
        /// 更新事件
        /// </summary>
        event EventHandler<ResourcesDownloadUpdateEvent> DownloadUpdateEventHandler;

        /// <summary>
        /// 开始检查更新
        /// </summary>
        void CheckAndLoadAsync();
    }
}