using System;
using LFramework.Runtime;

namespace LFramework.Hotfix
{
    /// <summary>
    /// 标识热更组件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HotfixComponentAttribute : GameAttribute
    {
        public Type BindType { get; private set; }
        public HotfixComponentAttribute(Type bindType)
        {
            this.BindType = bindType;
        }
    }
}