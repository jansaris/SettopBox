using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedComponents
{
    public static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static Dictionary<K, V> Clone<K, V>(this Dictionary<K, V> original) where V : ICloneable
        {
            Dictionary<K, V> ret = new Dictionary<K, V>(original.Count, original.Comparer);
            foreach (KeyValuePair<K, V> entry in original)
            {
                ret.Add(entry.Key, (V)entry.Value.Clone());
            }
            return ret;
        }
    }
}
