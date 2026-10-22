
using System;
using System.Runtime.ExceptionServices;
using GameFramework.UI;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public sealed class DefaultUIFormHelper : UIFormHelperBase
    {
        
        private ResourceComponent m_ResourceComponent = null;
        // Keep the original release owner available for loads completing after component shutdown.
        private IResourceHelper m_ResourceHelper = null;

        
        public override object InstantiateUIForm(object uiFormAsset)
        {
            return Instantiate((Object)uiFormAsset);
        }

        public override IUIForm CreateUIForm(object uiFormInstance, IUIGroup uiGroup,  bool isNewInstance,object userData)
        {
            if (m_ResourceHelper == null && m_ResourceComponent != null)
            {
                m_ResourceHelper = m_ResourceComponent.ResourceHelper;
            }

            GameObject gameObject = uiFormInstance as GameObject;
            if (gameObject == null)
            {
                Log.Error("UI form instance is invalid.");
                return null;
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
                gameObject.SetActive(false);
                InjectUnity.InjectGameObjectNotCheck(gameObject);
            }
            
            return gameObject.GetOrAddComponent<UIForm>();
        }

        public override void ReleaseUIForm(object uiFormAsset, object uiFormInstance)
        {
            var go = uiFormInstance as GameObject;
            var uiForm = go != null ? go.GetComponent<UIForm>() : null;
            Exception releaseException = null;
            try
            {
                uiForm?.OnRelease();
            }
            catch (Exception exception)
            {
                releaseException = exception;
            }

            try
            {
                if (m_ResourceHelper != null)
                {
                    m_ResourceHelper.Release(uiFormAsset);
                }
                else if (m_ResourceComponent == null)
                {
                    Log.Error("Resource component is invalid, cannot unload UI form asset.");
                }
                else
                {
                    m_ResourceComponent.UnloadAsset(uiFormAsset);
                }
            }
            catch (Exception exception)
            {
                releaseException = releaseException == null
                    ? exception
                    : new AggregateException("UI form lifecycle and resource release both failed.",
                        releaseException, exception);
            }
            finally
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(go);
                    }
                    else
                    {
                        DestroyImmediate(go);
                    }
                }
            }

            if (releaseException != null)
            {
                ExceptionDispatchInfo.Capture(releaseException).Throw();
            }
        }

        private void Awake()
        {
            m_ResourceComponent = LFrameworkAspect.Instance.Get<ResourceComponent>();
            m_ResourceHelper = m_ResourceComponent?.ResourceHelper;
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }
        }
    }
}
