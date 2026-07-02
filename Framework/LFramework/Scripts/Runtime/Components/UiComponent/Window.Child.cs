using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class Window
    {
        private readonly List<int> _childEntityIds = new List<int>();
        private readonly List<int> _removeAllChildBuffer = new List<int>();
        private readonly List<int> _removeChildRangeBuffer = new List<int>();

        /// <summary>
        /// 获取当前 Window 已登记的子实体数量。
        /// </summary>
        /// <returns>已登记的子实体数量。</returns>
        public int GetChildCount()
        {
            return _childEntityIds.Count;
        }

        /// <summary>
        /// 获取当前 Window 已登记的子实体编号列表。
        /// </summary>
        /// <returns>只读子实体编号列表。</returns>
        public IReadOnlyList<int> GetChildren()
        {
            return _childEntityIds;
        }

        /// <summary>
        /// 将当前 Window 已登记的子实体编号复制到指定列表。
        /// </summary>
        /// <param name="results">用于接收子实体编号的列表。</param>
        public void GetChildren(List<int> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            results.AddRange(_childEntityIds);
        }

        /// <summary>
        /// 同步加载并登记一个 Window 子实体。
        /// </summary>
        /// <param name="param">子实体加载参数。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>成功发起加载的实体编号；参数或 Window 无效时返回 0。</returns>
        public int AddChild<TLogic>(AddChildParam param)
            where TLogic : UIChildEntityLogic
        {
            if (!CanOperateChild() || !param.TryCreateEntityData(out var data))
            {
                return 0;
            }

            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            var entityId = ShowUIChild<TLogic, UIChildEntityData>(entityComponent, data);
            RegisterChild(entityId);
            return entityId;
        }

        /// <summary>
        /// 异步加载并登记一个 Window 子实体。
        /// </summary>
        /// <param name="param">子实体加载参数。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>加载完成后的子实体逻辑；参数无效、加载期间被卸载或实体不存在时返回 null。</returns>
        public async UniTask<TLogic> AddChildAsync<TLogic>(AddChildParam param)
            where TLogic : UIChildEntityLogic
        {
            if (!CanOperateChild() || !param.TryCreateEntityData(out var data))
            {
                return null;
            }

            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            var loadTask = ShowUIChildAsync<TLogic, UIChildEntityData>(entityComponent, data);
            RegisterChild(data.Id);

            int entityId;
            try
            {
                entityId = await loadTask;
            }
            catch
            {
                var wasRegistered = UnregisterChild(data.Id);
                if (!wasRegistered || this == null)
                {
                    return null;
                }

                throw;
            }

            if (this == null || !IsRegisteredChild(entityId))
            {
                UnregisterChild(entityId);
                HideChildEntity(entityComponent, entityId);
                return null;
            }

            var entity = entityComponent.GetEntity(entityId);
            return entity != null ? entity.Logic as TLogic : null;
        }

        /// <summary>
        /// 异步加载并登记一个 Window 子实体。
        /// </summary>
        /// <remarks>
        /// 保留旧签名用于兼容历史调用。实际加载参数以 param 为准。
        /// </remarks>
        /// <param name="parent">兼容参数，已由 param 承载。</param>
        /// <param name="assetsPath">兼容参数，已由 param 承载。</param>
        /// <param name="param">子实体加载参数。</param>
        /// <param name="userData">兼容参数，已由 param 承载。</param>
        /// <typeparam name="TLogic">子实体逻辑类型。</typeparam>
        /// <returns>加载完成后的子实体逻辑；参数无效、加载期间被卸载或实体不存在时返回 null。</returns>
        public UniTask<TLogic> AddChildAsync<TLogic>(Transform parent, string assetsPath, AddChildParam param,
            object userData)
            where TLogic : UIChildEntityLogic
        {
            return AddChildAsync<TLogic>(param);
        }

        /// <summary>
        /// 卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="id">子实体编号。</param>
        public void RemoveChild(int id)
        {
            if (!UnregisterChild(id))
            {
                return;
            }

            HideChildEntity(LFrameworkAspect.Instance.Get<EntityComponent>(), id);
        }

        /// <summary>
        /// 批量卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="ids">子实体编号列表。</param>
        public void RemoveChild(List<int> ids)
        {
            RemoveChildRange(ids);
        }

        /// <summary>
        /// 批量卸载当前 Window 持有的指定子实体。
        /// </summary>
        /// <param name="ids">子实体编号集合。</param>
        public void RemoveChild(HashSet<int> ids)
        {
            RemoveChildRange(ids);
        }

        /// <summary>
        /// 卸载当前 Window 持有的全部子实体。
        /// </summary>
        public void RemoveChild()
        {
            GetChildIdsSnapshot(_removeAllChildBuffer);
            if (_removeAllChildBuffer.Count == 0)
            {
                return;
            }

            _childEntityIds.Clear();
            HideChildEntities(_removeAllChildBuffer);
        }

        private static int ShowUIChild<TLogic, TEntityData>(EntityComponent entityComponent, TEntityData entityData)
            where TEntityData : UIChildEntityData
            where TLogic : UIChildEntityLogic
        {
            if (!PrepareChildEntity(entityComponent, entityData))
            {
                return 0;
            }

            var entityPath = ResolveChildEntityPath(entityData);
            entityComponent.ShowEntity(entityData.Id, typeof(TLogic), entityPath,
                GetUIChildEntityGroupName(), Constant.AssetPriority.EntityAssets, entityData);

            return entityData.Id;
        }

        private static UniTask<int> ShowUIChildAsync<TLogic, TEntityData>(EntityComponent entityComponent,
            TEntityData entityData)
            where TEntityData : UIChildEntityData
            where TLogic : UIChildEntityLogic
        {
            if (!PrepareChildEntity(entityComponent, entityData))
            {
                return default;
            }

            return entityComponent.ShowEntityAsync(entityData.Id, typeof(TLogic),
                ResolveChildEntityPath(entityData),
                GetUIChildEntityGroupName(),
                Constant.AssetPriority.EntityAssets, entityData);
        }

        private static bool PrepareChildEntity(EntityComponent entityComponent, UIChildEntityData entityData)
        {
            if (entityComponent == null)
            {
                Log.Error("Can not show child entity because entity component is invalid.");
                return false;
            }

            if (entityData == null)
            {
                Log.Warning("Child entity data is invalid.");
                return false;
            }

            if (entityData.Id == 0)
            {
                entityData.Id = entityComponent.GenerateSerialId();
            }

            return true;
        }

        private static string ResolveChildEntityPath(UIChildEntityData entityData)
        {
            return entityData.EntityAssetsPath;
        }
`

        private static string GetUIChildEntityGroupName()
        {
            var setting = SettingManager.GetProjectSelector()?.GetComponentSetting<UIComponentSetting>();
            if (setting != null)
            {
                return setting.ChildEntityGroupName;
            }

            var uiComponent = LFrameworkAspect.Instance.Get<UIComponent>();
            return uiComponent != null ? uiComponent.ChildEntityGroupName : string.Empty;
        }

        private bool CanOperateChild()
        {
            if (this != null)
            {
                return true;
            }

            Log.Error("Can not add or remove child entity because window is invalid.");
            return false;
        }

        private void RegisterChild(int id)
        {
            if (id == 0 || _childEntityIds.Contains(id))
            {
                return;
            }

            _childEntityIds.Add(id);
        }

        private bool UnregisterChild(int id)
        {
            return id != 0 && _childEntityIds.Remove(id);
        }

        private bool IsRegisteredChild(int id)
        {
            return id != 0 && _childEntityIds.Contains(id);
        }

        private void RemoveChildRange(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                return;
            }

            _removeChildRangeBuffer.Clear();
            foreach (var id in ids)
            {
                _removeChildRangeBuffer.Add(id);
            }

            var validCount = 0;
            for (var i = 0; i < _removeChildRangeBuffer.Count; i++)
            {
                var id = _removeChildRangeBuffer[i];
                if (!UnregisterChild(id))
                {
                    continue;
                }

                _removeChildRangeBuffer[validCount++] = id;
            }

            if (validCount < _removeChildRangeBuffer.Count)
            {
                _removeChildRangeBuffer.RemoveRange(validCount, _removeChildRangeBuffer.Count - validCount);
            }

            if (_removeChildRangeBuffer.Count == 0)
            {
                return;
            }

            HideChildEntities(_removeChildRangeBuffer);
        }

        private void GetChildIdsSnapshot(List<int> results)
        {
            results.Clear();
            results.AddRange(_childEntityIds);
        }

        private static void HideChildEntities(List<int> ids)
        {
            var entityComponent = LFrameworkAspect.Instance.Get<EntityComponent>();
            try
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    HideChildEntity(entityComponent, ids[i]);
                }
            }
            finally
            {
                ids.Clear();
            }
        }

        private static void HideChildEntity(EntityComponent entityComponent, int id)
        {
            if (id == 0)
            {
                return;
            }

            if (entityComponent == null)
            {
                Log.Error("Can not hide child entity because entity component is invalid.");
                return;
            }

            entityComponent.HideEntity(id);
        }
    }
}