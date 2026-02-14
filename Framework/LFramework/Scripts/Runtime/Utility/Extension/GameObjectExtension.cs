using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class GameObjectExtension
    {
        static List<Component> m_ComponentCache = new List<Component>();

        private static GameFrameworkMultiDictionary<GameObject, TrailRenderer> m_TrailRenderers =
            new GameFrameworkMultiDictionary<GameObject, TrailRenderer>();

        public static void SetLayer(this GameObject gameObject, int layer, bool isChild)
        {
            if (isChild)
            {
                gameObject.SetLayerRecursively(layer);
            }
            else
            {
                gameObject.layer = layer;
            }
            
        }

        public static T GetComponentNoAlloc<T>(this GameObject gameObject) where T : Component
        {
            gameObject.GetComponents(typeof(T), m_ComponentCache);
            Component component = m_ComponentCache.Count > 0 ? m_ComponentCache[0] : null;
            m_ComponentCache.Clear();
            return component as T;
        }

        public static void SafeSetActive(this GameObject gameObject,bool active)
        {
            if (gameObject.activeSelf == active)
            {
                return;
            }
            gameObject.SetActive(active);
        }

        public static void TrailRendererClear(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (m_TrailRenderers.Count >= 1000)
            {
                m_TrailRenderers.Clear();
            }
            
            if (!m_TrailRenderers.Contains(gameObject))
            {
                foreach (var trailRenderer in gameObject.GetComponentsInChildren<TrailRenderer>())
                {
                    m_TrailRenderers.Add(gameObject,trailRenderer);
                }
            }

            var flag = m_TrailRenderers.TryGetValue(gameObject, out var range);
            if (!flag)
            {
                return;
            }
            foreach (var trailRenderer in range )
            {
                if (trailRenderer)
                {
                    trailRenderer.Clear();
                }
            }
        }
    }
}