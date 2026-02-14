
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [PreLoadZenject]
    public abstract partial class NoParamEntityLogic : EntityLogic
    {
        private bool? _isUseUpdate;
        private CompositeDisposable _compositeDisposable;
        public int Id => Entity.Id;


        public sealed override bool HasUpdate
        {
            get
            {
                if (_isUseUpdate.HasValue)
                {
                    return _isUseUpdate.Value;
                }

                _isUseUpdate = NotUseUpdateHelper.IsCanUseUpdate(this.GetType());
                return _isUseUpdate.Value;
            }
        }

        public CompositeDisposable CompositeDisposable => _compositeDisposable;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            _subModuleKeyMap.Clear();
            foreach (var subModule in subModuleList)
            {
                subModule.OnInit(this, userData);
                _subModuleKeyMap.Add(subModule.GetType(),subModule);
            }
        }

        protected override void OnShow(object userData)
        {
            var entityData = userData as EntityData;
            if (entityData == null)
            {
                Log.Error("Entity data is invalid.");
                return;
            }

            if (entityData.Position.HasValue)
            {
                CachedTransform.localPosition = entityData.Position.Value;
            }

            if (entityData.Rotation.HasValue)
            {
                CachedTransform.localRotation = entityData.Rotation.Value;
            }

            if (entityData.Size.HasValue)
            {
                CachedTransform.localScale = entityData.Size.Value;
            }

            _compositeDisposable = new CompositeDisposable();
            base.OnShow(userData);
            Subscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            foreach (var subModule in subModuleList)
            {
                subModule.OnShow(entityData);
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subModule = subModuleList[index];
                subModule.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            _compositeDisposable.Dispose();
            _compositeDisposable = null;
            UnSubscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            foreach (var subModule in subModuleList)
            {
                subModule.OnHide(isShutdown, userData);
            }
        }

        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);
            foreach (var subModule in subModuleList)
            {
                subModule.OnAttached(childEntity, parentTransform, userData);
            }
        }

        protected override void InternalSetVisible(bool visible)
        {
            base.InternalSetVisible(visible);
            foreach (var subModule in subModuleList)
            {
                subModule.InternalSetVisible(visible);
            }
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
            foreach (var subModule in subModuleList)
            {
                subModule.OnAttachTo(parentEntity, parentTransform, userData);
            }
        }

        protected override void OnDetached(EntityLogic childEntity, object userData)
        {
            base.OnDetached(childEntity, userData);
            foreach (var subModule in subModuleList)
            {
                subModule.OnDetached(childEntity, userData);
            }
        }

        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            base.OnDetachFrom(parentEntity, userData);
            foreach (var subModule in subModuleList)
            {
                subModule.OnDetachFrom(parentEntity, userData);
            }
        }

        protected override void OnRecycle()
        {
            base.OnRecycle();
            foreach (var subModule in subModuleList)
            {
                subModule.OnRecycle();
            }
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            foreach (var subModule in subModuleList)
            {
                subModule.OnRelease();
            }
        }

        protected virtual void Subscribe(EventComponent eventComponent)
        {
            foreach (var subWindow in subModuleList)
            {
                subWindow.Subscribe(eventComponent);
            }
        }

        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
            foreach (var subWindow in subModuleList)
            {
                subWindow.UnSubscribe(eventComponent);
            }
        }
    }

    public abstract class EntityLogic<T> : NoParamEntityLogic where T : EntityData
    {
        [SerializeField] private T m_EntityData = null;


        public T EntityData => m_EntityData;


        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            var entityData = userData as EntityData;
            if (entityData == null)
            {
                Log.Error("Entity data is invalid.");
                return;
            }

            if (entityData is not T entity)
            {
                Log.Fatal($"Entity data '{entityData.GetType().FullName}' is not '{typeof(T).FullName}'");
                return;
            }

            m_EntityData = entity;
        }

        protected sealed override void OnShow(object userData)
        {
            var entityData = userData as EntityData;
            if (entityData == null)
            {
                Log.Error("Entity data is invalid.");
                return;
            }

            if (entityData is not T entity)
            {
                Log.Fatal($"Entity data '{entityData.GetType().FullName}' is not '{typeof(T).FullName}'");
                return;
            }

            OnVisibleBefore(entity);
            m_EntityData = entity;
            OnShow(m_EntityData);
            base.OnShow(userData);
        }

        protected virtual void OnVisibleBefore(T entityData)
        {
        }

        protected virtual void OnShow(T entityData)
        {
        }


        protected virtual void UpdateEntityData(T entityData)
        {
            m_EntityData = entityData;
        }
    }
}