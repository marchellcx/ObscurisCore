using System.ComponentModel;

namespace ObscurisCore.Utilities.Configs;

/// <summary>
/// Represents a range of 32-bit integer values with configurable minimum and maximum bounds.
/// </summary>
public class Int32Range
{
    /// <summary>
    /// Gets or sets the minimum value of the range.
    /// </summary>
    [Description("Sets the minimum value.")]
    public int MinValue { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum value of the range.
    /// </summary>
    [Description("Sets the maximum value.")]
    public int MaxValue { get; set; } = 0;

    /// <summary>
    /// Gets a random value between <see cref="MinValue"/> and <see cref="MaxValue"/>.
    /// </summary>
    /// <returns>The random value.</returns>
    public int GetRandom()
    {
        if (MinValue == MaxValue)
            return MinValue;

        if (MaxValue < MinValue)
            (MinValue, MaxValue) = (MaxValue, MinValue);

        return UnityEngine.Random.Range(MinValue, MaxValue);
    }

    /// <summary>
    /// Gets a random value between <see cref="MinValue"/> and <see cref="MaxValue"/> of a range for a specific key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="randomDict">The target dictionary.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <returns>The generated random range value.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public static int GetRandom<TKey>(TKey key, IDictionary<TKey, Int32Range> randomDict)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (randomDict is null)
            throw new ArgumentNullException(nameof(randomDict));

        if (!randomDict.TryGetValue(key, out var range))
            throw new KeyNotFoundException($"Could not find key '{key}' in dictionary");

        return range.GetRandom();
    }
}