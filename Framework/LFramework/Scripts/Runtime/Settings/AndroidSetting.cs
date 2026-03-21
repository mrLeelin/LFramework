using UnityEngine;
using Sirenix.OdinInspector;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Android 平台配置
    /// </summary>
    [CreateAssetMenu(fileName = "AndroidSetting", menuName = "LFramework/Settings/AndroidSetting")]
    public class AndroidSetting : BaseSetting
    {
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}
