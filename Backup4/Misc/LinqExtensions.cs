using System;
using System.Collections.Generic;
using System.Linq;

namespace Backup4.Synchronization
{
    public static class LinqExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var x in enumerable)
            {
                action(x);
            }
        }

        public static bool None<T>(this IEnumerable<T> enumerable) => !enumerable.Any();
        
        public static bool None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) => !enumerable.Any(predicate);
    }
}