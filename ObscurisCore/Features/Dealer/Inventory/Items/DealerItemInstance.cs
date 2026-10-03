namespace ObscurisCore.Features.Dealer.Inventory.Items;

/// <summary>
/// Represents an item offered by a dealer, including its pricing and discount information.
/// </summary>
public struct DealerItemInstance
{
    /// <summary>
    /// Gets the entry details of the dealer item.
    /// </summary>
    public readonly DealerItemEntry Entry;

    /// <summary>
    /// Represents the original price before any discounts or adjustments are applied.
    /// </summary>
    public readonly int OriginalPrice;

    /// <summary>
    /// Represents the current price value.
    /// </summary>
    public readonly int CurrentPrice;

    /// <summary>
    /// Represents the discount percentage to be applied.
    /// </summary>
    public readonly int DiscountPercentage;

    /// <summary>
    /// Initializes a new instance of the DealerItemInstance class with the specified item, original price, current
    /// price, and discount percentage.
    /// </summary>
    /// <param name="originalPrice">The original price of the item before any discounts are applied, in the relevant currency units. Must be
    /// non-negative.</param>
    /// <param name="currentPrice">The current price of the item after discounts are applied, in the relevant currency units. Must be
    /// non-negative.</param>
    /// <param name="discountPercentage">The percentage discount applied to the original price. Must be between 0 and 100.</param>
    public DealerItemInstance(DealerItemEntry entry, int originalPrice, int currentPrice, int discountPercentage)
    {
        Entry = entry;
        OriginalPrice = originalPrice;
        CurrentPrice = currentPrice;
        DiscountPercentage = discountPercentage;
    }
}