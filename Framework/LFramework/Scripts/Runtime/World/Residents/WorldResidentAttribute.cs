using System;

namespace LFramework.Runtime
{
    /// <summary>声明常驻对象适用的 World 类型；范围内切换保留实例，离开范围释放。</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class WorldResidentAttribute : GameAttribute
    {
        public WorldResidentAttribute(params Type[] worldTypes)
        {
            WorldTypes = worldTypes == null ? Array.Empty<Type>() : (Type[])worldTypes.Clone();
        }

        /// <summary>精确匹配的 World 类型，不自动包含派生类型。</summary>
        public Type[] WorldTypes { get; }

        /// <summary>初始化和更新顺序，数值较大者优先；关闭顺序相反。</summary>
        public int Sort { get; set; }
    }
}
