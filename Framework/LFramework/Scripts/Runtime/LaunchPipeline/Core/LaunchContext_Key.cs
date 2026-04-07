using UnityEngine;

namespace LFramework.Runtime.LaunchPipeline
{
    public partial class LaunchContext
    {
        /// <summary>
        /// 自定义设备id
        /// </summary>
        public const string CustomDeviceId = "DeviceId";


        /// <summary>
        /// 获取设备唯一id
        /// </summary>
        /// <returns></returns>
        public string GetCustomDeviceId()
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