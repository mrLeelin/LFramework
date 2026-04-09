
using System;
using GameFramework.UI;
using UnityEngine;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public sealed class DefaultUIFormHelper : UIFormHelperBase
    {
        
        private ResourceComponent m_ResourceComponent = null;

        
        public override object InstantiateUIForm(object uiFormAsset)
        {
            return Instantiate((Object)uiFormAsset);
        }

        public override IUIForm CreateUIForm(object uiFormInstance, IUIGroup uiGroup,  bool isNewInstance,object userData)
        {
            GameObject gameObject = uiFormInstance as GameObject;
            if (gameObject == null)
            {
                Log.Error("UI form instance is invalid.");
                return null;
            }

            if (isNewInstance)
            {
                // 新实例默认是激活的，先隐藏避免进入 OnOpen / 入场动画前渲染出原始界面一帧。
                gameObject.SetActive(false);
            }

            Transform transform = gameObject.transform;
            transform.SetParent(((UnityEngine.MonoBehaviour)uiGroup.Helper).transform);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            RectTransform rect = transform.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            
            if (isNewInstance)
            {
                LFrameworkAspect.Instance.DiContainer.InjectGameObjectNotCheck(gameObject);
            }
            
            return gameObject.GetOrAddComponent<UIForm>();
        }

        public override void ReleaseUIForm(object uiFormAsset, object uiFormInstance)
        {
            var go = uiFormInstance as GameObject;
            if (go == null)
            {
                return;
            }

            var uiForm = go.GetComponent<UIForm>();
            if (uiForm != null)
            {
                uiForm.OnRelease();
            }

            if (m_ResourceComponent == null)
            {
                Log.Error("Resource component is invalid, cannot unload UI form asset.");
                return;
            }
            m_ResourceComponent.UnloadAsset(uiFormAsset);
            Destroy(go);
        }

        private void Awake()
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
