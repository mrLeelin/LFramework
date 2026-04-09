using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime.LaunchPipeline
{
    public partial class LaunchContext
    {
        /// <summary>
        /// 自定义设备id
        /// </summary>
        private const string CustomDeviceId = "DeviceId";

        private const string DownloadLabels = "DownloadLabels";


        /// <summary>
        /// 获取下载标签列表。
        /// 默认从 <see cref="LaunchContext.CustomData"/> 中以 "DownloadLabels" 键获取。
        /// </summary>
        /// <returns>下载标签列表，返回 <c>null</c> 或空列表表示无需下载。</returns>
        internal virtual List<string> GetDownloadLabels()
        {
            if (!ContainsCustomData(DownloadLabels))
            {
                return new List<string>(0);
            }

            var result = this.GetCustomData<List<string>>(DownloadLabels);
            result ??= new List<string>();
            return result;
        }

        /// <summary>
        /// 设置下载标签列表
        /// </summary>
        /// <param name="labels"></param>
        public void SetDownloadLabels(List<string> labels)
        {
            SetCustomData(DownloadLabels, labels);
        }


        /// <summary>
        /// 获取设备唯一id
        /// </summary>
        /// <returns></returns>
        internal virtual string GetCustomDeviceId()
        {
            if (!ContainsCustomData(CustomDeviceId))
            {
                return SystemInfo.deviceUniqueIdentifier;
            }

            return GetCustomData<string>(CustomDeviceId);
        }

        /// <summary>
        /// 设置设备id
        /// </summary>
        /// <param name="customDeviceId"></param>
        public void SetCustomDeviceId(string customDeviceId) => SetCustomData(CustomDeviceId, customDeviceId);
    }
}