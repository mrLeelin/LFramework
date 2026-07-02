using System;

namespace LFramework.Runtime
{
    public interface IUILifecycle
    {
        void OnInterInit(object userData);
        void OnInterRecycle();
        void OnInterRelease();
        void OnInterOpen(object userData);
        void OnInterClose(bool isShutDown, object userData);
        void OnInterPause();
        void OnInterResume();
        void OnInterCover();
        void OnInterReveal();
        void OnInterRefocus(object userData);
        void OnInterUpdate(float elapseSeconds, float realElapseSeconds);
        void OnInterDepthChanged(int uiGroupDepth, int depthInUIGroup);
        
    }
}
