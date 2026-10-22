using System;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 标记仅作为设置资产配置数据、由 Helper 通过 <see cref="SettingManager"/> 直接读取的字段，
    /// 使其不参与运行时组件的字段参数映射，也不再触发“组件缺少字段”的编辑器诊断。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ComponentSettingMappingIgnoreAttribute : Attribute
    {
    }
}
