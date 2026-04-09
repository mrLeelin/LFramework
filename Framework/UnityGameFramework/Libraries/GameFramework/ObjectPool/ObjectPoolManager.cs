//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.ObjectPool
{
    /// <summary>
    /// 瀵硅薄姹犵鐞嗗櫒銆?
    /// </summary>
    [Preserve]
    internal sealed partial class ObjectPoolManager : GameFrameworkModule, IObjectPoolManager
    {
        private const int DefaultCapacity = int.MaxValue;
        private const float DefaultExpireTime = float.MaxValue;
        private const int DefaultPriority = 0;

        private readonly Dictionary<TypeNamePair, ObjectPoolBase> m_ObjectPools;
        private readonly List<ObjectPoolBase> m_CachedAllObjectPools;
        private readonly Comparison<ObjectPoolBase> m_ObjectPoolComparer;

        /// <summary>
        /// 鍒濆鍖栧璞℃睜绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public ObjectPoolManager()
        {
            m_ObjectPools = new Dictionary<TypeNamePair, ObjectPoolBase>();
            m_CachedAllObjectPools = new List<ObjectPoolBase>();
            m_ObjectPoolComparer = ObjectPoolComparer;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return 6;
            }
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犳暟閲忋€?
        /// </summary>
        public int Count
        {
            get
            {
                return m_ObjectPools.Count;
            }
        }

        /// <summary>
        /// 瀵硅薄姹犵鐞嗗櫒杞銆?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                objectPool.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗗璞℃睜绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                objectPool.Value.Shutdown();
            }

            m_ObjectPools.Clear();
            m_CachedAllObjectPools.Clear();
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄥ璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <returns>鏄惁瀛樺湪瀵硅薄姹犮€?/returns>
        public bool HasObjectPool<T>() where T : ObjectBase
        {
            return InternalHasObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄥ璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <returns>鏄惁瀛樺湪瀵硅薄姹犮€?/returns>
        public bool HasObjectPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalHasObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄥ璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>鏄惁瀛樺湪瀵硅薄姹犮€?/returns>
        public bool HasObjectPool<T>(string name) where T : ObjectBase
        {
            return InternalHasObjectPool(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄥ璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>鏄惁瀛樺湪瀵硅薄姹犮€?/returns>
        public bool HasObjectPool(Type objectType, string name)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalHasObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄥ璞℃睜銆?
        /// </summary>
        /// <param name="condition">瑕佹鏌ョ殑鏉′欢銆?/param>
        /// <returns>鏄惁瀛樺湪瀵硅薄姹犮€?/returns>
        public bool HasObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null)
            {
                throw new GameFrameworkException("Condition is invalid.");
            }

            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                if (condition(objectPool.Value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public IObjectPool<T> GetObjectPool<T>() where T : ObjectBase
        {
            return (IObjectPool<T>)InternalGetObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public ObjectPoolBase GetObjectPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalGetObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public IObjectPool<T> GetObjectPool<T>(string name) where T : ObjectBase
        {
            return (IObjectPool<T>)InternalGetObjectPool(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public ObjectPoolBase GetObjectPool(Type objectType, string name)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalGetObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <param name="condition">瑕佹鏌ョ殑鏉′欢銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public ObjectPoolBase GetObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null)
            {
                throw new GameFrameworkException("Condition is invalid.");
            }

            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                if (condition(objectPool.Value))
                {
                    return objectPool.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <param name="condition">瑕佹鏌ョ殑鏉′欢銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀵硅薄姹犮€?/returns>
        public ObjectPoolBase[] GetObjectPools(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null)
            {
                throw new GameFrameworkException("Condition is invalid.");
            }

            List<ObjectPoolBase> results = new List<ObjectPoolBase>();
            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                if (condition(objectPool.Value))
                {
                    results.Add(objectPool.Value);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// 鑾峰彇瀵硅薄姹犮€?
        /// </summary>
        /// <param name="condition">瑕佹鏌ョ殑鏉′欢銆?/param>
        /// <param name="results">瑕佽幏鍙栫殑瀵硅薄姹犮€?/param>
        public void GetObjectPools(Predicate<ObjectPoolBase> condition, List<ObjectPoolBase> results)
        {
            if (condition == null)
            {
                throw new GameFrameworkException("Condition is invalid.");
            }

            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                if (condition(objectPool.Value))
                {
                    results.Add(objectPool.Value);
                }
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊璞℃睜銆?
        /// </summary>
        /// <returns>鎵€鏈夊璞℃睜銆?/returns>
        public ObjectPoolBase[] GetAllObjectPools()
        {
            return GetAllObjectPools(false);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊璞℃睜銆?
        /// </summary>
        /// <param name="results">鎵€鏈夊璞℃睜銆?/param>
        public void GetAllObjectPools(List<ObjectPoolBase> results)
        {
            GetAllObjectPools(false, results);
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊璞℃睜銆?
        /// </summary>
        /// <param name="sort">鏄惁鏍规嵁瀵硅薄姹犵殑浼樺厛绾ф帓搴忋€?/param>
        /// <returns>鎵€鏈夊璞℃睜銆?/returns>
        public ObjectPoolBase[] GetAllObjectPools(bool sort)
        {
            if (sort)
            {
                List<ObjectPoolBase> results = new List<ObjectPoolBase>();
                foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
                {
                    results.Add(objectPool.Value);
                }

                results.Sort(m_ObjectPoolComparer);
                return results.ToArray();
            }
            else
            {
                int index = 0;
                ObjectPoolBase[] results = new ObjectPoolBase[m_ObjectPools.Count];
                foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
                {
                    results[index++] = objectPool.Value;
                }

                return results;
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊璞℃睜銆?
        /// </summary>
        /// <param name="sort">鏄惁鏍规嵁瀵硅薄姹犵殑浼樺厛绾ф帓搴忋€?/param>
        /// <param name="results">鎵€鏈夊璞℃睜銆?/param>
        public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in m_ObjectPools)
            {
                results.Add(objectPool.Value);
            }

            if (sort)
            {
                results.Sort(m_ObjectPoolComparer);
            }
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>() where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name)
        {
            return InternalCreateObjectPool(objectType, name, false, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, int capacity)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, float expireTime)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, int capacity)
        {
            return InternalCreateObjectPool(objectType, name, false, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, float expireTime)
        {
            return InternalCreateObjectPool(objectType, name, false, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, int capacity, float expireTime)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, int capacity, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, int capacity, float expireTime)
        {
            return InternalCreateObjectPool(objectType, name, false, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, int capacity, int priority)
        {
            return InternalCreateObjectPool(objectType, name, false, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, false, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, false, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, false, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, false, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="autoReleaseInterval">瀵硅薄姹犺嚜鍔ㄩ噴鏀惧彲閲婃斁瀵硅薄鐨勯棿闅旂鏁般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, false, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="autoReleaseInterval">瀵硅薄姹犺嚜鍔ㄩ噴鏀惧彲閲婃斁瀵硅薄鐨勯棿闅旂鏁般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽鍗曟鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateSingleSpawnObjectPool(Type objectType, string name, float autoReleaseInterval, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, false, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>() where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name)
        {
            return InternalCreateObjectPool(objectType, name, true, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, int capacity)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, float expireTime)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, int capacity)
        {
            return InternalCreateObjectPool(objectType, name, true, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, float expireTime)
        {
            return InternalCreateObjectPool(objectType, name, true, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, int capacity, float expireTime)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, int capacity, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, int capacity, float expireTime)
        {
            return InternalCreateObjectPool(objectType, name, true, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, int capacity, int priority)
        {
            return InternalCreateObjectPool(objectType, name, true, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, true, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(string.Empty, true, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, string.Empty, true, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, true, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="autoReleaseInterval">瀵硅薄姹犺嚜鍔ㄩ噴鏀惧彲閲婃斁瀵硅薄鐨勯棿闅旂鏁般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            return InternalCreateObjectPool<T>(name, true, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 鍒涘缓鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瀵硅薄姹犲悕绉般€?/param>
        /// <param name="autoReleaseInterval">瀵硅薄姹犺嚜鍔ㄩ噴鏀惧彲閲婃斁瀵硅薄鐨勯棿闅旂鏁般€?/param>
        /// <param name="capacity">瀵硅薄姹犵殑瀹归噺銆?/param>
        /// <param name="expireTime">瀵硅薄姹犲璞¤繃鏈熺鏁般€?/param>
        /// <param name="priority">瀵硅薄姹犵殑浼樺厛绾с€?/param>
        /// <returns>瑕佸垱寤虹殑鍏佽澶氭鑾峰彇鐨勫璞℃睜銆?/returns>
        public ObjectPoolBase CreateMultiSpawnObjectPool(Type objectType, string name, float autoReleaseInterval, int capacity, float expireTime, int priority)
        {
            return InternalCreateObjectPool(objectType, name, true, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool<T>() where T : ObjectBase
        {
            return InternalDestroyObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalDestroyObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="name">瑕侀攢姣佺殑瀵硅薄姹犲悕绉般€?/param>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool<T>(string name) where T : ObjectBase
        {
            return InternalDestroyObjectPool(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <param name="objectType">瀵硅薄绫诲瀷銆?/param>
        /// <param name="name">瑕侀攢姣佺殑瀵硅薄姹犲悕绉般€?/param>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool(Type objectType, string name)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            return InternalDestroyObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <typeparam name="T">瀵硅薄绫诲瀷銆?/typeparam>
        /// <param name="objectPool">瑕侀攢姣佺殑瀵硅薄姹犮€?/param>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool<T>(IObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null)
            {
                throw new GameFrameworkException("Object pool is invalid.");
            }

            return InternalDestroyObjectPool(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 閿€姣佸璞℃睜銆?
        /// </summary>
        /// <param name="objectPool">瑕侀攢姣佺殑瀵硅薄姹犮€?/param>
        /// <returns>鏄惁閿€姣佸璞℃睜鎴愬姛銆?/returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null)
            {
                throw new GameFrameworkException("Object pool is invalid.");
            }

            return InternalDestroyObjectPool(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        /// <summary>
        /// 閲婃斁瀵硅薄姹犱腑鐨勫彲閲婃斁瀵硅薄銆?
        /// </summary>
        public void Release()
        {
            GetAllObjectPools(true, m_CachedAllObjectPools);
            foreach (ObjectPoolBase objectPool in m_CachedAllObjectPools)
            {
                objectPool.Release();
            }
        }

        /// <summary>
        /// 閲婃斁瀵硅薄姹犱腑鐨勬墍鏈夋湭浣跨敤瀵硅薄銆?
        /// </summary>
        public void ReleaseAllUnused()
        {
            GetAllObjectPools(true, m_CachedAllObjectPools);
            foreach (ObjectPoolBase objectPool in m_CachedAllObjectPools)
            {
                objectPool.ReleaseAllUnused();
            }
        }

        private bool InternalHasObjectPool(TypeNamePair typeNamePair)
        {
            return m_ObjectPools.ContainsKey(typeNamePair);
        }

        private ObjectPoolBase InternalGetObjectPool(TypeNamePair typeNamePair)
        {
            ObjectPoolBase objectPool = null;
            if (m_ObjectPools.TryGetValue(typeNamePair, out objectPool))
            {
                return objectPool;
            }

            return null;
        }

        private IObjectPool<T> InternalCreateObjectPool<T>(string name, bool allowMultiSpawn, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasObjectPool<T>(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist object pool '{0}'.", typeNamePair));
            }

            ObjectPool<T> objectPool = new ObjectPool<T>(name, allowMultiSpawn, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjectPools.Add(typeNamePair, objectPool);
            return objectPool;
        }

        private ObjectPoolBase InternalCreateObjectPool(Type objectType, string name, bool allowMultiSpawn, float autoReleaseInterval, int capacity, float expireTime, int priority)
        {
            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Object type '{0}' is invalid.", objectType.FullName));
            }

            TypeNamePair typeNamePair = new TypeNamePair(objectType, name);
            if (HasObjectPool(objectType, name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist object pool '{0}'.", typeNamePair));
            }

            Type objectPoolType = typeof(ObjectPool<>).MakeGenericType(objectType);
            ObjectPoolBase objectPool = (ObjectPoolBase)Activator.CreateInstance(objectPoolType, name, allowMultiSpawn, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjectPools.Add(typeNamePair, objectPool);
            return objectPool;
        }

        private bool InternalDestroyObjectPool(TypeNamePair typeNamePair)
        {
            ObjectPoolBase objectPool = null;
            if (m_ObjectPools.TryGetValue(typeNamePair, out objectPool))
            {
                objectPool.Shutdown();
                return m_ObjectPools.Remove(typeNamePair);
            }

            return false;
        }

        private static int ObjectPoolComparer(ObjectPoolBase a, ObjectPoolBase b)
        {
            return a.Priority.CompareTo(b.Priority);
        }
    }
}
