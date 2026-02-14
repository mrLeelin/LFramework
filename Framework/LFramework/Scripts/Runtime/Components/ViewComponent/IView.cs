using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IView
    {
        /// <summary>
        /// The identifier in (ViewModel & View)
        /// </summary>
        string Identifier { get; set; }

        /// <summary>
        /// View model
        /// </summary>
        IViewModel ViewModelObject { get; set; }

        /// <summary>
        /// The type of view model
        /// </summary>
        Type ViewModelType { get; }

        void OnViewBeCreate();
        
        void OnViewBeDestroy();
    }
}