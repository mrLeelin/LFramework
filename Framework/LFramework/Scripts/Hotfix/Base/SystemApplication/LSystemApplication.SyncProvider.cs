using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    public partial class LSystemApplication
    {
        private static readonly IDictionary<string, PropertyInfo> CachePropertyInfoByClass =
            new Dictionary<string, PropertyInfo>();
        
        // 新增：缓存类型的属性信息，避免每次反射
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TypePropertiesCache = new();
        
        // 新增：缓存类型的泛型参数信息
        private static readonly ConcurrentDictionary<Type, Type[]> TypeGenericArgumentsCache = new();
        
        // 新增：缓存类型的接口信息
        private static readonly ConcurrentDictionary<Type, Type[]> TypeInterfacesCache = new();

        private readonly Dictionary<Type, List<ISyncServerData>> _syncServerDates = new();
        private readonly Dictionary<ISyncServerData, Type> _linkSyncServerDates = new();

        
        public void Sync(object commonData, bool isLogin)
        {
            try
            {
                TrySync(commonData, isLogin);
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }


        private void TrySync(object data, bool isLogin)
        {
            foreach (var kPair in _syncServerDates)
            {
                var property = GetPropertyInfo(data, kPair.Key);
                if (property == null) continue;

                var value = property.GetValue(data);
                if (value == null) continue;
                
                // 如果value是list并且Count == 0那么Return
                if (value is System.Collections.ICollection { Count: 0 }) continue;

                foreach (var serverData in kPair.Value)
                {
                    serverData.SyncServer(value, isLogin);
                }
            }
        }

        private void RegisterCommonDataSync(SystemProviderBase systemProviderBase)
        {
            if (systemProviderBase is not ISyncServerData syncServerData) return;

            var syncType = syncServerData.GetType();
            
            // 使用缓存获取接口信息，避免每次反射
            var interfaces = TypeInterfacesCache.GetOrAdd(syncType, t => t.GetInterfaces());
            
            //The data module is sync script
            foreach (var @interface in interfaces)
            {
                if (!@interface.IsGenericType)
                {
                    continue;
                }

                // 使用缓存获取泛型参数，避免每次反射
                var genericArgs = TypeGenericArgumentsCache.GetOrAdd(@interface, t => t.GetGenericArguments());
                if (genericArgs.Length == 0) continue;
                
                var actualType = genericArgs[0];
                if (_syncServerDates.TryGetValue(actualType, out var list))
                {
                    list.Add(syncServerData);
                }
                else
                {
                    _syncServerDates.Add(actualType, new List<ISyncServerData> { syncServerData });
                }

                _linkSyncServerDates.Add(syncServerData, actualType);
            }
        }

        private void UnRegisterCommonDataSync(SystemProviderBase systemProviderBase)
        {
            if (systemProviderBase is not ISyncServerData syncServerData) return;

            if (!_linkSyncServerDates.Remove(syncServerData, out var type)) return;

            if (!_syncServerDates.TryGetValue(type, out var list)) return;

            if (!list.Remove(syncServerData))
            {
                return;
            }

            if (list.Count > 0)
            {
                return;
            }

            _syncServerDates.Remove(type);
        }


        private static PropertyInfo GetPropertyInfo(object origin, Type type)
        {
            if (type == null) throw new Exception("Type is null.");

            var key = $"{type.FullName}";
            if (CachePropertyInfoByClass.TryGetValue(key, out var info)) return info;

            var result =
                GetPropertyInfoWithType(origin, type); //?? GetPropertyInfoWithTypeName(origin, type,propertyName);
            if (result == null) return null;

            CachePropertyInfoByClass.Add(key, result);
            return result;
        }

        private static PropertyInfo GetPropertyInfoWithType(object origin, Type type)
        {
            if (type == null) throw new Exception("type is empty");
            
            var originType = origin.GetType();
            
            // 使用缓存获取属性信息，避免每次反射
            var properties = TypePropertiesCache.GetOrAdd(originType, t => t.GetProperties());
            
            foreach (var p in properties)
            {
                if (!p.CanRead) continue;

                var propertyType = p.PropertyType;
                if (!propertyType.IsGenericType)
                {
                    //not list
                    if (propertyType == type) return p;
                    continue;
                }

                // 使用缓存获取泛型参数，避免每次反射
                var genericArgs = TypeGenericArgumentsCache.GetOrAdd(propertyType, t => t.GenericTypeArguments);
                if (genericArgs.Length == 0) continue;
                
                var first = genericArgs[0];
                if (first == type) return p;
            }

            Log.Error(
                $"common proto file. update property is not contain this property. type:{type.Name.ToLower()}");
            return null;
        }
    }
}