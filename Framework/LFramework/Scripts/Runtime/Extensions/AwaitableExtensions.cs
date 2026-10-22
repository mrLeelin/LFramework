using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class AwaitableExtensions
    {
        private static readonly HashSet<UniTaskCompletionSource<int?>>
            SuiFormTcs = new();

        private static readonly Dictionary<EntityComponent, int> EntityWaitCounts = new();


        private static readonly Dictionary<string, UniTaskCompletionSource<bool>> SSceneTcs = new();

        public static void SubscribeEvent(EventComponent eventComponent)
        {
            eventComponent.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            eventComponent.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        }

        /// <summary>
        /// 清理所有未完成的异步任务，防止内存泄漏。
        /// 应在应用关闭或场景切换时调用。
        /// </summary>
        public static void ClearAll()
        {
            var uiWaiters = new List<UniTaskCompletionSource<int?>>(SuiFormTcs);
            SuiFormTcs.Clear();
            var sceneWaiters = new List<UniTaskCompletionSource<bool>>(SSceneTcs.Values);
            SSceneTcs.Clear();

            var entityComponents = new List<EntityComponent>(EntityWaitCounts.Keys);
            foreach (var component in entityComponents) component.CancelPendingEntityTasks();

            foreach (var tcs in uiWaiters) tcs.TrySetCanceled();
            foreach (var tcs in sceneWaiters) tcs.TrySetCanceled();
        }

        #region Ui form

        /// <summary>
        /// 打开界面（可等待）
        /// </summary>
        public static async UniTask<int?> OpenUIFormAsync(this UIComponent uiComponent,
            string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
        {
            if (uiComponent == null) throw new ArgumentNullException(nameof(uiComponent));
            var tcs = new UniTaskCompletionSource<int?>();
            int serialId = 0;
            bool serialAssigned = false;
            Dictionary<int, (bool canceled, string error)> earlyResults = null;

            void Complete(int completedSerialId, bool canceled, string error)
            {
                if (!serialAssigned)
                {
                    earlyResults ??= new Dictionary<int, (bool, string)>();
                    if (!earlyResults.ContainsKey(completedSerialId))
                        earlyResults.Add(completedSerialId, (canceled, error));
                    return;
                }

                if (completedSerialId != serialId || !SuiFormTcs.Remove(tcs)) return;
                if (canceled) tcs.TrySetCanceled();
                else if (error != null) tcs.TrySetException(new GameFrameworkException(error));
                else tcs.TrySetResult(serialId);
            }

            void OnSuccess(int id) => Complete(id, false, null);
            void OnFailure(int id, string error) => Complete(id, false, error ?? "UI form open failed.");
            void OnCanceled(int id) => Complete(id, true, null);

            // Register before opening: pooled forms and custom resource managers can finish synchronously.
            SuiFormTcs.Add(tcs);
            uiComponent.OpenUIFormSucceeded += OnSuccess;
            uiComponent.OpenUIFormFailed += OnFailure;
            uiComponent.OpenUIFormCanceled += OnCanceled;
            try
            {
                serialId = uiComponent.OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, userData);
                serialAssigned = true;
                if (earlyResults != null && earlyResults.TryGetValue(serialId, out var result))
                    Complete(serialId, result.canceled, result.error);
                return await tcs.Task;
            }
            finally
            {
                SuiFormTcs.Remove(tcs);
                uiComponent.OpenUIFormSucceeded -= OnSuccess;
                uiComponent.OpenUIFormFailed -= OnFailure;
                uiComponent.OpenUIFormCanceled -= OnCanceled;
            }
        }

        #endregion

        #region Entity

        /// <summary>
        /// 显示实体
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="entityId"></param>
        /// <param name="entityAssetName"></param>
        /// <param name="entityGroupName"></param>
        /// <param name="priority"></param>
        /// <param name="userData"></param>
        /// <typeparam name="TLogic"></typeparam>
        /// <returns></returns>
        public static UniTask<int> ShowEntityAsync<TLogic>(this EntityComponent entityComponent, int entityId,
            string entityAssetName, string entityGroupName, int priority, object userData)
            where TLogic : EntityLogic
        {
            return ShowEntityAsync<TLogic>(entityComponent, entityId, entityAssetName, entityGroupName,
                priority, userData, CancellationToken.None);
        }

        /// <summary>Show an entity with cancellation support.</summary>
        public static UniTask<int> ShowEntityAsync<TLogic>(this EntityComponent entityComponent, int entityId,
            string entityAssetName, string entityGroupName, int priority, object userData, CancellationToken cancellationToken)
            where TLogic : EntityLogic
        {
            return ShowEntityAsync(entityComponent, entityId, typeof(TLogic), entityAssetName, entityGroupName,
                priority, userData, cancellationToken);
        }

        /// <summary>
        /// 显示实体（可等待）
        /// </summary>
        public static UniTask<int> ShowEntityAsync(this EntityComponent entityComponent, int entityId,
            Type entityLogicType, string entityAssetName, string entityGroupName, int priority, object userData)
        {
            return ShowEntityAsync(entityComponent, entityId, entityLogicType, entityAssetName, entityGroupName,
                priority, userData, CancellationToken.None);
        }

        /// <summary>Show an entity with cancellation support.</summary>
        public static UniTask<int> ShowEntityAsync(this EntityComponent entityComponent, int entityId,
            Type entityLogicType, string entityAssetName, string entityGroupName, int priority, object userData, CancellationToken cancellationToken)
        {
            TrackEntityWait(entityComponent);
            return TrackShowAsync(entityComponent, entityId, entityLogicType, entityAssetName, entityGroupName, priority, userData, cancellationToken);
        }

        private static async UniTask<int> TrackShowAsync(EntityComponent component, int entityId, Type logicType,
            string assetName, string groupName, int priority, object userData, CancellationToken cancellationToken)
        {
            try { return await component.ShowEntityAwaitable(entityId, logicType, assetName, groupName, priority, userData, cancellationToken); }
            finally { UntrackEntityWait(component); }
        }

        private static void TrackEntityWait(EntityComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            EntityWaitCounts.TryGetValue(component, out int count);
            EntityWaitCounts[component] = count + 1;
        }

        private static void UntrackEntityWait(EntityComponent component)
        {
            if (!EntityWaitCounts.TryGetValue(component, out int count)) return;
            if (count <= 1) EntityWaitCounts.Remove(component);
            else EntityWaitCounts[component] = count - 1;
        }

        /// <summary>
        /// 隐藏实体（可等待）
        /// </summary>
        public static UniTask HideEntityAsync(this EntityComponent entityComponent, int entityID)
        {
            return HideEntityAsync(entityComponent, entityID, CancellationToken.None);
        }

        /// <summary>Hide an entity with cancellation support.</summary>
        public static async UniTask HideEntityAsync(this EntityComponent entityComponent, int entityID, CancellationToken cancellationToken)
        {
            TrackEntityWait(entityComponent);
            try { await entityComponent.HideEntityAwaitable(entityID, cancellationToken); }
            finally { UntrackEntityWait(entityComponent); }
        }

        #endregion

        #region Scene

        /// <summary>
        /// 加载场景（可等待）
        /// </summary>
        public static UniTask<bool> LoadSceneAsync(this SceneComponent sceneComponent, string sceneAssetName)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            SSceneTcs.Add(sceneAssetName, tcs);
            sceneComponent.LoadScene(sceneAssetName);
            return tcs.Task;
        }

        private static void OnLoadSceneSuccess(object sender, GameEventArgs e)
        {
            var ne = (LoadSceneSuccessEventArgs)e;
            SSceneTcs.TryGetValue(ne.SceneAssetName, out var tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetResult(true);
            SSceneTcs.Remove(ne.SceneAssetName);
        }

        private static void OnLoadSceneFailure(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
            SSceneTcs.TryGetValue(ne.SceneAssetName, out var tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            SSceneTcs.Remove(ne.SceneAssetName);
        }

        #endregion

        #region Assets

        // LoadAssetAsync 已迁移到 ResourceComponent.LoadAssetHandle<T>()，请使用新的 Handle API

        #endregion
    }
}
