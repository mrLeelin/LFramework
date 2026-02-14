using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public abstract class UnitySystemApplicationBehaviour : UnityEngine.MonoBehaviour, ISystemApplication
    {
        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        internal const int GameFrameworkSceneId = 0;


        private readonly GameFrameworkLinkedList<GameFrameworkComponent> _gameFrameworkComponents = new();
        public DiContainer DiContainer { get; protected set; }

        /// <summary>
        /// The game framework components that are registered in the game framework.
        /// </summary>
        protected GameFrameworkLinkedList<GameFrameworkComponent> RuntimeGameFrameworkComponents =>
            _gameFrameworkComponents;

        protected virtual void StartApplication()
        {
            SingletonManager.AddSingleton(new LFrameworkAspect(DiContainer));
            RegisterSetting();
            RegisterComponents();
            BindComponents();
            ResolveApplicationDependencies();
            StartComponents();
            ApplicationStarted();
            SetUpComponents();
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

        protected virtual void OnApplicationQuit()
        {
            StopApplication(ShutdownType.Quit);
        }

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

        protected virtual void RegisterSetting()
        {
        }

        protected virtual void BindComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                DiContainer.Bind(gameFrameworkComponent.GetType()).FromInstance(gameFrameworkComponent).AsSingle();
            }
        }

        protected virtual void ResolveApplicationDependencies()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                DiContainer.Inject(gameFrameworkComponent);
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