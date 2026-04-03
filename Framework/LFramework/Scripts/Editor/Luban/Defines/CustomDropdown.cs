using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace Luban.Editor
{
    [Serializable]
    public class CustomDropDownDic : UnitySerializedDictionary<string, string>
    {
        private readonly Dictionary<string, CachedDropdown> _dropdownCache = new();
        private List<ValueDropdownItem<string>> _keyDropdownCache;
        private string _keyDropdownSignature;

        public IEnumerable<ValueDropdownItem<string>> GetKeyDropdown()
        {
            string signature = string.Join("\u001f", Keys.OrderBy(static key => key, StringComparer.Ordinal));
            if (_keyDropdownCache != null && _keyDropdownSignature == signature)
            {
                return _keyDropdownCache;
            }

            var items = new List<ValueDropdownItem<string>>();
            foreach (var key in Keys)
            {
                items.Add(new ValueDropdownItem<string>(key, key));
            }

            _keyDropdownSignature = signature;
            _keyDropdownCache = items;
            return items;
        }

        public IEnumerable<ValueDropdownItem<string>> GetDropdown(string key, bool append_empty)
        {
            if (key is null)
            {
                return append_empty
                    ? new List<ValueDropdownItem<string>> { new("", "") }
                    : Array.Empty<ValueDropdownItem<string>>();
            }

            TryGetValue(key, out var values);
            string source = values ?? string.Empty;
            string cacheKey = $"{key}|{append_empty}";
            if (_dropdownCache.TryGetValue(cacheKey, out var cached) && cached.Source == source)
            {
                return cached.Items;
            }

            var items = new List<ValueDropdownItem<string>>();
            if (append_empty)
            {
                items.Add(new ValueDropdownItem<string>("无", ""));
            }

            if (!string.IsNullOrEmpty(values))
            {
                source = values.Replace(" ", "");
                foreach (var value in source.Split(","))
                {
                    items.Add(new ValueDropdownItem<string>(value, value));
                }
            }

            _dropdownCache[cacheKey] = new CachedDropdown(source, items);
            return items;
        }

        private readonly struct CachedDropdown
        {
            public CachedDropdown(string source, List<ValueDropdownItem<string>> items)
            {
                Source = source;
                Items = items;
            }

            public string Source { get; }
            public List<ValueDropdownItem<string>> Items { get; }
        }
    }
}
