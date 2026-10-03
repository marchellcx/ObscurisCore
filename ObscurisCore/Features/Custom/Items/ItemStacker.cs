using InventorySystem.Items;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.Events;
using LabExtended.Extensions;

using NiveraAPI.IO.Configs;
using Utils.NonAllocLINQ;

namespace ObscurisCore.Features.Custom.Items;

/// <summary>
/// Manages item stacking.
/// </summary>
public static class ItemStacker
{
    /// <summary>
    /// Gets the mapping of item types to their maximum stack sizes.
    /// </summary>
    /// <remarks>The returned dictionary provides the maximum number of items that can be stacked
    /// together for each item type. The dictionary is read-only and reflects the current configuration. Modifying
    /// the returned dictionary may result in undefined behavior.</remarks>
    [Config("stacking", "items", "Items that can be stacked.")]
    public static Dictionary<ItemType, ushort> Config { get; set; } = new()
    {
        { ItemType.Coin, 100 }
    };

    /// <summary>
    /// Gets a dictionary that maps each player to their associated item stack.
    /// </summary>
    /// <remarks>The dictionary provides access to the current item stack for each player. The
    /// collection is read-only; to modify the mapping, use the appropriate methods provided by the class.</remarks>
    public static Dictionary<ExPlayer, Dictionary<ItemType, List<ItemBase>>> Stacks { get; } = new();

    /// <summary>
    /// Gets the total number of items of the specified type that the player currently possesses, including both
    /// inventory and stacked items.
    /// </summary>
    /// <remarks>This method sums the count of the specified item type from both the player's
    /// inventory and any additional stacked items. Use this method to determine the player's complete holdings of a
    /// particular item type.</remarks>
    /// <param name="player">The player whose items are to be counted. Cannot be null.</param>
    /// <param name="itemType">The type of item to count in the player's possession.</param>
    /// <returns>The total count of items of the specified type held by the player. Returns 0 if the player has none of the
    /// specified item.</returns>
    public static int GetTotalItemCount(this ExPlayer player, ItemType itemType)
    {
        var count = player.Inventory.CountItems(itemType);

        if (Stacks.TryGetValue(player, out var stackInfo)
            && stackInfo.TryGetValue(itemType, out var stack))
            count += stack.Count;

        return count;
    }

    /// <summary>
    /// Gets the number of items of the specified type currently stacked by the player.
    /// </summary>
    /// <param name="player">The player whose stacked items are to be counted. Cannot be null.</param>
    /// <param name="itemType">The type of item to count within the player's stack.</param>
    /// <returns>The number of stacked items of the specified type for the player. Returns 0 if the player has no stacked
    /// items of that type.</returns>
    public static int GetStackedItemCount(this ExPlayer player, ItemType itemType)
    {
        if (!Stacks.TryGetValue(player, out var stackInfo))
            return 0;

        if (!stackInfo.TryGetValue(itemType, out var stack))
            return 0;

        return stack.Count;
    }

    /// <summary>
    /// Removes a specified number of stacked items of the given type from the player's inventory, if present.
    /// </summary>
    /// <remarks>If the player does not have any stacked items of the specified type, or if the amount
    /// is greater than the number of available items, only the available items are removed. No exception is thrown
    /// if fewer items are present than requested.</remarks>
    /// <param name="player">The player whose stacked items are to be removed.</param>
    /// <param name="itemType">The type of item to remove from the player's stacked items.</param>
    /// <param name="amount">The maximum number of items to remove. Must be greater than zero.</param>
    public static void RemoveStackedItems(this ExPlayer player, ItemType itemType, int amount)
    {
        if (!Stacks.TryGetValue(player, out var stackInfo))
            return;

        if (!stackInfo.TryGetValue(itemType, out var stack))
            return;

        while (amount > 0 && stack.Count > 0)
        {
            stack.RemoveAt(0);
            amount--;
        }

        player.ResyncStacks();
    }

    /// <summary>
    /// Removes all stacked items of the specified type from the player's inventory.
    /// </summary>
    /// <remarks>If the player does not have any stacked items of the specified type, the method
    /// performs no action. The method does not throw an exception if the player or item type is not
    /// found.</remarks>
    /// <param name="player">The player whose stacked items are to be cleared.</param>
    /// <param name="type">The type of item to clear from the player's stacked items.</param>
    /// <param name="cache">An optional list to which the cleared items will be added before removal. If null, the items are not cached.</param>
    public static void ClearStackedItems(this ExPlayer player, ItemType type, List<ItemBase>? cache = null)
    {
        if (!Stacks.TryGetValue(player, out var stackInfo))
            return;

        if (!stackInfo.TryGetValue(type, out var stack))
            return;

        if (cache != null)
            cache.AddRange(stack);
        else
            stack.ForEach(x => x.DestroyItem());

        stack.Clear();

        player.ResyncStacks();
    }

