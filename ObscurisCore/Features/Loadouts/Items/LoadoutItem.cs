using System.ComponentModel;

namespace ObscurisCore.Features.Loadouts.Items;

/// <summary>
/// An item in a config loadout.
/// </summary>
public class LoadoutItem
{
    /// <summary>
    /// Gets or sets the base type of the item (null if the item is a custom item).
    /// </summary>
    [Description("Sets the type of the vanilla item.")]
    public ItemType? BaseType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the custom item (null if the item is a vanilla item).
    /// </summary>
    [Description("Sets the ID of the custom item.")]
    public string? CustomType { get; set; }
}