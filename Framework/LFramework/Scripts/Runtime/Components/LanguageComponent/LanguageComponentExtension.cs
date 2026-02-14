using System.Collections;
using System.Collections.Generic;
using GameFramework.Localization;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class LanguageComponentExtension
    {
        public static string GetLanguageShort(this Language language)
        {
            switch (language)
            {
                case Language.English:
                    return "en";
                case Language.ChineseSimplified:
                    return "zh";
                case Language.ChineseTraditional:
                    return "zh_tw";
                default:
                    return "en";
            }
        }
    }
}