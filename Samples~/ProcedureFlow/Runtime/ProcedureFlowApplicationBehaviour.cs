using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;
using Zenject;

namespace LFramework.Samples.ProcedureFlow
{
    /// <summary>
    /// Uses a local selector asset so the sample remains deterministic after import.
    /// </summary>
    public sealed class ProcedureFlowApplicationBehaviour : LSystemApplicationBehaviour
    {
        [SerializeField] private ProjectSettingSelector sampleProjectSettingSelector;

        protected override bool RegisterSetting()
        {
            if (sampleProjectSettingSelector == null)
            {
                Debug.LogError("[ProcedureFlow] Sample ProjectSettingSelector is missing.");
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
                Debug.LogError("[ProcedureFlow] GameSetting is missing from the sample selector.");
                return false;
            }

            return true;
        }
    }
}
