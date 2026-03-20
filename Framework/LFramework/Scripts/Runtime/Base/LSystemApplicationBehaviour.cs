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
    
    /// <summary>
    /// 启动器
    /// 可以继承这个启动器启动游戏
    /// </summary>
    public class LSystemApplicationBehaviour : UnitySystemApplicationBehaviour
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

            // 使用 SettingManager 获取 SettingSelector
            var selector = SettingManager.GetSelector();
            if (selector == null)
            {
                Log.Fatal("SettingSelector not found! Please create a SettingSelector asset.");
                return;
            }

            // 自动绑定所有 Setting 到 DI 容器
            var allSettingsFromSelector = selector.GetAllSettings();
            foreach (var setting in allSettingsFromSelector)
            {
                if (setting == null) continue;

                var settingType = setting.GetType();
                DiContainer.Bind(settingType).FromInstance(setting).AsSingle();
                Log.Info($"[LSystemApplicationBehaviour] {settingType.Name} bound to DI: {setting.name}");
            }

            // 验证 GameSetting 是否存在
            var gameSetting = SettingManager.GetSetting<GameSetting>();
            if (gameSetting == null)
            {
                Log.Fatal("GameSetting not found in SettingSelector! Please assign it.");
            }
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