using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    
    public static class UIComponentExtension
    {
        public static Camera GetCanvasCamera(this UIComponent uiComponent)
        {
            return uiComponent.CanvasRoot.GetComponent<Canvas>().worldCamera;
        }
        
    }
}