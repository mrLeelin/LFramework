using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract class UnitySystemApplicationBehaviour : UnityEngine.MonoBehaviour, ISystemApplication
    {
        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        internal const int GameFrameworkSceneId = 0;


        private readonly GameFrameworkLinkedList<GameFrameworkComponent> _gameFrameworkComponents = new();

        /// <summary>
        /// The game framework components that are registered in the game framework.
        /// </summary>
        protected GameFrameworkLinkedList<GameFrameworkComponent> RuntimeGameFrameworkComponents =>
            _gameFrameworkComponents;

        protected virtual void StartApplication()
        {
            if (!RunStartupStage(
                    "AddSingleton",
                    () => SingletonManager.AddSingleton(new LFrameworkAspect()),
                    cleanupOnFailure: false))
            {
                return;
            }

            if (!RunStartupStage("RegisterSetting", RegisterSetting))
            {
                return;
            }

            if (!RunStartupStage("RegisterComponents", RegisterComponents))
            {
                return;
            }

            if (!RunStartupStage("BindComponents", BindComponents))
            {
                return;
            }

            if (!RunStartupStage("ResolveApplicationDependencies", ResolveApplicationDependencies))
            {
                return;
            }

            if (!RunStartupStage("StartComponents", StartComponents))
            {
                return;
            }

            if (!RunStartupStage("ApplicationStarted", ApplicationStarted))
            {
                return;
            }

            RunStartupStage("SetUpComponents", SetUpComponents);
        }

        private bool RunStartupStage(string stageName, Action action, bool cleanupOnFailure = true)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at {stageName}: {e}");
                if (cleanupOnFailure)
                {
                    CleanupFailedStartup(stageName);
                }

                return false;
            }
        }

        private bool RunStartupStage(string stageName, Func<bool> action)
        {
            try
            {
                if (action())
                {
                    return true;
                }

                CleanupFailedStartup(stageName);
                return false;
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at {stageName}: {e}");
                CleanupFailedStartup(stageName);
                return false;
            }
        }

        private void CleanupFailedStartup(string stageName)
        {
            try
            {
                StopApplication(ShutdownType.None);
            }
            catch (Exception cleanupException)
            {
                Log.Fatal($"StartApplication cleanup failed after {stageName}: {cleanupException}");
            }
        }

        protected virtual void Update()
        {
            foreach (var component in _gameFrameworkComponents)
            {
                component.UpdateComponent(Time.deltaTime, Time.unscaledDeltaTime);
            }

            SingletonManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            foreach (var component in _gameFrameworkComponents)
            {
                component.LateUpdate();
            }

            SingletonManager.LateUpdate();
        }

        #region Unity event functions

        private void OnApplicationFocus(bool hasFocus)
        {
            foreach (var component in RuntimeGameFrameworkComponents)
            {
                component.RuntimeOnApplicationFocus(hasFocus);
            }

            Log.Info($"[The application OnApplicationFocus {hasFocus}]");
            OnApplicationFocusInternal(hasFocus);
        }

        protected virtual void OnApplicationFocusInternal(bool hasFocus){}

        protected virtual void OnApplicationPauseInternal(bool pauseStatus){}

        protected virtual void OnApplicationQuitInternal(){}

        private void OnApplicationPause(bool pauseStatus)
        {
            foreach (var component in RuntimeGameFrameworkComponents)
            {
                component.RuntimeOnApplicationPause(pauseStatus);
            }

            Log.Info($"[The application OnApplicationPause {pauseStatus}]");
            OnApplicationPauseInternal(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            foreach (var component in RuntimeGameFrameworkComponents)
            {
                component.RuntimeOnApplicationQuit();
            }

            OnApplicationQuitInternal();
            StopApplication(ShutdownType.Quit);
        }

        #endregion

        public virtual void StopApplication(ShutdownType shutdownType)
        {
            foreach (var component in _gameFrameworkComponents)
            {
                component.ShutDown();
            }

            Log.Info("Shutdown Game Framework ({0})...", shutdownType);
            _gameFrameworkComponents.Clear();
            SingletonManager.Close();
            if (shutdownType == ShutdownType.None)
            {
                return;
            }

            if (shutdownType == ShutdownType.Restart)
            {
                SceneManager.LoadScene(GameFrameworkSceneId);
                return;
            }

            if (shutdownType == ShutdownType.Quit)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }
        }

        protected virtual void RegisterComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                //符合原来的架构
                UnityGameFramework.Runtime.GameEntry.RegisterComponent(gameFrameworkComponent);
            }
        }

        protected virtual bool RegisterSetting()
        {
            return true;
        }

        protected virtual void BindComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                LServices.Register(gameFrameworkComponent.GetType(), gameFrameworkComponent);
            }
        }

        protected virtual void ResolveApplicationDependencies()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                Injection.Inject(gameFrameworkComponent);
            }
        }

        protected virtual void StartComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                gameFrameworkComponent.Parent = this.gameObject;
                gameFrameworkComponent.AwakeComponent();
            }

            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                gameFrameworkComponent.StartComponent();
            }
        }

        protected virtual void SetUpComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                gameFrameworkComponent.SetUpComponent();
            }
        }

        protected abstract void ApplicationStarted();


        protected void RegisterComponent(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                Log.Error("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            LinkedListNode<GameFrameworkComponent> current = _gameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    Log.Error("Game Framework component type '{0}' is already exist.", type.FullName);
                    return;
                }

                current = current.Next;
            }

            _gameFrameworkComponents.AddLast(gameFrameworkComponent);
        }
    }
}
