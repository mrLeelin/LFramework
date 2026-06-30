using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public partial class UIChildEntityLogic : EntityLogic<UIChildEntityData>
    {
        [Inject] private EntityComponent EntityComponent { get; set; }

        private Transform _originParent;
        private bool _isSetParent;

        protected override void OnShow(UIChildEntityData entityData)
        {
            base.OnShow(entityData);

            if (entityData.Parent != null)
            {
                _originParent = CachedTransform.parent;
            }

            if (entityData.DependOn == 0)
            {
                SetParent();
            }
            else
            {
                _isSetParent = false;
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (EntityData.DependOn == 0)
            {
                return;
            }

            if (_isSetParent)
            {
                return;
            }

            var flag = EntityComponent.HasEntity(EntityData.DependOn);
            if (!flag)
            {
                // If the dependent entity is not ready yet, keep the current parent.
                SetParent();
                return;
            }

            var entity = EntityComponent.GetEntity(EntityData.DependOn);
            if (entity.transform.parent == null)
            {
                return;
            }

            SetParent();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            if (_originParent != null)
            {
                CachedTransform.SetParent(_originParent);
                CachedTransform.localPosition = Vector3.zero;
                CachedTransform.localScale = Vector3.one;
                CachedTransform.rotation = Quaternion.identity;
            }
            else
            {
                CachedTransform.SetParent(null);
                //Object.DontDestroyOnLoad(CachedTransform.gameObject);
            }

            _originParent = null;
        }

        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
        }

        private void SetParent()
        {
            _isSetParent = true;
            if (EntityData.Parent == null)
            {
                return;
            }

            CachedTransform.SetParent(EntityData.Parent);
            CachedTransform.localPosition = Vector3.zero;
            CachedTransform.localScale = Vector3.one;
            CachedTransform.localRotation = Quaternion.identity;
        }
    }
}
