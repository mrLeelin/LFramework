using System.Collections;
using System.Collections.Generic;
using GameFramework.UI;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IUIBehaviour
    {
        
        /// <summary>
        /// Ui Form
        /// </summary>
        IUIForm UIForm { get; }
        /// <summary>
        /// Transform
        /// </summary>
        Transform CacheTransform { get;  }
        /// <summary>
        /// RectTransform
        /// </summary>
        RectTransform CacheRectTransform { get; }
        
        /// <summary>
        /// 是否可用
        /// </summary>
        bool Available { get;  }
        
        /// <summary>
        /// 设置状态
        /// </summary>
        public bool Visible { get; set; }

        public void CloseSelf();
    }
}