using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class BaseSubEntity : UnityEngine.MonoBehaviour
    {
        [BoxGroup("Sub Entity Key")] [SerializeField]
        private string key;

        protected NoParamEntityLogic BaseEntityLogic { get; private set; }

        /// <summary>
        /// 自动管理的订阅容器，用于 UniRx 订阅
        /// 在 OnRelease 时会自动释放所有订阅，防止内存泄漏
        /// </summary>
        protected CompositeDisposable Disposables { get; private set; }

        /// <summary>
        /// 子模块启用状态
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        public string GetKey() => key;

        #region Lifecycle

        public void OnInit(NoParamEntityLogic noParamEntityLogic, object userData)
        {
            BaseEntityLogic = noParamEntityLogic;

            // 防御性释放：如果被重复初始化，先释放旧的 Disposables
            Disposables?.Dispose();
            Disposables = new CompositeDisposable();

            OnInternalInit(userData);
        }

        public void OnShow(EntityData entityData)
        {
            if (!IsEnabled) return;
            OnInternalShow(entityData);
        }

        public void OnHide(bool isShutdown, object userData)
        {
            if (!IsEnabled) return;
            OnInternalHide(isShutdown, userData);
        }

        public void Subscribe(EventComponent eventComponent)
        {
            if (!IsEnabled) return;
            OnInternalSubscribe(eventComponent);
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            if (!IsEnabled) return;
            OnInternalUnSubscribe(eventComponent);
        }

        public void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            if (!IsEnabled) return;
            OnInternalAttachChild(childEntity, parentTransform, userData);
        }

        public void InternalSetVisible(bool visible)
        {
            if (!IsEnabled) return;
            OnInternalSetVisible(visible);
        }

        public void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            if (!IsEnabled) return;
            OnInternalAttachTo(parentEntity, parentTransform, userData);
        }

        public void OnDetached(EntityLogic childEntity, object userData)
        {
            if (!IsEnabled) return;
            OnInternalDetachChild(childEntity, userData);
        }

        public void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            if (!IsEnabled) return;
            OnInternalDetachFrom(parentEntity, userData);
        }

        public void OnRecycle()
        {
            OnInternalRecycle();
        }

        public void OnRelease()
        {
            Disposables?.Dispose();
            Disposables = null;
            OnInternalRelease();
        }

        #endregion

        /// <summary>
        /// 设置子模块启用状态
        /// </summary>
        /// <param name="en">是否启用</param>
        /// <param name="isEnableGameObject"></param>
        public void SetEnabled(bool en,bool isEnableGameObject = false)
        {
            if (IsEnabled == en) return;

            IsEnabled = en;
            if (isEnableGameObject)
            {
                gameObject.SetActive(en);
            }
            OnInternalEnabledChanged(en);
        }

        /// <summary>
        /// 启用状态改变时调用
        /// </summary>
        /// <param name="en">是否启用</param>
        protected virtual void OnInternalEnabledChanged(bool en)
        {
        }

        public void SetKey(string newKey) => key = newKey;
    }
}
