using System;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class EntityComponentExtensions
    {
        // 关于 EntityId 的约定：
        // 0 为无效
        // 正值用于和服务器通信的实体（如玩家角色、NPC、怪等，服务器只产生正值）
        // 负值用于本地生成的临时实体（如特效、FakeObject等）
        private static int s_SerialId = 0;


        /// <summary>
        /// 生成游戏唯一id
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <returns></returns>
        public static int GenerateSerialId(this EntityComponent entityComponent)
        {
            return --s_SerialId;
        }


        /// <summary>
        /// Show Entity
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="logicType"></param>
        /// <param name="entityGroup"></param>
        /// <param name="priority"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int ShowEntity(this EntityComponent entityComponent,
            Type logicType,
            string entityGroup,
            int priority,
            EntityData data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return 0;
            }

            if (priority == 0)
            {
                priority = Constant.AssetPriority.EntityAssets;
            }

            if (data.Id == 0)
            {
                data.Id = entityComponent.GenerateSerialId();
            }

            if (string.IsNullOrEmpty(data.EntityAssetsPath))
            {
                Log.Fatal("The entity Assets path is null.");
            }

            var entityPath = data.EntityAssetsPath;
            entityComponent.ShowEntity(data.Id, logicType, entityPath,
                entityGroup,
                priority, data);
            return data.Id;
        }


        /// <summary>
        /// 异步ShowEntity
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="logicType"></param>
        /// <param name="entityGroup"></param>
        /// <param name="priority"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UniTask<int> ShowEntityAsync(
            this EntityComponent entityComponent,
            Type logicType,
            string entityGroup,
            int priority,
            EntityData data)
        {
            if (data == null)
            {
                Log.Fatal("Data is invalid.");
                return default;
            }

            if (priority == 0)
            {
                priority = Constant.AssetPriority.EntityAssets;
            }
            
            if (data.Id == 0)
            {
                data.Id = entityComponent.GenerateSerialId();
            }

            var entityPath = data.EntityAssetsPath;
            return entityComponent.ShowEntityAsync(data.Id, logicType,
                entityPath,
                entityGroup,
                priority, data);
        }
    }
}