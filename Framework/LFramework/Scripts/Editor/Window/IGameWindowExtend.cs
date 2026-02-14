using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace LFramework.Editor
{
    public interface IGameWindowExtend
    {
        IEnumerable<OdinMenuItem> Handle(OdinMenuTree tree);
    }

}
