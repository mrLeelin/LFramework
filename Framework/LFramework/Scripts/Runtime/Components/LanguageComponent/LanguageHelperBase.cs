using System.Collections;
using System.Collections.Generic;
using GameFramework.Localization;
using UnityEngine;

namespace LFramework.Runtime
{
    public abstract class LanguageHelperBase : UnityEngine.MonoBehaviour, ILanguageHelper
    {
        /// <summary>
        /// 系统语言
        /// </summary>
        public abstract Language SystemLanguage { get; }
        /// <summary>
        /// 多语言数量
        /// </summary>
        public abstract int DictionaryCount { get; }
        /// <summary>
        /// 是否拥有多语言
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool HasRawString(string key);
        /// <summary>
        /// 获取多语言
        /// </summary>
        /// <param name="key"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public abstract string GetRawString(string key, Language language);
    }

}

