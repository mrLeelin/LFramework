using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class DefaultUIGroupHelper : UIGroupHelperBase
    {

        public const int DepthFactor = 1;

        private int m_Depth = 0;
        private Canvas m_CachedCanvas = null;
        private Canvas m_ParentCanvas = null;


        /// <summary>
        /// 设置界面组深度。
        /// </summary>
        /// <param name="depth">界面组深度。</param>
        public override void SetDepth(int depth)
        {
            m_Depth = depth;
            m_CachedCanvas.overrideSorting = true;
            m_CachedCanvas.sortingOrder = DepthFactor * depth;
        }

        private void Awake()
        {
            m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
            gameObject.GetOrAddComponent<GraphicRaycaster>();
        }

        private void Start()
        {
            m_ParentCanvas = transform.parent.GetComponent<Canvas>();
            m_CachedCanvas.overrideSorting = true;
            m_CachedCanvas.sortingOrder = DepthFactor * m_Depth;
            m_CachedCanvas.sortingLayerName = m_ParentCanvas.sortingLayerName;
            //m_CachedCanvas.vertexColorAlwaysGammaSpace = true;
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}