using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IViewModel : IReference
    {
        /// <summary>
        /// The identifier in (ViewModel & View)
        /// </summary>
        public string Identifier { get; set; }
        
        /// <summary>
        /// The exist view in view model
        /// </summary>
        public int References { get; set; }

        /// <summary>
        /// 执行绑定
        /// </summary>
        internal void Initialize();

    }
}