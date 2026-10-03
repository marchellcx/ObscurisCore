using System.ComponentModel;

namespace ObscurisCore.Features.Loadouts.Items;

/// <summary>
/// An ammo definition in a loadout config.
/// </summary>
public class LoadoutAmmo
{
    /// <summary>
    /// Gets or sets the amount of the ammo.
    /// </summary>
    [Description("Sets the amount of the ammo.")]
    public ushort Amount { get; set; }

    /// <summary>
    /// Gets or sets the item type of the ammo (null if this is custom ammo).
    /// </summary>
    [Description("Sets the type of the vanilla ammo.")]
    public ItemType? BaseType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the custom ammo (null if this is vanilla ammo).
    /// </summary>
    [Description("Sets the ID of the custom ammo.")]
    public string? CustomType { get; set; }
}