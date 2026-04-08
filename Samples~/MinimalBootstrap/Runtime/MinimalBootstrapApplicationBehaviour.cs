using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;
using Zenject;

namespace LFramework.Samples.MinimalBootstrap
{
    /// <summary>
    /// Self-contained sample bootstrap so the imported sample does not depend on project-global settings.
    /// </summary>
    public sealed class MinimalBootstrapApplicationBehaviour : LSystemApplicationBehaviour
    {
        [SerializeField] private ProjectSettingSelector sampleProjectSettingSelector;

        protected override bool RegisterSetting()
        {
            if (sampleProjectSettingSelector == null)
            {
                Debug.LogError("[MinimalBootstrap] Sample ProjectSettingSelector is missing.");
                return false;
            }

            foreach (BaseSetting setting in sampleProjectSettingSelector.GetAllSettings())
            {
                if (setting == null)
                {
                    continue;
                }

                DiContainer.Bind(setting.GetType()).FromInstance(setting).AsSingle();
            }

            if (sampleProjectSettingSelector.GetSetting<GameSetting>() == null)
            {
                Debug.LogError("[MinimalBootstrap] GameSetting is missing from the sample selector.");
                return false;
            }

            return true;
        }
    }
}
