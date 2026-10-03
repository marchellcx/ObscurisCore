using LabExtended.Extensions;

namespace ObscurisCore.Extensions;

/// <summary>
/// Provides extension methods for working with enumeration types.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retrieves the highest value of an enumeration based on the underlying numeric type.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="type">The enumeration type to process.</param>
    /// <returns>The highest value from the enumeration as a specified type.</returns>
    public static T GetHighestValue<T>(Type type)
    {
        var numeric = Enum.GetUnderlyingType(type);
        var values = Enum.GetValues(type);
        var cast = values.CastArray<Enum>();

        return (T)(object)cast
            .OrderBy(en => (long)Convert.ChangeType(en, numeric))
            .First();
    }
}