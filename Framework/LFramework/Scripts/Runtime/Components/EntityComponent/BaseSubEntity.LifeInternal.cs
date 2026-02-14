
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class BaseSubEntity
    {
        protected virtual void OnInternalInit(object userData){}
        protected virtual void OnInternalShow(EntityData entityData){}
        protected virtual void OnInternalHide(bool isShutdown, object userData){}
        protected virtual void OnInternalRecycle(){}
        protected virtual void OnInternalUpdate(float elapseSeconds, float realElapseSeconds){}
        protected virtual void OnInternalRelease(){}
        protected  virtual void OnInternalAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData){}
        protected  virtual void OnInternalDetachFrom(EntityLogic parentEntity, object userData){}
        protected  virtual void OnInternalAttachChild(EntityLogic childEntity, Transform parentTransform, object userData){}
        protected  virtual void OnInternalDetachChild(EntityLogic childEntity, object userData){}
        protected virtual void OnInternalSetVisible(bool visible){}


        protected virtual void OnInternalSubscribe(EventComponent eventComponent)
        {
        
        }
        
        protected virtual  void OnInternalUnSubscribe(EventComponent eventComponent){}
    }
}