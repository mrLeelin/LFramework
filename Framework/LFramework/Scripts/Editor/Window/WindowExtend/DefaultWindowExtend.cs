using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace LFramework.Editor
{
    public class DefaultWindowExtend  : IGameWindowExtend
    {
        public string FoldName => "默认扩展";

        public IEnumerable<OdinMenuItem> Handle(OdinMenuTree tree)
        {
            return null;
        }
        
    }
}