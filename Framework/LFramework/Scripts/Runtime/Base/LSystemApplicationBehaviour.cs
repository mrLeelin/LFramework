using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public abstract class LSystemApplicationBehaviour : UnitySystemApplicationBehaviour
    {
        [Inject] private DebuggerComponent DebuggerComponent { get; }
        [Inject] private EventComponent EventComponent { get; }
  

        [SerializeField] private string[] allComponentTypes;
        [SerializeField] private List<ComponentSetting> allSettings;


        protected virtual void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            DiContainer = new DiContainer();
            StartApplication();
        }

        protected override void RegisterSetting()
        {
            base.RegisterSetting();

            // 从 BaseComponentSetting 获取 GameSetting
            var baseComponentSetting = allSettings.OfType<BaseComponentSetting>().FirstOrDefault();
            if (baseComponentSetting == null)
            {
                Log.Fatal("BaseComponentSetting not found in allSettings!");
                return;
            }

            var gameSetting = baseComponentSetting.GameSetting;
            if (gameSetting == null)
            {
                Log.Fatal("GameSetting is null in BaseComponentSetting! Please assign it in the inspector.");
                return;
            }

            DiContainer.Bind<GameSetting>().FromInstance(gameSetting).AsSingle();
            Log.Info($"[LSystemApplicationBehaviour] GameSetting bound to DI: {gameSetting.name}");
        }

        protected override void RegisterComponents()
        {
            if (allComponentTypes is not { Length: > 0 })
            {
                Log.Fatal("None components this is error in game.");
                return;
            }

            var settingsDict = SettingToDict(allSettings);
            foreach (var fullName in allComponentTypes)
            {
                var fType = Utility.Assembly.GetType(fullName);
                if (fType == null)
                {
                    Log.Error($"Type '{fullName}' is null, skipping.");
                    continue;
                }

                if (fType.IsAbstract ||
                    fType.IsInterface ||
                    !typeof(GameFrameworkComponent).IsAssignableFrom(fType))
                {
                    continue;
                }

                settingsDict.TryGetValue(fullName, out var setting);
                var component = ComponentHelper.CreateComponent(fType, setting);
                if (component == null)
                {
                    continue;
                }

                RegisterComponent(component);
            }

            base.RegisterComponents();
        }

        protected override void ResolveApplicationDependencies()
        {
            base.ResolveApplicationDependencies();
            DiContainer.Inject(this);
        }

        protected override void ApplicationStarted()
        {
            DiContainer.Bind<ISystemApplication>().FromInstance(this).AsSingle();
            AwaitableExtensions.SubscribeEvent(EventComponent);
            
        }

      

        public override void StopApplication(ShutdownType shutdownType)
        {
            base.StopApplication(shutdownType);
           
        }


        private Dictionary<string, ComponentSetting> SettingToDict(List<ComponentSetting> settings)
        {
            return settings.ToDictionary(k => k.bindTypeName, v => v);
        }
        
        private void OnGUI()
        {
            if (DebuggerComponent != null)
            {
                DebuggerComponent.OnGUI();
            }
        }
    }
}