using System;
using LFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Inject类型  子类会自动继承
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class PreLoadZenjectAttribute : GameAttribute
    {
        public PreLoadZenjectAttribute()
        {
        }
    }
}