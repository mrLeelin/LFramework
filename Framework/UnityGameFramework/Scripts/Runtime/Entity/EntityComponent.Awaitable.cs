using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;

namespace UnityGameFramework.Runtime
{
    public sealed partial class EntityComponent
    {
        private readonly Dictionary<int, Action> _pendingEntityShows = new();
        private readonly HashSet<Action> _pendingEntityHides = new();
        private bool _entityAwaitablesShutdown;

        public override void ShutDown()
        {
            _entityAwaitablesShutdown = true;
            CancelPendingEntityTasks();
            base.ShutDown();
        }

        /// <summary>Cancel outstanding waits and any entity loads that those waits own.</summary>
        public void CancelPendingEntityTasks()
        {
            var shows = new List<Action>(_pendingEntityShows.Values);
            var hides = new List<Action>(_pendingEntityHides);
            foreach (var cancel in shows) cancel();
            foreach (var cancel in hides) cancel();
        }

        /// <summary>Show an entity, completing on success, failure or cancellation.</summary>
        public UniTask<int> ShowEntityAwaitable<TLogic>(int entityId, string entityAssetName,
            string entityGroupName, int priority, object userData, CancellationToken cancellationToken = default)
            where TLogic : EntityLogic => ShowEntityAwaitable(entityId, typeof(TLogic), entityAssetName,
                entityGroupName, priority, userData, cancellationToken);

