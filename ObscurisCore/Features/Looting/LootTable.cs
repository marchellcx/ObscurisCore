using System.Collections.ObjectModel;
using ObscurisCore.Features.Looting.Configs;

namespace ObscurisCore.Features.Looting;

/// <summary>
/// Represents a loot table.
/// </summary>
public struct LootTable
{
    /// <summary>
    /// The weight of the table.
    /// </summary>
    public readonly float Weight;
    
    /// <summary>
    /// The items in the table.
    /// </summary>
    public readonly ReadOnlyCollection<LootItem> Items;
    
    /// <summary>
    /// Creates a new instance of the LootTable struct.
    /// </summary>
    public LootTable(float weight, ReadOnlyCollection<LootItem> items)
    {
        Weight = weight;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}