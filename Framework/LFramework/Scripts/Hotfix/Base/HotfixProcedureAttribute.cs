using System;
using LFramework.Runtime;

namespace LFramework.Hotfix
{
    /// <summary>
    /// 标识热更 Procedure
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HotfixProcedureAttribute : GameAttribute
    {

        public HotfixProcedureAttribute()
        {
          
        }
    }
}