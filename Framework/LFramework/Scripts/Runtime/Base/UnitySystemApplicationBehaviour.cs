using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;
using VContainer;

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
        /// The root VContainer resolver, built during StartApplication.
        /// Replaces the former Zenject DiContainer property.
        /// </summary>
        protected IObjectResolver RootResolver { get; private set; }

        /// <summary>
        /// Temporary builder reference available only during the scope-building phase of StartApplication.
        /// Subclasses use this in RegisterSetting / BindComponents / OnConfigureRootScope.
        /// </summary>
        protected IContainerBuilder ScopeBuilder { get; private set; }

        /// <summary>
        /// The game framework components that are registered in the game framework.
        /// </summary>
        protected GameFrameworkLinkedList<GameFrameworkComponent> RuntimeGameFrameworkComponents =>
            _gameFrameworkComponents;

        protected virtual void StartApplication()
        {
            FrameworkResolverContext resolverContext;
            try
            {
                resolverContext = new FrameworkResolverContext();
                ScopeBuilder = new ContainerBuilder();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at creating resolver context: {e}");
                return;
            }

            try
            {
                if (!RegisterSetting())
                {
                    ScopeBuilder = null;
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at RegisterSetting: {e}");
                ScopeBuilder = null;
                return;
            }

            try
            {
                RegisterComponents();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at RegisterComponents: {e}");
                ScopeBuilder = null;
                return;
            }

            try
            {
                BindComponents();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at BindComponents: {e}");
                ScopeBuilder = null;
                return;
            }

            try
            {
                OnConfigureRootScope(ScopeBuilder);
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at OnConfigureRootScope: {e}");
                ScopeBuilder = null;
                return;
            }

            // Build the root scope and wire up the resolver context
            try
            {
                RootResolver = ((ContainerBuilder)ScopeBuilder).Build();
                ScopeBuilder = null; // builder is no longer usable after Build()
                resolverContext.SetRoot(RootResolver);
                SingletonManager.AddSingleton(new LFrameworkAspect(resolverContext));
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at building root scope: {e}");
                ScopeBuilder = null;
                return;
            }

            try
            {
                ResolveApplicationDependencies();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at ResolveApplicationDependencies: {e}");
                return;
            }

            try
            {
                StartComponents();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at StartComponents: {e}");
                return;
            }

            try
            {
                ApplicationStarted();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at ApplicationStarted: {e}");
                return;
            }

            try
            {
                SetUpComponents();
            }
            catch (Exception e)
            {
                Log.Fatal($"StartApplication failed at SetUpComponents: {e}");
                return;
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

        protected virtual bool RegisterSetting()
        {
            return true;
        }

        protected virtual void BindComponents()
        {
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                ScopeBuilder.RegisterInstance(gameFrameworkComponent).As(gameFrameworkComponent.GetType());
            }
        }

        /// <summary>
        /// Override point for subclasses to add additional registrations to the root scope builder
        /// before it is built. Called after RegisterSetting, RegisterComponents, and BindComponents.
        /// </summary>
        protected virtual void OnConfigureRootScope(IContainerBuilder builder)
        {
        }

        protected virtual void ResolveApplicationDependencies()
        {
            var injector = LFrameworkAspect.Instance.FrameworkInjector;
            foreach (var gameFrameworkComponent in _gameFrameworkComponents)
            {
                injector.Inject(gameFrameworkComponent);
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
