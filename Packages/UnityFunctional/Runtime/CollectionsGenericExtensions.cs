﻿using System;
using System.Collections.Generic;

namespace Bravasoft.Unity.Functional
{
    public static class CollectionsGenericExtensions
    {
        public static Option<T> FirstOrNone<T>(this List<T> list, Func<T, bool> predicate)
        {
            for (int i = 0; i < list.Count; ++i)
                if (predicate(list[i]))
                    return list[i];

            return Option<T>.None;
        }

        public static Option<TValue> TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key) =>
            dict.TryGetValue(key, out var value) ? Option.Some(value) : Option.None;

        public static Option<TValue> TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) =>
            dict.TryGetValue(key, out var value) ? Option.Some(value) : Option.None;
    }
}
