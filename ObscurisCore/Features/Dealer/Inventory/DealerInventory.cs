using InventorySystem.Items;

using LabExtended.Extensions;

using ObscurisCore.Features.Dealer.Inventory.Items;

using Utils.NonAllocLINQ;

namespace ObscurisCore.Features.Dealer.Inventory;

/// <summary>
/// Represents the inventory of a spawned dealer.
/// </summary>
public class DealerInventory
{
    /// <summary>
    /// Gets or sets the number of the round the inventory was generated in.
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// A list of actual item instances the dealer is selling.
    /// </summary>
    public List<DealerItemInstance> Items { get; } = new();

    /// <summary>
    /// A list of all purchased items by the player.
    /// </summary>
    public List<DealerItemInstance> PurchasedItems { get; } = new();

    /// <summary>
    /// A list of all active item mappings for quick lookup.
    /// </summary>
    public Dictionary<ItemBase, DealerItemInstance> ActiveMapping { get; } = new();

    /// <summary>
    /// Gets or sets the amount of coins in the player's inventory.
    /// </summary>
    public int CoinCount { get; set; } = 0;

    /// <summary>
    /// Cached items from the player's inventory.
    /// </summary>
    public List<ItemBase> CachedItems { get; } = new();

    /// <summary>
    /// Cached ammo from the player's inventory.
    /// </summary>
    public Dictionary<ItemType, ushort> CachedAmmo { get; } = new();

    /// <summary>
    /// Resets the inventory to its initial state, removing all items, coins, and cached data.
    /// </summary>
    /// <remarks>Call this method to clear all inventory contents and related caches. After calling,
    /// the inventory will be empty and all coin counts and purchased items will be reset. This operation cannot be
    /// undone.</remarks>
    public void ResetInventory()
    {
        ActiveMapping.ForEachKey(x => x.DestroyItem());
        ActiveMapping.Clear();

        Items.Clear();

        CachedAmmo.Clear();
        CachedItems.Clear();

        CoinCount = 0;

        PurchasedItems.Clear();
    }

    /// <summary>
    /// Removes all items, ammunition, and coins from the inventory, resetting it to an empty state.
    /// </summary>
    /// <remarks>Use this method to completely clear the inventory, including all cached and purchased
    /// items. After calling this method, the inventory will contain no items or ammunition, and the coin count will
    /// be set to zero.</remarks>
    public void ClearInventory()
    {
        ActiveMapping.ForEachKey(x => x.DestroyItem());
        ActiveMapping.Clear();

        CachedAmmo.Clear();
        CachedItems.Clear();

        PurchasedItems.Clear();

        CoinCount = 0;
    }
}