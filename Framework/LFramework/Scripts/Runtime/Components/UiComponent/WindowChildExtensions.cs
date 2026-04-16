using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Window 添加子物体的扩展类
    /// </summary>
    public static class WindowChildExtensions
    {
        private static readonly GameFrameworkMultiDictionary<Window, int> CacheAllChildEntities
            = new GameFrameworkMultiDictionary<Window, int>();


        /// <summary>
        /// 获取子对象数量
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static int GetChildCount(this Window window)
        {
            return CacheAllChildEntities.TryGetValue(window, out var range) ? range.Count : 0;
        }

        /// <summary>
        /// 获取加载的子对象
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static GameFrameworkLinkedListRange<int> GetChildren(this Window window)
        {
            return CacheAllChildEntities.TryGetValue(window, out var range) ? range : default;
        }

        /// <summary>
        /// 添加Child
        /// </summary>
        /// <param name="window"></param>
        /// <param name="param"></param>
        /// <typeparam name="TLogic"></typeparam>
        /// <returns></returns>
        public static int AddChild<TLogic>(this Window window, AddChildParam param)
            where TLogic : UIChildEntityLogic
        {
            if (!param.IsValid())
            {
                return 0;
            }

            var data = new UIChildEntityData(param.AssetPath, param.ParentTransform,
                param.UserData);

            if (param.Position != Vector3.zero)
            {
                data.Position = param.Position;
            }

            if (param.Size != 0)
            {
                data.Size = Vector3.one * param.Size;
            }

            data.DependOn = param.DependOn;

            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            var result = ShowUIChild<TLogic, UIChildEntityData>(entityComponent, data);
            CacheAllChildEntities.Add(window, result);
            return result;
        }


        /// <summary>
        /// 添加Child Async
        /// </summary>
        /// <param name="window"></param>
        /// <param name="parent"></param>
        /// <param name="assetsPath"></param>
        /// <param name="param"></param>
        /// <param name="userData"></param>
        /// <typeparam name="TLogic"></typeparam>
        /// <returns></returns>
        public static async UniTask<TLogic> AddChildAsync<TLogic>(this Window window, Transform parent,
            string assetsPath, AddChildParam param, object userData)
            where TLogic : UIChildEntityLogic
        {
            if (!param.IsValid())
            {
                return null;
            }

            var data = new UIChildEntityData(param.AssetPath, param.ParentTransform,
                param.UserData);

            if (param.Position != Vector3.zero)
            {
                data.Position = param.Position;
            }

            if (param.Size != 0)
            {
                data.Size = Vector3.one * param.Size;
            }

            data.DependOn = param.DependOn;

            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            var id = await ShowUIChildAsync<TLogic, UIChildEntityData>(entityComponent, data);
            CacheAllChildEntities.Add(window, id);
            return entityComponent.GetEntity(id).Logic as TLogic;
        }

        
        public static void RemoveChild(this Window window, int id)
        {
            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            entityComponent.HideEntity(id);
            CacheAllChildEntities.Remove(window, id);
        }

        public static void RemoveChild(this Window window, List<int> id)
        {
            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            foreach (var i in id)
            {
                entityComponent.HideEntity(i);
                CacheAllChildEntities.Remove(window, i);
            }
        }
        
        public static void RemoveChild(this Window window, HashSet<int> id)
        {
            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            foreach (var i in id)
            {
                entityComponent.HideEntity(i);
                CacheAllChildEntities.Remove(window, i);
            }
        }

        public static void RemoveChild(this Window window)
        {
            if (!CacheAllChildEntities.TryGetValue(window, out var ids))
            {
                return;
            }

            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            foreach (var i in ids)
            {
                entityComponent.HideEntity(i);
            }

            CacheAllChildEntities.RemoveAll(window);
        } 
        
        
        /// <summary>
        /// ShowUi Child
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="entityData"></param>
        /// <typeparam name="TLogic"></typeparam>
        /// <typeparam name="TEntityData"></typeparam>
        /// <returns></returns>
        private static int ShowUIChild<TLogic, TEntityData>(EntityComponent entityComponent, TEntityData entityData)
            where TEntityData : UIChildEntityData
            where TLogic : UIChildEntityLogic
        {
            if (entityData == null)
            {
                Log.Warning("Data is invalid.");
                return 0;
            }

            if (entityData.Id == 0)
            {
                entityData.Id = entityComponent.GenerateSerialId();
            }
            var uiComponent = LFrameworkAspect.Instance.Get<UIComponent>();
            var entityPath = entityData.EntityAssetsPath;
            entityComponent.ShowEntity(entityData.Id, typeof(TLogic), entityPath,
                uiComponent.UiChildEntityGroup,
                0, entityData);
            return entityData.Id;
        }

        /// <summary>
        /// Show UiChildAsync
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="entityData"></param>
        /// <typeparam name="TLogic"></typeparam>
        /// <typeparam name="TEntityData"></typeparam>
        /// <returns></returns>
        private static UniTask<int> ShowUIChildAsync<TLogic, TEntityData>(EntityComponent entityComponent,
            TEntityData entityData)
            where TEntityData : UIChildEntityData
            where TLogic : UIChildEntityLogic
        {
            if (entityData == null)
            {
                Log.Warning("Data is invalid.");
                return default;
            }

            if (entityData.Id == 0)
            {
                entityData.Id = entityComponent.GenerateSerialId();
            }
            var uiComponent = LFrameworkAspect.Instance.Get<UIComponent>();
            
            var entityPath = entityData.EntityAssetsPath;
            return entityComponent.ShowEntityAsync(entityData.Id, typeof(TLogic),
                entityPath,
                uiComponent.UiChildEntityGroup,
                0, entityData);
        }
    }
}