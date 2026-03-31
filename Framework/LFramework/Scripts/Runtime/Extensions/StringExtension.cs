using System;
using System.Text.RegularExpressions;
using ModestTree;
using UnityEngine;

namespace LFramework.Runtime
{
    public static class StringExtension
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\{(.*?)\}", RegexOptions.Compiled);

        /// <summary>
        ///  替换枚举占位符
        /// </summary>
        /// <param name="input"></param>
        /// <param name="getDescription"></param>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static string ReplaceEnumPlaceholders<TEnum>(this string input, Func<TEnum, string> getDescription)
            where TEnum : struct, Enum
        {
            return PlaceholderRegex.Replace(input, match =>
            {
                var token = match.Groups[1].Value;
                return Enum.TryParse(token, out TEnum enumValue) ? getDescription(enumValue) : match.Value; // 保留原样
            });
        }

        /// <summary>
        ///  替换占位符
        /// </summary>
        /// <param name="input"></param>
        /// <param name="getDescription"></param>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static string ReplacePlaceholders(this string input, Func<string, string> getDescription)
        {
            return PlaceholderRegex.Replace(input, match =>
            {
                var token = match.Groups[1].Value;
                return getDescription(token);
            });
        }

        /// <summary>
        /// Extracts main and subobject keys if properly formatted
        /// </summary>
        /// <param name="keyObj">The key as an object.</param>
        /// <param name="mainKey">The key of the main asset.  This will be set to null if a sub key is not found.</param>
        /// <param name="subKey">The key of the sub object.  This will be set to null if not found.</param>
        /// <returns>Returns true if properly formatted keys are extracted.</returns>
        public static bool ExtractKeyAndSubKey(this string keyObj, out string mainKey, out string subKey)
        {
            var key = keyObj;
            if (key != null)
            {
                var i = key.IndexOf('[');
                if (i > 0)
                {
                    var j = key.LastIndexOf(']');
                    if (j > i)
                    {
                        mainKey = key.Substring(0, i);
                        subKey = key.Substring(i + 1, j - (i + 1));
                        var pointIndex = key.LastIndexOf('.');
                        if (pointIndex > 0)
                        {
                            mainKey += key.Substring(pointIndex, key.Length - pointIndex);
                        }

                        return true;
                    }
                }
            }

            mainKey = null;
            subKey = null;
            return false;
        }
    }
}