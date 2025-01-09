using System.Collections.Concurrent;
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

    /// <summary>
    /// Dequeues a batch of items from a <see cref="ConcurrentQueue{T}"/>.
    /// </summary>
    /// <param name="queue">The queue.</param>
    /// <param name="batchSize">The batch size.</param>
    /// <typeparam name="T">The type of the queue items.</typeparam>
    /// <returns>A list of dequeued items.</returns>
    public static IReadOnlyCollection<T> DequeueBatch<T>(this ConcurrentQueue<T> queue, int batchSize) {
        var items = new List<T>(batchSize);
        for (var i = 0; i < batchSize; i++)
        {
            if (queue.TryDequeue(out var item))
            {
                items.Add(item);
            }
            else
            {
                break; // Exit if the queue is empty before reaching the batch size
            }
        }
        return items;
    }

    /// <summary>
    /// Dequeues a batch of items from a <see cref="ConcurrentQueue{T}"/>.
    /// </summary>
    /// <param name="queue">The queue.</param>
    /// <param name="batchSize">The batch size.</param>
    /// <param name="items">The items to return.</param>
    /// <typeparam name="T">The type of the queue items.</typeparam>
    /// <returns><c>True</c> if any items are in the batch, otherwise <c>false</c></returns>
    public static bool TryDequeueBatch<T>(
        this ConcurrentQueue<T> queue,
        int batchSize,
        out IReadOnlyCollection<T> items)
    {
        items = queue.DequeueBatch(batchSize);
        return items.Count > 0;
    }
}