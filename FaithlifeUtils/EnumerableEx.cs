using System;
using System.Collections.Generic;

namespace FaithlifeUtils;

/// <summary>
/// Holds extension methods for the <see cref="IEnumerable{T}" /> interface
/// </summary>
public static class EnumerableEx
{
    /// <summary>
    /// Performs the specified action on each element of the <see cref="List&lt;T&gt;" />
    /// </summary>
    /// <param name="source">The source <see cref="List&lt;T&gt;" /></param>
    /// <param name="action">The <see cref="Action&lt;T&gt;" /> delegate to perform on each element</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var element in source)
            action(element);
    }
}