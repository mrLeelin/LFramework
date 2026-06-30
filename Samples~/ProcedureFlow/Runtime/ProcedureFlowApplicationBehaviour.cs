using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;

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

                LServices.Register(setting.GetType(), setting);
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
