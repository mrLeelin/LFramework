using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class AwaitableExtensions
    {
        private static readonly Dictionary<int, UniTaskCompletionSource<int?>>
            SuiFormTcs = new();

        private static readonly Dictionary<int, UniTaskCompletionSource<int>> SEntityTcs = new();
        private static readonly Dictionary<int, UniTaskCompletionSource> SEntityHideTcs = new();


        private static readonly Dictionary<string, UniTaskCompletionSource<bool>> SSceneTcs = new();

        public static void SubscribeEvent(EventComponent eventComponent)
        {
            eventComponent.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            eventComponent.Subscribe(OpenUIFormFailureEventArgs.EventId, OnOpenUIFormFailure);

            eventComponent.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            eventComponent.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            eventComponent.Subscribe(HideEntityCompleteEventArgs.EventId,OnHideEntitySuccess);


            eventComponent.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            eventComponent.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        }

        /// <summary>
        /// 清理所有未完成的异步任务，防止内存泄漏。
        /// 应在应用关闭或场景切换时调用。
        /// </summary>
        public static void ClearAll()
        {
            foreach (var tcs in SuiFormTcs.Values)
            {
                tcs.TrySetCanceled();
            }
            SuiFormTcs.Clear();

            foreach (var tcs in SEntityTcs.Values)
            {
                tcs.TrySetCanceled();
            }
            SEntityTcs.Clear();

            foreach (var tcs in SEntityHideTcs.Values)
            {
                tcs.TrySetCanceled();
            }
            SEntityHideTcs.Clear();

            foreach (var tcs in SSceneTcs.Values)
            {
                tcs.TrySetCanceled();
            }
            SSceneTcs.Clear();
        }

        #region Ui form

        /// <summary>
        /// 打开界面（可等待）
        /// </summary>
        public static UniTask<int?> OpenUIFormAsync(this UIComponent uiComponent,
            string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
        {
            int serialId = uiComponent.OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, userData);
            var tcs = new UniTaskCompletionSource<int?>();
            SuiFormTcs.Add(serialId, tcs);
            return tcs.Task;
        }

        private static void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            OpenUIFormSuccessEventArgs ne = (OpenUIFormSuccessEventArgs)e;
            SuiFormTcs.TryGetValue(ne.UIForm.SerialId, out UniTaskCompletionSource<int?> tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetResult(ne.UIForm.SerialId);
            SuiFormTcs.Remove(ne.UIForm.SerialId);
        }

        private static void OnOpenUIFormFailure(object sender, GameEventArgs e)
        {
            OpenUIFormFailureEventArgs ne = (OpenUIFormFailureEventArgs)e;
            SuiFormTcs.TryGetValue(ne.SerialId, out UniTaskCompletionSource<int?> tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            SuiFormTcs.Remove(ne.SerialId);
        }

        #endregion

        #region Entity

        /// <summary>
        /// 显示实体（可等待）
        /// </summary>
        public static UniTask<int> ShowEntityAsync(this EntityComponent entityComponent, int entityId,
            Type entityLogicType, string entityAssetName, string entityGroupName, int priority, object userData)
        {
            var tcs = new UniTaskCompletionSource<int>();
            SEntityTcs.Add(entityId, tcs);
            entityComponent.ShowEntity(entityId, entityLogicType, entityAssetName, entityGroupName, priority, userData);
            return tcs.Task;
        }


        private static void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            var ne = (ShowEntitySuccessEventArgs)e;
            var data = (EntityData)ne.UserData;
            SEntityTcs.TryGetValue(data.Id, out var tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetResult(data.Id);
            SEntityTcs.Remove(data.Id);
        }

        private static void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            var ne = (ShowEntityFailureEventArgs)e;
            SEntityTcs.TryGetValue(ne.EntityId, out var tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            SEntityTcs.Remove(ne.EntityId);
        }

        /// <summary>
        /// 隐藏实体（可等待）
        /// </summary>
        public static UniTask HideEntityAsync(this EntityComponent entityComponent, int entityID)
        {
            var tcs = new UniTaskCompletionSource();
            SEntityHideTcs.Add(entityID, tcs);
            entityComponent.HideEntity(entityID);
            return tcs.Task;
        }

        private static void OnHideEntitySuccess(object sender, GameEventArgs e)
        {
            var arg = e as HideEntityCompleteEventArgs;
            if (arg == null)
            {
                return;
            }

            SEntityHideTcs.TryGetValue(arg.EntityId, out var tcs);
            if (tcs == null)
            {
                return;
            }

            tcs.TrySetResult();
            SEntityHideTcs.Remove(arg.EntityId);
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
