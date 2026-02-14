using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Localization;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public class LanguageComponent : GameFrameworkComponent
    {

        [Inject] private BaseComponent BaseComponent { get; }
        
        private ILanguageHelper _localizationHelper;
        private Language _language;


        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set
            {
                if (value == Language.Unspecified)
                {
                    throw new GameFrameworkException("Language is invalid.");
                }

                _language = value;
            }
        }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public Language SystemLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Afrikaans: return Language.Afrikaans;
                    case UnityEngine.SystemLanguage.Arabic: return Language.Arabic;
                    case UnityEngine.SystemLanguage.Basque: return Language.Basque;
                    case UnityEngine.SystemLanguage.Belarusian: return Language.Belarusian;
                    case UnityEngine.SystemLanguage.Bulgarian: return Language.Bulgarian;
                    case UnityEngine.SystemLanguage.Catalan: return Language.Catalan;
                    case UnityEngine.SystemLanguage.Chinese: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.ChineseSimplified: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.ChineseTraditional: return Language.ChineseTraditional;
                    case UnityEngine.SystemLanguage.Czech: return Language.Czech;
                    case UnityEngine.SystemLanguage.Danish: return Language.Danish;
                    case UnityEngine.SystemLanguage.Dutch: return Language.Dutch;
                    case UnityEngine.SystemLanguage.English: return Language.English;
                    case UnityEngine.SystemLanguage.Estonian: return Language.Estonian;
                    case UnityEngine.SystemLanguage.Faroese: return Language.Faroese;
                    case UnityEngine.SystemLanguage.Finnish: return Language.Finnish;
                    case UnityEngine.SystemLanguage.French: return Language.French;
                    case UnityEngine.SystemLanguage.German: return Language.German;
                    case UnityEngine.SystemLanguage.Greek: return Language.Greek;
                    case UnityEngine.SystemLanguage.Hebrew: return Language.Hebrew;
                    case UnityEngine.SystemLanguage.Hungarian: return Language.Hungarian;
                    case UnityEngine.SystemLanguage.Icelandic: return Language.Icelandic;
                    case UnityEngine.SystemLanguage.Indonesian: return Language.Indonesian;
                    case UnityEngine.SystemLanguage.Italian: return Language.Italian;
                    case UnityEngine.SystemLanguage.Japanese: return Language.Japanese;
                    case UnityEngine.SystemLanguage.Korean: return Language.Korean;
                    case UnityEngine.SystemLanguage.Latvian: return Language.Latvian;
                    case UnityEngine.SystemLanguage.Lithuanian: return Language.Lithuanian;
                    case UnityEngine.SystemLanguage.Norwegian: return Language.Norwegian;
                    case UnityEngine.SystemLanguage.Polish: return Language.Polish;
                    case UnityEngine.SystemLanguage.Portuguese: return Language.PortuguesePortugal;
                    case UnityEngine.SystemLanguage.Romanian: return Language.Romanian;
                    case UnityEngine.SystemLanguage.Russian: return Language.Russian;
                    case UnityEngine.SystemLanguage.SerboCroatian: return Language.SerboCroatian;
                    case UnityEngine.SystemLanguage.Slovak: return Language.Slovak;
                    case UnityEngine.SystemLanguage.Slovenian: return Language.Slovenian;
                    case UnityEngine.SystemLanguage.Spanish: return Language.Spanish;
                    case UnityEngine.SystemLanguage.Swedish: return Language.Swedish;
                    case UnityEngine.SystemLanguage.Thai: return Language.Thai;
                    case UnityEngine.SystemLanguage.Turkish: return Language.Turkish;
                    case UnityEngine.SystemLanguage.Ukrainian: return Language.Ukrainian;
                    case UnityEngine.SystemLanguage.Unknown: return Language.Unspecified;
                    case UnityEngine.SystemLanguage.Vietnamese: return Language.Vietnamese;
                    default: return Language.English;
                }
                /*
                if (_localizationHelper == null)
                {
                    throw new GameFrameworkException("You must set localization helper first.");
                }

                return _localizationHelper.SystemLanguage;
                */
            }
        }

        /// <summary>
        /// 获取字典数量。
        /// </summary>
        public int DictionaryCount
        {
            get { return _localizationHelper.DictionaryCount; }
        }

        public override void AwakeComponent()
        {
            base.AwakeComponent();
            CreateInstance();
        }

        public override void StartComponent()
        {
            base.StartComponent();
          
        }

        public void SetLocalizationHelper(ILanguageHelper languageHelper)
        {
            _localizationHelper = languageHelper;
            if (_localizationHelper == null)
            {
                throw new GameFrameworkException("You must set localization helper first.");
            }

            var helperBase = _localizationHelper as LanguageHelperBase;
            if (helperBase == null)
            {
                return;
            }

            helperBase.name = "Localization Helper";
            Transform transform = helperBase.transform;
            transform.SetParent(Instance);
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString(string key)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            return value;
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T">字典参数的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg">字典参数。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T>(string key, T arg)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, arg, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2>(string key, T1 arg1, T2 arg2)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4}", key, value, arg1, arg2, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5}", key, value, arg1, arg2, arg3, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6}", key, value, arg1, arg2, arg3, arg4,
                    exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7}", key, value, arg1, arg2, arg3, arg4,
                    arg5, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8}", key, value, arg1, arg2, arg3,
                    arg4, arg5, arg6, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", key, value, arg1, arg2,
                    arg3, arg4, arg5, arg6, arg7, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7, T8 arg8)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", key, value, arg1,
                    arg2, arg3, arg4, arg5, arg6, arg7, arg8, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", key, value, arg1,
                    arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", key, value,
                    arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string key, T1 arg1, T2 arg2, T3 arg3,
            T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", key,
                    value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string key, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                    arg12);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string key, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                    arg12, arg13);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format(
                    "<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", key, value, arg1,
                    arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string key, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12,
            T13 arg13, T14 arg14)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                    arg12, arg13, arg14);
            }
            catch (Exception exception)
            {
                string args = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", arg1,
                    arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <typeparam name="T15">字典参数 15 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <param name="arg15">字典参数 15。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string key, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12,
            T13 arg13, T14 arg14, T15 arg15)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                    arg12, arg13, arg14, arg15);
            }
            catch (Exception exception)
            {
                string args = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 根据字典主键获取字典内容字符串。
        /// </summary>
        /// <typeparam name="T1">字典参数 1 的类型。</typeparam>
        /// <typeparam name="T2">字典参数 2 的类型。</typeparam>
        /// <typeparam name="T3">字典参数 3 的类型。</typeparam>
        /// <typeparam name="T4">字典参数 4 的类型。</typeparam>
        /// <typeparam name="T5">字典参数 5 的类型。</typeparam>
        /// <typeparam name="T6">字典参数 6 的类型。</typeparam>
        /// <typeparam name="T7">字典参数 7 的类型。</typeparam>
        /// <typeparam name="T8">字典参数 8 的类型。</typeparam>
        /// <typeparam name="T9">字典参数 9 的类型。</typeparam>
        /// <typeparam name="T10">字典参数 10 的类型。</typeparam>
        /// <typeparam name="T11">字典参数 11 的类型。</typeparam>
        /// <typeparam name="T12">字典参数 12 的类型。</typeparam>
        /// <typeparam name="T13">字典参数 13 的类型。</typeparam>
        /// <typeparam name="T14">字典参数 14 的类型。</typeparam>
        /// <typeparam name="T15">字典参数 15 的类型。</typeparam>
        /// <typeparam name="T16">字典参数 16 的类型。</typeparam>
        /// <param name="key">字典主键。</param>
        /// <param name="arg1">字典参数 1。</param>
        /// <param name="arg2">字典参数 2。</param>
        /// <param name="arg3">字典参数 3。</param>
        /// <param name="arg4">字典参数 4。</param>
        /// <param name="arg5">字典参数 5。</param>
        /// <param name="arg6">字典参数 6。</param>
        /// <param name="arg7">字典参数 7。</param>
        /// <param name="arg8">字典参数 8。</param>
        /// <param name="arg9">字典参数 9。</param>
        /// <param name="arg10">字典参数 10。</param>
        /// <param name="arg11">字典参数 11。</param>
        /// <param name="arg12">字典参数 12。</param>
        /// <param name="arg13">字典参数 13。</param>
        /// <param name="arg14">字典参数 14。</param>
        /// <param name="arg15">字典参数 15。</param>
        /// <param name="arg16">字典参数 16。</param>
        /// <returns>要获取的字典内容字符串。</returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string key,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11,
            T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                    arg12, arg13, arg14, arg15, arg16);
            }
            catch (Exception exception)
            {
                string args =
                    Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", arg1,
                        arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15,
                        arg16);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 是否存在字典。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>是否存在字典。</returns>
        public bool HasRawString(string key)
        {
            if (_localizationHelper == null)
            {
                throw new GameFrameworkException("You must set localization helper first.");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new GameFrameworkException("Key is invalid.");
            }

            return _localizationHelper.HasRawString(key);
        }

        /// <summary>
        /// 根据字典主键获取字典值。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <returns>字典值。</returns>
        private string GetRawString(string key)
        {
            if (_localizationHelper == null)
            {
                throw new GameFrameworkException("You must set localization helper first.");
            }

            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return _localizationHelper.GetRawString(key, Language);
        }
    }
}