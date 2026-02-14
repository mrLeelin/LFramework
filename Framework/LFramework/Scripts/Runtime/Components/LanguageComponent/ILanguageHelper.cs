using System.Collections;
using System.Collections.Generic;
using GameFramework.Localization;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface ILanguageHelper : ILocalizationHelper 
    {
      
        /// <summary>
        /// 字典数量
        /// </summary>
        public int DictionaryCount { get; }

        /// <summary>
        /// 是否包含多语言
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool HasRawString(string key);


        /// <summary>
        /// 获取多语言
        /// </summary>
        /// <param name="key"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        string GetRawString(string key,Language language);
    }
}

