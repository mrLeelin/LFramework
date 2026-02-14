using System;

namespace LFramework.Runtime
{
    public interface IUILifecycle
    {
        void OnInit(object userData);
        void OnRecycle();
        void OnRelease();
        void OnOpen(object userData);
        void OnClose(bool isShutDown, object userData);
        void OnPause();
        void OnResume();
        void OnCover();
        void OnReveal();
        void OnRefocus(object userData);
        void OnUpdate(float elapseSeconds, float realElapseSeconds);
        void OnDepthChanged(int uiGroupDepth, int depthInUIGroup);
        
    }
}