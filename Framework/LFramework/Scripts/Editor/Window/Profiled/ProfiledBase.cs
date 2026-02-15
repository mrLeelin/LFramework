using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal abstract class ProfiledBase
    {

        internal virtual string Title { get; } = "";

        internal virtual string SubTitle { get; } = "";
        
        internal abstract bool CanDraw { get; }

        /// <summary>
        /// Draw
        /// </summary>
        internal abstract void Draw();


        protected void GetComponent<T>(ref T instance) where T : GameFrameworkComponent
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }
            if (instance != null)
            {
                return;
            }

            instance = LFrameworkAspect.Instance.Get<T>();
        }
    }
}

