using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PostHog.Library;

/// <summary>
/// Helpful extensions to collections.
/// </summary>
internal static class CollectionExtensions
{
    /// <summary>
    /// Wraps an <see cref="IEnumerable{T}"/> in a <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <param name="enumerable">The <see cref="IEnumerable{T}"/>.</param>
    /// <typeparam name="T">The type of objects to enumerate.</typeparam>
    /// <returns>The <see cref="IReadOnlyList{T}"/> that wraps the enumerable.</returns>
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> enumerable)
    {
        return new ReadOnlyCollection<T>(enumerable.ToList());
    }

    /// <summary>
    /// Filters out null values from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(item => item is not null)!;
    }
}