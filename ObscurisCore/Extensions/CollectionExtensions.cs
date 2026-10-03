using NiveraAPI.Utilities;

namespace ObscurisCore.Extensions;

/// <summary>
/// Extensions for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Retrieves a random element from the collection that satisfies the specified predicate,
    /// or returns the default value of the type if the collection is null, empty, or no element matches the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="collection">The collection to retrieve the random element from.</param>
    /// <param name="predicate">An optional predicate to filter the elements in the collection.</param>
    /// <returns>A random element that satisfies the predicate, or the default value of the type if no such element exists or the collection is null.</returns>
    public static T GetRandomOrDefault<T>(this IEnumerable<T> collection, Predicate<T>? predicate = null)
    {
        if (collection == null)
            return default!;

        if (predicate != null)
            collection = collection.Where(obj => predicate(obj));
        
        var count = collection.Count();

        if (count < 1)
            return default!;

        var random = StaticRandom.GetInt(0, count - 1);

        if (random < 0 || random >= count)
            return default!;

        return collection.ElementAtOrDefault(random);
    }
    
    /// <summary>
    /// Safely sets a value in the dictionary by applying a transformation function to the
    /// existing value if the key exists, or to the specified default value if the key does not exist.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
    /// <param name="dict">The dictionary to perform the operation on.</param>
    /// <param name="key">The key for the value to be set or updated.</param>
    /// <param name="defaultValue">The default value to use if the key does not exist in the dictionary.</param>
    /// <param name="newValue">A function that takes the current value (or default value) and returns the new value to set.</param>
    /// <exception cref="ArgumentNullException">Thrown if the dictionary or key is null.</exception>
    public static void SetValueSafe<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue? defaultValue,
        Func<TValue, TValue> newValue)
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));

        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (dict.TryGetValue(key, out var value))
        {
            dict[key] = newValue(value);
        }
        else
        {
            dict[key] = newValue(defaultValue);
        }
    }
}