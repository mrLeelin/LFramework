using System.Collections;
using System.Collections.Generic;
using GameFramework.Entity;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class DefaultEntityHelper : EntityHelperBase
    {
        
        protected ResourceComponent m_ResourceComponent = null;

        
        public override object InstantiateEntity(object entityAsset)
        {
            var obj = Instantiate((Object)entityAsset);
            return obj;
        }

        public override IEntity CreateEntity(object entityInstance, IEntityGroup entityGroup, object userData)
        {
            GameObject go = entityInstance as GameObject;
            if (go == null)
            {
                Log.Error("Entity instance is invalid.");
                return null;
            }
            
            go.transform.SetParent(((UnityEngine.MonoBehaviour)entityGroup.Helper).transform);
            //Object.DontDestroyOnLoad(go);
            return go.GetOrAddComponent<Entity>();
        }

        public override void ReleaseEntity(object entityAsset, object entityInstance)
        {
            var go = entityInstance as GameObject;
            if (go == null)
            {
                return;
            }

            var entity = go.GetComponent<Entity>();
            if (entity)
            {
                entity.OnRelease();
            }
            m_ResourceComponent.UnloadAsset(entityAsset);
            Destroy(go);
        }

        public override void BeforeOnInitEntity(object entityInstance)
        {
            var go = entityInstance as GameObject;
            if (go == null)
            {
                Log.Fatal("Before on init 'entityInstance' is not 'GameObject'");
            }
            LFrameworkAspect.Instance.DiContainer.InjectGameObjectNotCheck(go);
        }

        private void Start()
        {
            m_ResourceComponent = LFrameworkAspect.Instance.Get<ResourceComponent>();
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }
        }
    }
}

