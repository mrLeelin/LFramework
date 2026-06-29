using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 提供版本检查任务所需的可替换配置策略。
    /// </summary>
    public interface ICheckVersionConfigProvider
    {
        /// <summary>
        /// 生成版本检查请求地址。
        /// </summary>
        /// <param name="gameSetting">游戏设置。</param>
        /// <param name="settingComponent">本地设置组件。</param>
        /// <returns>用于请求远程版本配置的 URL。</returns>
        string GetVersionUrl(GameSetting gameSetting, SettingComponent settingComponent);

        /// <summary>
        /// 将远端返回的 JSON 解析为版本配置。
        /// </summary>
        /// <param name="jsonStr">远端版本 JSON。</param>
        /// <returns>解析后的远端版本配置。</returns>
        IGameVersionConfig ParseRemoteGameVersion(string jsonStr);

        /// <summary>
        /// 根据本地设置构建客户端当前版本配置。
        /// </summary>
        /// <param name="gameSetting">游戏设置。</param>
        /// <param name="settingComponent">本地设置组件。</param>
        /// <returns>客户端当前版本配置。</returns>
        IGameVersionConfig BuildLocalGameVersion(GameSetting gameSetting, SettingComponent settingComponent);

        /// <summary>
        /// 比较远端版本和客户端版本，决定启动管线后续更新流程。
        /// </summary>
        /// <param name="remote">远端版本配置。</param>
        /// <param name="client">客户端当前版本配置。</param>
        /// <returns>版本比较结果和错误信息。</returns>
        (GameVersionCompareResult result, string errorMessage) CompareGameVersion(
            IGameVersionConfig remote,
            IGameVersionConfig client);
        
    }
}
