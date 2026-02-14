using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.UI;
using UnityEngine;

namespace LFramework.Runtime
{
    public class UIFormDepthUpdatedEventArg : GameEventArgs<UIFormDepthUpdatedEventArg>
    {

        public static UIFormDepthUpdatedEventArg Create(int serialID, string assetsName, int depth,IUIForm uiForm)
        {
            var arg = ReferencePool.Acquire<UIFormDepthUpdatedEventArg>();
            arg.SerialID = serialID;
            arg.AssetsName = assetsName;
            arg.Depth = depth;
            arg.UIForm = uiForm;
            return arg;
        }

        public IUIForm UIForm;
        
        public int SerialID;

        public string AssetsName;
        
        public int Depth;
        
        public override void Clear()
        {
            SerialID = 0;
            AssetsName = null;
            Depth = 0;
        }
    }
}

