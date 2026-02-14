using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [PreLoadZenject]
    public abstract partial class BaseSubEntity : UnityEngine.MonoBehaviour
    {
        [BoxGroup("Sub Entity Key")] [SerializeField]
        private string key;

        protected NoParamEntityLogic BaseEntityLogic { get; private set; }
        public string GetKey() => key;

        public void OnInit(NoParamEntityLogic noParamEntityLogic, object userData)
        {
            BaseEntityLogic = noParamEntityLogic;
            OnInternalInit(userData);
        }

        public void OnShow(EntityData entityData)
        {
            OnInternalShow(entityData);
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            OnInternalUpdate(elapseSeconds, realElapseSeconds);
        }

        public void OnHide(bool isShutdown, object userData)
        {
            OnInternalHide(isShutdown, userData);
        }

        public void Subscribe(EventComponent eventComponent)
        {
            OnInternalSubscribe(eventComponent);
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            OnInternalUnSubscribe(eventComponent);
        }

        public void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            OnInternalAttachChild(childEntity, parentTransform, userData);
        }

        public void InternalSetVisible(bool visible)
        {
            OnInternalSetVisible(visible);
        }

        public void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            OnInternalAttachTo(parentEntity, parentTransform, userData);
        }

        public void OnDetached(EntityLogic childEntity, object userData)
        {
            OnInternalDetachChild(childEntity, userData);
        }

        public void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            OnInternalDetachFrom(parentEntity, userData);
        }

        public void OnRecycle()
        {
            OnInternalRecycle();
        }

        public void OnRelease()
        {
            OnInternalRelease();
        }

        public void SetKey(string newKey) => key = newKey;
    }
}