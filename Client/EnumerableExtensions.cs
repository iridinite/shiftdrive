/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftDrive {

    /// <summary>
    /// Extension methods for <seealso cref="IEnumerable{T}"/>.
    /// </summary>
    internal static class EnumerableExtensions {

        /// <summary>
        /// Iterates over a collection and invokes the specified delegate on each item.
        /// </summary>
        /// <param name="source">The collection to iterate through.</param>
        /// <param name="fn">The function to invoke for each item.</param>
        public static void ForEach<TResult>(this IEnumerable<TResult> source, Action<TResult> fn) {
            var copy = source.ToArray(); // copy protects against enumerable modification errors
            foreach (var item in copy) {
                fn(item);
            }
        }

    }

}
