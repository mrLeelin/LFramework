using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "BaseComponentSetting",
        menuName = "LFramework/Settings/BaseComponentSetting")]
    public sealed class BaseComponentSetting : ComponentSetting
    {
        // GameSetting 引用
        [Title("Game Setting Reference")]
        [InfoBox("请点击下方按钮选择要使用的 GameSetting", InfoMessageType.Warning, "IsGameSettingNull")]
        [SerializeField]
        private GameSetting gameSetting;

        [SerializeField] private bool m_EditorResourceMode = true;

        [SerializeField] private Language m_EditorLanguage = Language.Unspecified;

        [SerializeField] private string m_TextHelperTypeName = "UnityGameFramework.Runtime.DefaultTextHelper";

        [SerializeField] private string m_VersionHelperTypeName = "UnityGameFramework.Runtime.DefaultVersionHelper";

        [SerializeField] private string m_LogHelperTypeName = "UnityGameFramework.Runtime.DefaultLogHelper";

        [SerializeField] private string m_CompressionHelperTypeName = "UnityGameFramework.Runtime.DefaultCompressionHelper";

        [SerializeField] private string m_JsonHelperTypeName = "UnityGameFramework.Runtime.DefaultJsonHelper";

        [SerializeField] private int m_FrameRate = 30;

        [SerializeField] private float m_GameSpeed = 1f;

        [SerializeField] private bool m_RunInBackground = true;

        [SerializeField] private bool m_NeverSleep = true;

        // 公共访问器
        public GameSetting GameSetting => gameSetting;
        
      
    }
}