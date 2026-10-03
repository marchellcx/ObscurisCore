using ObscurisCore.Utilities.Configs;

using System.ComponentModel;

namespace ObscurisCore.Features.Dealer.Inventory.Items;

/// <summary>
/// Represents an entry for a dealer's inventory item.
/// </summary>
public class DealerItemEntry
{
    /// <summary>
    /// Gets or sets the rarity of the item, indicating the likelihood that it will be selected for a dealer's
    /// inventory.
    /// </summary>
    [Description("Sets the rarity of the item (aka the chance of it being selected to a dealer's inventory).")]
    public byte Rarity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the price of the item in coins.
    /// </summary>
    [Description("Sets the price of the item (in coins).")]
    public int Price { get; set; } = 0;

    /// <summary>
    /// Gets or sets the probability, as a percentage, that a discount will be applied to the item.
    /// </summary>
    [Description("Sets the probability (in percent) that a discount will be applied to the item.")]
    public int DiscountChance { get; set; } = 0;

    /// <summary>
    /// Gets or sets the range of possible discount percentages that can be applied to the item.
    /// </summary>
    [Description("Sets the range of possible discounts (in percent) that can be applied to the item.")]
    public Int32Range DiscountRange { get; set; } = new()
    {
        MinValue = 0,
        MaxValue = 0
    };

    /// <summary>
    /// Gets or sets the type of the base item or the name of a custom item.
    /// </summary>
    [Description("Sets the type of the base item (or a name of a custom item).")]
    public string Item { get; set; } = string.Empty;
}