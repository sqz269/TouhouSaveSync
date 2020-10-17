using System;
using System.Collections.Generic;
using System.Text;

namespace TouhouSaveSync.Utility
{
    static class DictionaryExtension
    {
        public static TValue GetValueOrDefault<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue ret;
            // Ignore return value
            dictionary.TryGetValue(key, out ret);
            return ret;
        }
    }
}
