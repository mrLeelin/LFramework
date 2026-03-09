using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Localization;
using GameFramework.Procedure;
using LFramework.Runtime.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    public class GameLaunchProcedure : RuntimeBaseProcedure
    {
        [Inject] private BaseComponent BaseComponent { get; }
        [Inject] private LanguageComponent LocalizationComponent { get; }
        [Inject] private SettingComponent SettingComponent { get; }
        [Inject] private ResourceComponent ResourceComponent { get; }

        [Inject] private SoundComponent SoundComponent { get; }

        [Inject] private UIComponent UIComponent { get; }
        [Inject] private ConfigComponent ConfigComponent { get; }
        [Inject] private GameNotificationsComponent GameNotificationsComponent { get; }

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("Enter [GameLaunchProcedure].");

            InitLanguageSettings();
            InitCurrentVariant();
            InitSoundSettings();
        }


        protected virtual void InitLanguageSettings()
        {
            if (BaseComponent.EditorResourceMode && BaseComponent.EditorLanguage != Language.Unspecified)
            {
                // 编辑器资源模式直接使用 Inspector 上设置的语言
                LocalizationComponent.Language = BaseComponent.EditorLanguage;
                return;
            }


            Language language = LocalizationComponent.SystemLanguage;

            if (SettingComponent.HasSetting(Constant.Setting.Language))
            {
                try
                {
                    string languageString = SettingComponent.GetString(Constant.Setting.Language);
                    language = (Language)Enum.Parse(typeof(Language), languageString);
                }
                catch
                {
                    //ignore
                }
            }

            if (language != Language.English
                && language != Language.ChineseSimplified
                && language != Language.French
                //&& language != Language.ChineseTraditional
                // && language != Language.Korean
               )
            {
                // 若是暂不支持的语言，则使用英语
                language = Language.English;

                SettingComponent.SetString(Constant.Setting.Language, language.ToString());
                SettingComponent.Save();
            }

            LocalizationComponent.Language = language;
            Log.Info("Init language settings complete, current language is '{0}'.",
                LocalizationComponent.Language.ToString());
        }

        protected virtual void InitCurrentVariant()
        {
            Log.Info("Init current variant complete.");
        }

        private void InitSoundSettings()
        {
            if (SettingComponent.HasSetting(Constant.Setting.MusicMuted))
            {
                SoundComponent.Mute("Music", SettingComponent.GetBool(Constant.Setting.MusicMuted, false));
            }


            if (SettingComponent.HasSetting(Constant.Setting.MusicVolume))
            {
                SoundComponent.SetVolume("Music", SettingComponent.GetFloat(Constant.Setting.MusicVolume, 1));
            }


            if (SettingComponent.HasSetting(Constant.Setting.SoundMuted))
            {
                SoundComponent.Mute("Sound", SettingComponent.GetBool(Constant.Setting.SoundMuted, false));
                SoundComponent.Mute("UISound", SettingComponent.GetBool(Constant.Setting.SoundMuted, false));
            }


            if (SettingComponent.HasSetting(Constant.Setting.SoundVolume))
            {
                SoundComponent.SetVolume("Sound", SettingComponent.GetFloat(Constant.Setting.SoundVolume, 1));
                SoundComponent.SetVolume("UISound", SettingComponent.GetFloat(Constant.Setting.SoundVolume, 1));
            }


            Log.Info("Init sound settings complete.");
        }
    }
}