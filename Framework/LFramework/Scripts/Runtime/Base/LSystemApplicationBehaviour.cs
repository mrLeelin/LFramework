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

        protected override bool RegisterSetting()
        {

            // 使用 SettingManager 获取 ProjectSettingSelector
            var selector = SettingManager.GetProjectSelector();
            if (selector == null)
            {
                Debug.LogError(GetMissingProjectSelectorGuidanceMessage());
                return false;
            }

            // 自动绑定所有 Setting 到 DI 容器
            var allSettingsFromSelector = selector.GetAllSettings();
            foreach (var setting in allSettingsFromSelector)
            {
                if (setting == null) continue;

                var settingType = setting.GetType();
                DiContainer.Bind(settingType).FromInstance(setting).AsSingle();
                Debug.Log($"[LSystemApplicationBehaviour] {settingType.Name} bound to DI: {setting.name}");
            }

            // 验证 GameSetting 是否存在
            var gameSetting = SettingManager.GetSetting<GameSetting>();
            if (gameSetting == null)
            {
                Debug.LogError("GameSetting not found in ProjectSettingSelector! Please assign it.");
                return false;
            }

            return true;
        }

        protected override void RegisterComponents()
        {
            if (allComponentTypes is not { Length: > 0 })
            {
                Log.Fatal("None components this is error in game.");
                return;
            }

            var effectiveSettings = ResolveComponentSettingsForRegistration(SettingManager.GetProjectSelector());
            var settingsDict = SettingToDict(effectiveSettings);
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

        /// <summary>
        /// 缺失 ProjectSettingSelector 时的引导信息
        /// </summary>
        public static string GetMissingProjectSelectorGuidanceMessage()
        {
            return "ProjectSettingSelector not found! " +
                   "Please open 'LFramework/GameSetting' -> 'Framework Setting' and click '初始化 Project Settings' " +
                   "to generate 'Assets/Game/Resources/ProjectSettingSelector.asset'.";
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

        /// <summary>
        /// 解析组件注册使用的 Setting 列表。
        /// 优先使用工程侧 ProjectSettingSelector，历史项目回退到序列化列表。
        /// </summary>
        public static List<ComponentSetting> ResolveComponentSettingsForRegistration(
            ProjectSettingSelector projectSelector)
        {
            if (projectSelector != null)
            {
                var projectSettings = projectSelector.GetAllComponentSettings();
                if (projectSettings.Count > 0)
                {
                    return projectSettings;
                }
            }

            return new List<ComponentSetting>();
        }

        private Dictionary<string, ComponentSetting> SettingToDict(List<ComponentSetting> settings)
        {
            return settings
                .Where(setting => setting != null && !string.IsNullOrWhiteSpace(setting.bindTypeName))
                .GroupBy(setting => setting.bindTypeName)
                .ToDictionary(group => group.Key, group => group.First());
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
