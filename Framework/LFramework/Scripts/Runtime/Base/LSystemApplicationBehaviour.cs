using System;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Default framework bootstrap behaviour.
    /// </summary>
    /// <remarks>
    /// The bootstrap owns object creation only. Created settings and components are registered into
    /// <see cref="LServices"/> so generated <see cref="Inject"/> code can resolve dependencies without
    /// a runtime reflection container.
    /// </remarks>
    public partial class LSystemApplicationBehaviour : UnitySystemApplicationBehaviour
    {
        private DebuggerComponent DebuggerComponent { get; set; }
        private EventComponent EventComponent { get; set; }

        [SerializeField] private string[] allComponentTypes;

        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };

            StartApplication();
        }

        protected override bool RegisterSetting()
        {
            var selector = SettingManager.GetProjectSelector();
            if (selector == null)
            {
                Debug.LogError(GetMissingProjectSelectorGuidanceMessage());
                return false;
            }

            foreach (var setting in selector.GetAllSettings())
            {
                if (setting == null)
                {
                    continue;
                }

                var settingType = setting.GetType();
                LServices.Register(settingType, setting);
                Debug.Log($"[LSystemApplicationBehaviour] {settingType.Name} registered in LServices: {setting.name}");
            }

            if (SettingManager.GetSetting<GameSetting>() == null)
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
                var componentType = Utility.Assembly.GetType(fullName);
                if (componentType == null)
                {
                    Log.Error($"Type '{fullName}' is null, skipping.");
                    continue;
                }

                if (componentType.IsAbstract ||
                    componentType.IsInterface ||
                    !typeof(GameFrameworkComponent).IsAssignableFrom(componentType))
                {
                    continue;
                }

                settingsDict.TryGetValue(fullName, out var setting);
                var component = ComponentHelper.CreateComponent(componentType, setting);
                if (component == null)
                {
                    continue;
                }

                RegisterComponent(component);
            }

            base.RegisterComponents();
        }

        /// <summary>
        /// User-facing guidance when the project selector asset is missing.
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
            LServices.Inject(this);
            ResolveFrameworkDependencies();
        }

        /// <summary>
        /// Resolves bootstrap-owned dependencies that must stay available even when a project subclass
        /// has its own generated injector implementation.
        /// </summary>
        private void ResolveFrameworkDependencies()
        {
            DebuggerComponent = LServices.Get<DebuggerComponent>();
            EventComponent = LServices.Get<EventComponent>();
        }

        protected override void ApplicationStarted()
        {
            LServices.Register<ISystemApplication>(this);
            AwaitableExtensions.SubscribeEvent(EventComponent);
        }

        public override void StopApplication(ShutdownType shutdownType)
        {
            base.StopApplication(shutdownType);
        }

        /// <summary>
        /// Resolves component settings used while creating framework components.
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
