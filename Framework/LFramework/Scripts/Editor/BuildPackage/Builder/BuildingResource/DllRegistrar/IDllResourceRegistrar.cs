using System.Collections.Generic;
using LFramework.Runtime.Settings;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// DLL 资源注册器接口
    /// 抽象不同资源系统的 DLL 注册行为，支持 Addressable 和 YooAsset 等资源管理系统
    /// </summary>
    public interface IDllResourceRegistrar
    {
        /// <summary>
        /// 将 AOT DLL 文件注册到资源系统
        /// </summary>
        /// <param name="dllPaths">DLL 文件的完整路径列表</param>
        /// <param name="setting">HybridCLR 配置</param>
        /// <returns>注册是否成功</returns>
        bool RegisterAotDlls(List<string> dllPaths, HybridCLRSetting setting);

        /// <summary>
        /// 将 Hotfix DLL 文件注册到资源系统
        /// </summary>
        /// <param name="dllPaths">DLL 文件的完整路径列表</param>
        /// <param name="setting">HybridCLR 配置</param>
        /// <returns>注册是否成功</returns>
        bool RegisterHotfixDlls(List<string> dllPaths, HybridCLRSetting setting);

        /// <summary>
        /// 确保资源系统中存在指定的资源分组
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>分组是否存在或创建成功</returns>
        bool EnsureGroupExists(string groupName);
    }
}
