using System.Collections.Generic;

namespace LFramework.Runtime
{
    public static class DictionaryExtensions
    {
        public static void SafeAddNumber<TKey>(this Dictionary<TKey, int> dict, TKey key, int value)
        {
            if (dict.TryGetValue(key, out var v))
            {
                dict[key] = v + value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }
    }
}