    /// <summary>
    /// Adds the specified amount of a stackable item to the player's inventory, respecting the maximum stack size
    /// for that item type.
    /// </summary>
    /// <remarks>If the item type is not configured as stackable or the maximum stack size is less
    /// than one, no items are added. The method will not exceed the configured maximum stack size for the item. If
    /// the player does not already have the item, one instance is added to the inventory before stacking additional
    /// items. Any remaining amount that cannot be stacked due to the maximum size is ignored.</remarks>
    /// <param name="player">The player to whom the stackable item will be added.</param>
    /// <param name="item">The type of item to add as a stackable entry.</param>
    /// <param name="amount">The number of items to attempt to add to the player's stack. Must be greater than zero.</param>
    public static void AddStackable(this ExPlayer player, ItemType item, int amount)
    {
        if (!Config.TryGetValue(item, out var maxSize) || maxSize < 1)
            return;

        if (!Stacks.TryGetValue(player, out var stackInfo))
            Stacks[player] = stackInfo = new();

        if (!stackInfo.TryGetValue(item, out var stack))
            stackInfo[item] = stack = new();

        if (player.Inventory.CountItems(item) < 1)
        {
            player.Inventory.AddItem(item);
            amount--;
        }

        while (amount > 0)
        {
            var instance = item.GetItemInstance<ItemBase>();

            if (instance != null)
            {
                instance.Owner = player.ReferenceHub;
                instance.ServerAddReason = ItemAddReason.AdminCommand;
                instance.OnAdded(null);

                stack.Add(instance);
            }

            amount--;

            if (stack.Count >= maxSize)
                break;
        }

        player.ResyncStacks();
    }

    /// <summary>
    /// Synchronizes the player's ammo counts with the current state of their stacked items.
    /// </summary>
    /// <remarks>Call this method after modifying the player's stacked items to ensure that their ammo
    /// counts accurately reflect the current stack state.</remarks>
    /// <param name="player">The player whose ammo stacks are to be resynchronized. Cannot be null.</param>
    public static void ResyncStacks(this ExPlayer player)
    {
        if (!Stacks.TryGetValue(player, out var stackInfo))
            return;

        foreach (var pair in stackInfo)
            player.Ammo.SetAmmo(pair.Key, (ushort)pair.Value.Count);
    }

    private static void Internal_PickedUp(PlayerPickedUpItemEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (player.Inventory.CountItems(args.Item.Type) > 1)
        {
            if (!Config.TryGetValue(args.Item.Type, out var maxSize) || maxSize < 1)
                return;

            if (!Stacks.TryGetValue(player, out var stackInfo))
                Stacks[player] = stackInfo = new();

            if (stackInfo.Count >= maxSize)
                return;

            if (!stackInfo.TryGetValue(args.Item.Type, out var stack))
                stackInfo[args.Item.Type] = stack = new();

            stack.Add(args.Item.Base);

            player.Ammo.SetAmmo(args.Item.Type, (ushort)stack.Count);

            player.ReferenceHub.inventory.UserInventory.Items.Remove(args.Item.Serial);
            player.ReferenceHub.inventory.SendItemsNextFrame = true;
        }
    }

    private static void Internal_DroppedAmmo(PlayerDroppingAmmoEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (args.Type.IsAmmo())
            return;

        if (!Config.TryGetValue(args.Type, out var maxSize) || maxSize < 1)
            return;

        if (!Stacks.TryGetValue(player, out var stackInfo))
            return;

        if (!stackInfo.TryGetValue(args.Type, out var stack) || stack.Count < 1)
            return;

        args.IsAllowed = false;

        var amount = args.Amount;

        while (amount > 0 && stack.Count > 0)
        {
            var item = stack.RemoveAndTake(0);

            item.ServerDropItem(true);

            amount--;
        }

        player.ResyncStacks();
    }

    private static void Internal_DroppedItem(PlayerDroppedItemEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (args.Pickup?.Base == null)
            return;

        if (!Config.TryGetValue(args.Pickup.Type, out var maxSize) || maxSize < 1)
            return;

        if (!Stacks.TryGetValue(player, out var stackInfo))
            return;

        if (!stackInfo.TryGetValue(args.Pickup.Type, out var stack) || stack.Count < 1)
            return;

        var item = stack.RemoveAndTake(0);

        item.TransferItem(player.ReferenceHub);

        if (stack.Count < 1)
        {
            stackInfo.Remove(args.Pickup.Type);

            player.Ammo.Ammo.Remove(args.Pickup.Type);
            player.ReferenceHub.inventory.SendAmmoNextFrame = true;
        }
        else
        {
            player.Ammo.SetAmmo(args.Pickup.Type, (ushort)stack.Count);
        }

        player.ReferenceHub.inventory.ServerSendItems();
        player.ReferenceHub.inventory.SendItemsNextFrame = false;

        if (player.Inventory.CurrentItemIdentifier.SerialNumber == args.Pickup.Serial)
            player.Inventory.CurrentItemIdentifier = item.ItemId;
    }

    private static void Internal_Left(ExPlayer player)
    {
        if (Stacks.TryGetValue(player, out var stackInfo))
        {
            stackInfo.ForEachValue(items =>
            {
                items.ForEach(item => { item.DestroyItem(); });

                items.Clear();
            });

            stackInfo.Clear();
        }

        Stacks.Remove(player);
    }

    private static void Initialize()
    {
        PlayerEvents.PickedUpItem += Internal_PickedUp;

        PlayerEvents.DroppedItem += Internal_DroppedItem;
        PlayerEvents.DroppingAmmo += Internal_DroppedAmmo;

        ExPlayerEvents.Left += Internal_Left;
    }
}