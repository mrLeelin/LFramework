using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class BaseSubEntity : UnityEngine.MonoBehaviour
    {
        private static readonly Dictionary<Type, bool> LegacyUpdateOverrides = new();
        private static readonly object LegacyUpdateOverridesLock = new();

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

        /// <summary>
        /// 是否已实际执行事件订阅，保证取消订阅与订阅成对出现
        /// </summary>
        private bool _isSubscribed;
        private bool _isSubscriptionTransition;
        private bool _unsubscribeRequested;

        internal bool RequiresUpdate => this is ISubModuleUpdate || HasLegacyUpdateOverride(GetType());

        private static bool HasLegacyUpdateOverride(Type type)
        {
            lock (LegacyUpdateOverridesLock)
            {
                if (!LegacyUpdateOverrides.TryGetValue(type, out bool requiresUpdate))
                {
                    requiresUpdate = type.GetMethod(nameof(OnInternalUpdate),
                        BindingFlags.Instance | BindingFlags.NonPublic)?.DeclaringType != typeof(BaseSubEntity);
                    LegacyUpdateOverrides.Add(type, requiresUpdate);
                }

                return requiresUpdate;
            }
        }

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
            OnInternalHide(isShutdown, userData);
        }

        public void Subscribe(EventComponent eventComponent)
        {
            if (!IsEnabled || _isSubscribed || _isSubscriptionTransition) return;
            _isSubscribed = true;
            _isSubscriptionTransition = true;
            Exception subscribeError = null;
            try
            {
                OnInternalSubscribe(eventComponent);
            }
            catch (Exception exception)
            {
                subscribeError = exception;
            }
            finally
            {
                _isSubscriptionTransition = false;
            }

            if (subscribeError == null && !_unsubscribeRequested) return;

            _unsubscribeRequested = false;
            Exception cleanupError = null;
            try
            {
                UnSubscribe(eventComponent);
            }
            catch (Exception exception)
            {
                cleanupError = exception;
            }

            if (subscribeError != null && cleanupError != null)
                throw new AggregateException("Sub-entity subscription and cleanup both failed.", subscribeError, cleanupError);
            if (subscribeError != null) ExceptionDispatchInfo.Capture(subscribeError).Throw();
            if (cleanupError != null) ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            // Undo an attempted subscription, including a partially failed one.
            if (!_isSubscribed) return;
            if (_isSubscriptionTransition)
            {
                _unsubscribeRequested = true;
                return;
            }

            _isSubscriptionTransition = true;
            try
            {
                OnInternalUnSubscribe(eventComponent);
                _isSubscribed = false;
                _unsubscribeRequested = false;
            }
            finally
            {
                _isSubscriptionTransition = false;
            }
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
            Disposables?.Dispose();
            Disposables = null;
            OnInternalRelease();
        }

        internal void DispatchUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (this is ISubModuleUpdate update)
            {
                update.OnSubModuleUpdate(elapseSeconds, realElapseSeconds);
                return;
            }

            OnInternalUpdate(elapseSeconds, realElapseSeconds);
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
