using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    /// <summary>
    /// Singleton class manager
    /// </summary>
    public static class SingletonManager
    {
        private static readonly Dictionary<Type, ISingleton> SingletonTypes = new();
        private static readonly Stack<ISingleton> Singletons = new();
        private static readonly Queue<ISingleton> Updates = new();
        private static readonly Queue<ISingleton> LateUpdates = new();


        /// <summary>
        /// 添加单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AddSingleton<T>() where T : Singleton<T>, new()
        {
            var singleton = new T();
            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 添加单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AddSingletonMonoBehaviour<T>() where T : SingletonMonoBehaviour<T>, new()
        {
            var singleton = new GameObject($"[Singleton : {nameof(T)}]")
                .AddComponent<T>();
            Object.DontDestroyOnLoad(singleton.gameObject);
            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 清空所有单例
        /// </summary>
        public static void Close()
        {
            // 顺序反过来清理
            while (Singletons.Count > 0)
            {
                ISingleton iSingleton = Singletons.Pop();
                iSingleton.Destroy();
            }

            SingletonTypes.Clear();
        }
        
        /// <summary>
        /// 添加单例
        /// </summary>
        /// <param name="singleton"></param>
        /// <exception cref="Exception"></exception>
        public static void AddSingleton(ISingleton singleton)
        {
            var singletonType = singleton.GetType();
            if (SingletonTypes.ContainsKey(singletonType))
            {
                throw new Exception($"already exist singleton: {singletonType.Name}");
            }

            SingletonTypes.Add(singletonType, singleton);
            Singletons.Push(singleton);

            singleton.Register();

            if (singleton is ISingletonUpdate)
            {
                Updates.Enqueue(singleton);
            }

            if (singleton is ISingletonLateUpdate)
            {
                LateUpdates.Enqueue(singleton);
            }
        }

        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            int count = Updates.Count;
            while (count-- > 0)
            {
                ISingleton singleton = Updates.Dequeue();

                if (singleton.IsDisposed())
                {
                    continue;
                }

                if (singleton is not ISingletonUpdate update)
                {
                    continue;
                }

                Updates.Enqueue(singleton);
                try
                {
                    update.Update(elapseSeconds, realElapseSeconds);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public static void LateUpdate()
        {
            int count = LateUpdates.Count;
            while (count-- > 0)
            {
                ISingleton singleton = LateUpdates.Dequeue();

                if (singleton.IsDisposed())
                {
                    continue;
                }

                if (singleton is not ISingletonLateUpdate lateUpdate)
                {
                    continue;
                }

                LateUpdates.Enqueue(singleton);
                try
                {
                    lateUpdate.LateUpdate();
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                }
            }
        }
    }
}