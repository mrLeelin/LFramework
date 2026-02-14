using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework
{
    public static class BulkInstantiateUtility
    {
        /// <summary>
        /// 批量实例化 GameObject
        /// </summary>
        /// <param name="prefab">要实例化的 Prefab</param>
        /// <param name="count">实例化数量</param>
        /// <param name="scene">目标场景（默认当前场景）</param>
        /// <returns>返回所有新实例的 GameObject 列表</returns>
        public static List<GameObject> InstantiateMultiple(GameObject prefab, int count, UnityEngine.SceneManagement.Scene? scene = null)
        {
            if (prefab == null || count <= 0)
                return new List<GameObject>();

            int sourceId = prefab.GetInstanceID();

            var newIds = new NativeArray<int>(count, Allocator.Temp);
            var newTransformIds = new NativeArray<int>(count, Allocator.Temp);

            // 如果没有指定场景，就用当前激活场景
            var targetScene = scene ?? SceneManager.GetActiveScene();

            GameObject.InstantiateGameObjects(
                sourceId,
                count,
                newIds,
                newTransformIds,
                targetScene
            );

            // 把新实例的 ID 转换成 GameObject 引用
            var result = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var obj = Resources.InstanceIDToObject(newIds[i]) as GameObject;
                if (obj != null)
                    result.Add(obj);
            }

            newIds.Dispose();
            newTransformIds.Dispose();

            return result;
        }

        /// <summary>
        /// 批量实例化 GameObject
        /// </summary>
        /// <param name="result"></param>
        /// <param name="prefab">要实例化的 Prefab</param>
        /// <param name="count">实例化数量</param>
        /// <param name="scene">目标场景（默认当前场景）</param>
        /// <returns>返回所有新实例的 GameObject 列表</returns>
        public static void InstantiateMultiple(ref List<GameObject> result,GameObject prefab, int count, UnityEngine.SceneManagement.Scene? scene = null)
        {
            if (prefab == null || count <= 0)
                return;

            int sourceId = prefab.GetInstanceID();

            var newIds = new NativeArray<int>(count, Allocator.Temp);
            var newTransformIds = new NativeArray<int>(count, Allocator.Temp);

            // 如果没有指定场景，就用当前激活场景
            var targetScene = scene ?? SceneManager.GetActiveScene();

            GameObject.InstantiateGameObjects(
                sourceId,
                count,
                newIds,
                newTransformIds,
                targetScene
            );

            // 把新实例的 ID 转换成 GameObject 引用
            for (int i = 0; i < count; i++)
            {
                var obj = Resources.InstanceIDToObject(newIds[i]) as GameObject;
                if (obj != null)
                    result.Add(obj);
            }

            newIds.Dispose();
            newTransformIds.Dispose();
        }
    }
}