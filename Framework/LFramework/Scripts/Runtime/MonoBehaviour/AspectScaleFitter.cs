using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LFramework.Runtime
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class AspectScaleFitter : UnityEngine.EventSystems.UIBehaviour, ILayoutSelfController
    {
        [SerializeField] private bool isUseAspectRatioValue;


        [HideIf("isUseAspectRatioValue")] [SerializeField]
        private Vector2 height;

        [HideIf("isUseAspectRatioValue")] [SerializeField]
        private Vector2 value;

        [HideIf("isUseAspectRatioValue")] [SerializeField]
        private bool isUseSceneHeight;

        [ShowIf("isUseAspectRatioValue")] [SerializeField]
        private Vector2 aspectRatio;

        [ShowIf("isUseAspectRatioValue")] [SerializeField]
        private Vector2 aspectRatioValue;

        [System.NonSerialized] private RectTransform m_Rect;

        // This "delayed" mechanism is required for case 1014834.
        private bool m_DelayedSetDirty = false;

        //Does the gameobject has a parent for reference to enable FitToParent/EnvelopeParent modes.
        private bool m_DoesParentExist = false;

        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        // field is never assigned warning
#pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

        protected AspectScaleFitter()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        protected override void Start()
        {
            base.Start();
            //Disable the component if the aspect mode is not valid or the object state/setup is not supported with AspectRatio setup.
            if (!IsComponentValidOnObject())
                this.enabled = false;
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        /// <summary>
        /// Update the rect based on the delayed dirty.
        /// Got around issue of calling onValidate from OnEnable function.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedSetDirty)
            {
                m_DelayedSetDirty = false;
                SetDirty();
            }
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }


        private void UpdateRect()
        {
            if (!IsActive() || !IsComponentValidOnObject())
                return;

            m_Tracker.Clear();
            m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Scale);
            if (isUseAspectRatioValue)
            {
                var v = GetAspectRatio();
                if (v <= aspectRatio.x)
                {
                    rectTransform.localScale = Vector3.one * aspectRatioValue.x;
                }
                else if (v >= aspectRatio.y)
                {
                    rectTransform.localScale = Vector3.one * aspectRatioValue.y;
                }
                else
                {
                    var progress = (v - aspectRatio.x) / (aspectRatio.y - aspectRatio.x);
                    rectTransform.localScale =
                        Vector3.one * Mathf.Lerp(aspectRatioValue.x, aspectRatioValue.y, progress);
                }
            }
            else
            {
                if (GetHeight() <= height.x)
                {
                    rectTransform.localScale = Vector3.one * value.x;
                }
                else if (GetHeight() >= height.y)
                {
                    rectTransform.localScale = Vector3.one * value.y;
                }
                else
                {
                    var progress = (GetHeight() - height.x) / (height.y - height.x);
                    rectTransform.localScale = Vector3.one * Mathf.Lerp(value.x, value.y, progress);
                }
            }
        }

        private float GetHeight()
        {
            if (isUseSceneHeight)
            {
                return Screen.height;
            }

            return rectTransform.rect.height;
        }

        private float GetAspectRatio()
        {
            return Screen.height / (float)Screen.width;
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutVertical()
        {
        }

        /// <summary>
        /// Mark the AspectRatioFitter as dirty.
        /// </summary>
        protected void SetDirty()
        {
            UpdateRect();
        }

        public bool IsComponentValidOnObject()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return false;
            }

            return true;
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_DelayedSetDirty = true;
        }

#endif
    }
}