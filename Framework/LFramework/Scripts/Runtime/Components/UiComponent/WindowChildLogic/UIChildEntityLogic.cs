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

            if (CanSetParent(entityData.DependOn, false, false))
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
            var dependencyHasParent = false;
            if (flag)
            {
                var entity = EntityComponent.GetEntity(EntityData.DependOn);
                dependencyHasParent = entity != null && entity.transform.parent != null;
            }

            if (!CanSetParent(EntityData.DependOn, flag, dependencyHasParent))
            {
                return;
            }

            SetParent();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
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
            }

            _originParent = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            //让父类不执行SetParent
            //让SubModule正常执行
            //parentTransform is null.
            base.OnAttachTo(parentEntity, null, userData);
        }

        /// <summary>
        /// 判断子实体是否可以设置父节点。
        /// </summary>
        /// <param name="dependOn">依赖实体编号，0 表示不等待依赖。</param>
        /// <param name="dependencyExists">依赖实体是否已存在。</param>
        /// <param name="dependencyHasParent">依赖实体是否已经挂到父节点。</param>
        /// <returns>是否可以设置父节点。</returns>
        public static bool CanSetParent(int dependOn, bool dependencyExists, bool dependencyHasParent)
        {
            return dependOn == 0 || dependencyExists && dependencyHasParent;
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