        /// <summary>Show an entity. Cancellation also cancels its pending load.</summary>
        public UniTask<int> ShowEntityAwaitable(int entityId, Type entityLogicType, string entityAssetName,
            string entityGroupName, int priority, object userData, CancellationToken cancellationToken = default)
        {
            if (!PlayerLoopHelper.IsMainThread) return UniTask.FromException<int>(new GameFrameworkException("Entity operations require the Unity main thread."));
            if (_entityAwaitablesShutdown) return UniTask.FromException<int>(new GameFrameworkException("Entity component is shut down."));
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<int>(cancellationToken);
            if (entityLogicType == null) return UniTask.FromException<int>(new ArgumentNullException(nameof(entityLogicType)));
            if (m_EntityManager == null) return UniTask.FromException<int>(new GameFrameworkException("Entity component is not initialized."));
            if (_pendingEntityShows.ContainsKey(entityId))
                return UniTask.FromException<int>(new GameFrameworkException($"Entity '{entityId}' already has a pending show operation."));

            var manager = m_EntityManager;
            var info = ShowEntityInfo.Create(entityLogicType, userData);
            int infoVersion = info.Version;
            var tcs = new UniTaskCompletionSource<int>();
            var task = tcs.Task;
            bool completed = false;
            bool started = false;
            bool terminalEvent = false;
            bool cancellationRequested = false;
            CancellationTokenRegistration registration = default;
            EventHandler<GameFramework.Entity.ShowEntitySuccessEventArgs> onSuccess = null;
            EventHandler<GameFramework.Entity.ShowEntityFailureEventArgs> onFailure = null;
            Action<int, object> onCanceled = null;
            Action cancel = null;

            void Detach()
            {
                completed = true;
                manager.ShowEntitySuccess -= onSuccess;
                manager.ShowEntityFailure -= onFailure;
                manager.ShowEntityCanceled -= onCanceled;
                _pendingEntityShows.Remove(entityId);
                registration.Dispose();
            }

            void CompleteCanceledShow(GameFramework.Entity.IEntity entity)
            {
                // Stop cancellation reentry, then release the old entity before a continuation can reuse its ID.
                Detach();
                try
                {
                    if (entity != null && ReferenceEquals(manager.GetEntity(entityId), entity))
                        manager.HideEntity(entityId);
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception exception)
                {
                    tcs.TrySetException(exception);
                }
            }

            onSuccess = (_, args) =>
            {
                if (completed || !ReferenceEquals(args.UserData, info)) return;
                terminalEvent = true;
                if (cancellationRequested)
                {
                    CompleteCanceledShow(args.Entity);
                    return;
                }
                int shownId = args.Entity.Id;
                Detach();
                tcs.TrySetResult(shownId);
            };
            onFailure = (_, args) =>
            {
                if (completed || !ReferenceEquals(args.UserData, info)) return;
                terminalEvent = true;
                if (cancellationRequested)
                {
                    CompleteCanceledShow(manager.GetEntity(entityId));
                    return;
                }
                string error = args.ErrorMessage;
                Detach();
                tcs.TrySetException(new GameFrameworkException(error));
            };
            cancel = () =>
            {
                if (completed) return;
                cancellationRequested = true;
                // Loading state is removed before OnInit/OnShow. Let that synchronous dispatch finish
                // before hiding the entity; hiding here would corrupt its in-progress initialization.
                if (started && !manager.IsLoadingEntity(entityId)) return;
                // Cancel the old load before publishing cancellation: a continuation may reuse its ID.
                try
                {
                    if (started && manager.IsLoadingEntity(entityId)) manager.HideEntity(entityId);
                }
                finally
                {
                    if (!completed)
                    {
                        Detach();
                        tcs.TrySetCanceled(cancellationToken);
                    }
                }
            };

            onCanceled = (canceledId, operationInfo) =>
            {
                if (completed || canceledId != entityId || !ReferenceEquals(operationInfo, info)) return;
                Detach();
                tcs.TrySetCanceled(cancellationToken);
            };

            manager.ShowEntitySuccess += onSuccess;
            manager.ShowEntityFailure += onFailure;
            manager.ShowEntityCanceled += onCanceled;
            _pendingEntityShows.Add(entityId, cancel);
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    if (!PlayerLoopHelper.IsMainThread)
                        PlayerLoopHelper.UnitySynchronizationContext.Post(_ => cancel(), null);
                    else cancel();
                });
                if (completed) registration.Dispose();
            }

            try
            {
                if (!completed)
                {
                    started = true;
                    manager.ShowEntity(entityId, entityAssetName, entityGroupName, priority, info);
                }
                else info.Release(infoVersion);
            }
            catch (Exception exception)
            {
                if (!terminalEvent && !manager.IsLoadingEntity(entityId)) info.Release(infoVersion);
                if (!completed)
                {
                    Detach();
                    tcs.TrySetException(exception);
                }
            }
            return task;
        }

        /// <summary>Hide an entity; hiding a missing or loading entity also completes.</summary>
        public UniTask HideEntityAwaitable(int entityId, CancellationToken cancellationToken = default)
        {
            if (!PlayerLoopHelper.IsMainThread) return UniTask.FromException(new GameFrameworkException("Entity operations require the Unity main thread."));
            if (_entityAwaitablesShutdown) return UniTask.FromException(new GameFrameworkException("Entity component is shut down."));
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled(cancellationToken);
            if (m_EntityManager == null) return UniTask.FromException(new GameFrameworkException("Entity component is not initialized."));
            var manager = m_EntityManager;
            var tcs = new UniTaskCompletionSource();
            var task = tcs.Task;
            bool completed = false;
            CancellationTokenRegistration registration = default;
            EventHandler<GameFramework.Entity.HideEntityCompleteEventArgs> onHidden = null;
            Action cancel = null;
            void Detach()
            {
                completed = true;
                manager.HideEntityComplete -= onHidden;
                _pendingEntityHides.Remove(cancel);
                registration.Dispose();
            }
            cancel = () =>
            {
                if (completed) return;
                Detach();
                tcs.TrySetCanceled(cancellationToken);
            };
            onHidden = (_, args) =>
            {
                if (completed || args.EntityId != entityId) return;
                Detach();
                tcs.TrySetResult();
            };
            manager.HideEntityComplete += onHidden;
            _pendingEntityHides.Add(cancel);
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    if (!PlayerLoopHelper.IsMainThread)
                        PlayerLoopHelper.UnitySynchronizationContext.Post(_ => cancel(), null);
                    else cancel();
                });
                if (completed) registration.Dispose();
            }
            try
            {
                if (completed) return task;
                bool loading = manager.IsLoadingEntity(entityId);
                if (loading && _pendingEntityShows.TryGetValue(entityId, out var cancelShow)) cancelShow();
                else manager.HideEntity(entityId);
                if (!completed && (loading || !manager.HasEntity(entityId)))
                {
                    Detach();
                    tcs.TrySetResult();
                }
            }
            catch (Exception exception)
            {
                if (!completed)
                {
                    Detach();
                    tcs.TrySetException(exception);
                }
            }
            return task;
        }
    }
}
