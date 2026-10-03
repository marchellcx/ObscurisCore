using AdminToys;

using InventorySystem.Items;

using LabExtended.API;
using LabExtended.API.Toys;
using LabExtended.API.Custom.Items;

using LabExtended.Core;
using LabExtended.Extensions;
using LabExtended.Utilities.Update;

using Mirror;
using ObscurisCore.Features.Elements.Alerts;

using PlayerRoles.FirstPersonControl;

using UnityEngine;
using ObscurisCore.Features.Dealer.Inventory.Items;
using ObscurisCore.Features.Dealer.Audio;
using ObscurisCore.Features.Dealer.Inventory;
using InventorySystem.Items.Coin;
using ObscurisCore.Features.Custom.Items;

namespace ObscurisCore.Features.Dealer;

/// <summary>
/// Represents a dealer instance that has spawned.
/// </summary>
public class DealerInstance
{
    /// <summary>
    /// Gets called when a player starts interacting with the dealer.
    /// </summary>
    public static event Action<DealerInstance, ExPlayer>? Started;

    /// <summary>
    /// Gets called when a player stops interacting with the dealer where the list is the list of purchased items.
    /// </summary>
    public static event Action<DealerInstance, ExPlayer, List<DealerItemInstance>>? Stopped;

    private PlayerUpdateComponent updateComponent;

    /// <summary>
    /// Gets the ID of the dealer instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Whether a player is currently interacting with the dealer.
    /// </summary>
    public bool IsActive => ActivePlayer?.ReferenceHub != null && ActiveInventory != null;

    /// <summary>
    /// Gets a value indicating whether the associated player object has been destroyed or is no longer valid.
    /// </summary>
    public bool IsDestroyed => Player?.ReferenceHub == null;

    /// <summary>
    /// Gets the spawned NPC player.
    /// </summary>
    public ExPlayer Player { get; }

    /// <summary>
    /// Gets or sets the player currently interacting with the dealer, if any.
    /// </summary>
    public ExPlayer ActivePlayer { get; set; }

    /// <summary>
    /// Gets the dealer's audio component.
    /// </summary>
    public DealerAudio Audio { get; private set; }

    /// <summary>
    /// Gets or sets the inventory of the currently active player, if any.
    /// </summary>
    public DealerInventory ActiveInventory { get; set; }

    /// <summary>
    /// Gets the light toy spawned under the dealer.
    /// </summary>
    public LightToy Light { get; private set; }

    /// <summary>
    /// Gets the interactable toy spawned in the dealer.
    /// </summary>
    public InteractableToy Interactable { get; private set; }

    /// <summary>
    /// Creates a new <see cref="DealerInstance"/> instance.
    /// </summary>
    /// <param name="player">The NPC player object.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DealerInstance(ExPlayer player, string id)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        Id = id;
        Player = player;

        Audio = new(this);
    }

    /// <summary>
    /// Initializes the dealer instance.
    /// </summary>
    public void Initialize()
    {
        Light = new(Player.Position, Player.Rotation)
        {
            Color = Color.magenta.FixPrimitiveColor(),
            Intensity = 0.25f,
            Range = 1.5f,
        };

        Interactable = new(Player.Position, Player.Rotation)
        {
            Shape = InvisibleInteractableToy.ColliderShape.Box,
            Scale = new(1f, Player.Role.MovementModule!.CharacterControllerSettings.Height / 2f, 1f),
            InteractionDuration = 0.5f,
            IsLocked = false
        };

        Audio.Initialize();

        updateComponent = PlayerUpdateComponent.Create();
        updateComponent.OnUpdate += OnUpdate;
    }

    /// <summary>
    /// Removes the current instance from the dealer manager and clears all associated inventories. If the instance
    /// is linked to a player, destroys the player's networked game object.
    /// </summary>
    public void DestroyInstance()
    {
        DealerManager.Dealers.Remove(this);

        Audio?.Destroy();
        Audio = null!;

        updateComponent?.Destroy();
        updateComponent = null!;

        if (ActivePlayer?.ReferenceHub != null)
        {
            if (ActiveInventory != null)
            {
                ActivePlayer.SendAlert(AlertType.Warn, 5f, "Dealer", $"Instance dealera se kterým právě obchoduješ byla odstraněna!", true);

                ActivePlayer.Inventory.UserInventory.Items.Clear();
                ActivePlayer.Inventory.UserInventory.ReserveAmmo.Clear();

                ActiveInventory.CachedItems.ForEach(x => ActivePlayer.Inventory.UserInventory.Items[x.ItemSerial] = x);
                ActivePlayer.Inventory.UserInventory.ReserveAmmo.AddRange(ActiveInventory.CachedAmmo);

                ActivePlayer.Inventory.Inventory.SendAmmoNextFrame = true;
                ActivePlayer.Inventory.Inventory.SendItemsNextFrame = true;

                ActiveInventory.ClearInventory();

                Stopped?.Invoke(this, ActivePlayer, ActiveInventory.PurchasedItems);
            }

            ActivePlayer = null!;
            ActiveInventory = null!;
        }

        if (Player?.ReferenceHub != null)
            NetworkServer.Destroy(Player.GameObject);
    }

    private void Stop()
    {
        try
        {
            var str =
                $"<b>Obchod s dealerem byl <color=red>ukončen</color>!</b>";

            if (ActiveInventory.PurchasedItems.Count > 0)
            {
                str += $"\n<b><color=red>Zakoupené předměty:</color></b>";

                foreach (var item in ActiveInventory.PurchasedItems)
                {
                    var itemName = item.Entry.Item;

                    if (ActiveInventory.ActiveMapping.TryGetKey(item, out var itemBase)
                        && CustomItem.IsCustomItem(itemBase.ItemSerial, out var customItem))
                        itemName = customItem.Name;

                    str += $"\n<b>- <color=yellow>{itemName}</color> za <color=green>{item.CurrentPrice} mincí</color></b>";
                }
            }

            if (ActivePlayer.Role.IsAlive)
            {
                ActivePlayer.SendAlert(AlertType.Warn, 10f, "Dealer", str, true);

                ActivePlayer.Inventory.UserInventory.Items.Clear();
                ActivePlayer.Inventory.UserInventory.ReserveAmmo.Clear();

                foreach (var item in ActiveInventory.ActiveMapping)
                    item.Key.DestroyItem();

                ActiveInventory.ActiveMapping.Clear();

                var coinsToAdd = ActivePlayer.IsBypassEnabled
                    ? ActiveInventory.CoinCount
                    : ActiveInventory.CoinCount - ActiveInventory.PurchasedItems.Sum(x => x.CurrentPrice);

                if (coinsToAdd > 0)
                    ActivePlayer.AddStackable(ItemType.Coin, coinsToAdd);

                ActiveInventory.CachedItems.ForEach(x => ActivePlayer.Inventory.UserInventory.Items[x.ItemSerial] = x);

                ActivePlayer.Inventory.UserInventory.ReserveAmmo.AddRange(ActiveInventory.CachedAmmo);
                ActivePlayer.Inventory.Inventory.SendAmmoNextFrame = true;

                Audio.OnTradeFinished(ActiveInventory.PurchasedItems.Count > 0);

                while (ActiveInventory.PurchasedItems.Count > 0)
                {
                    var purchasedItem = ActiveInventory.PurchasedItems.RemoveAndTake(0);

                    if (ActivePlayer.Inventory.ItemCount < 8)
                    {
                        if (Enum.TryParse<ItemType>(purchasedItem.Entry.Item, true, out var itemType))
                        {
                            ActivePlayer.Inventory.AddItem(itemType);
                        }
                        else if (CustomItem.TryGet(purchasedItem.Entry.Item, out var customItem))
                        {
                            customItem.AddItem(ActivePlayer);
                        }
                    }
                    else
                    {
                        if (Enum.TryParse<ItemType>(purchasedItem.Entry.Item, true, out var itemType))
                        {
                            ExMap.SpawnItem(itemType, ActivePlayer.Position, Vector3.one, ActivePlayer.Rotation);
                        }
                        else if (CustomItem.TryGet(purchasedItem.Entry.Item, out var customItem))
                        {
                            customItem.SpawnItem(ActivePlayer.Position, ActivePlayer.Rotation);
                        }
                    }
                }
            }

            Stopped?.InvokeSafe(this, ActivePlayer, ActiveInventory.PurchasedItems);

            ActiveInventory.ClearInventory();
        }
        catch (Exception ex)
        {
            ApiLog.Error("Dealer Instance", ex);
        }

        ActivePlayer = null!;
        ActiveInventory = null!;
    }

    private void OnUpdate()
    {
        if (IsActive)
        {
            if (!ActivePlayer.Role.IsAlive || ActivePlayer.Position.DistanceTo(Player) >= DealerManager.MaxDistance)
            {
                Stop();
            }
            else
            {
                Player.Role.MouseLook.LookAtDirection(ActivePlayer.Position.Position - Player.Position.Position);
            }
        }
        else
        {
            ExPlayer? closestPlayer = null;
            float closestDistance = 0f;

            ExPlayer.Players.ForEach(ply =>
            {
                if (ply?.ReferenceHub == null || !ply.Role.IsAlive)
                    return;

                var distance = Vector3.Distance(ply.Position, Player.Position);

                if (closestDistance == 0f || distance < closestDistance)
                {
                    closestPlayer = ply;
                    closestDistance = distance;
                }
            });

            if (closestPlayer?.ReferenceHub != null)
            {
                Player.Role.MouseLook.LookAtDirection(closestPlayer.Position.Position - Player.Position.Position);

                Audio.OnClosestPlayerDetected(closestPlayer, closestDistance);
            }
        }
    }

    internal void OnSelected(ItemBase? item)
    {
        if (item == null)
            return;

        if (ActiveInventory.ActiveMapping.TryGetValue(item, out var instance))
        {
            var itemName = instance.Entry.Item;

            if (CustomItem.IsCustomItem(item.ItemSerial, out var customItem))
                itemName = customItem.Name;

            var str = $"<b>Vybral si předmět <color=yellow>{itemName}</color> za <color=red>{instance.CurrentPrice}</color> mincí!";

            if (instance.DiscountPercentage > 0)
                str += $" <color=yellow>({instance.DiscountPercentage}% sleva!)</color>";

            str += "</b>\n" +
                "<b><color=red>Vyhoď předmět na zem pro zakoupení</color></b>";

            ActivePlayer.SendAlert(AlertType.Info, 10f, "Dealer", str, true);
        }
    }

    internal bool OnDropping(ItemBase? item, out bool destroyItem)
    {
        destroyItem = false;

        if (item == null)
            return true;

        if (ActiveInventory.ActiveMapping.TryGetValue(item, out var instance))
        {
            var availableCoins = ActiveInventory.CoinCount - ActiveInventory.PurchasedItems.Sum(x => x.CurrentPrice);

            if (!ActivePlayer.IsBypassEnabled && instance.CurrentPrice > availableCoins)
            {
                Audio.OnPurchasingItem(false);

                ActivePlayer.SendAlert(AlertType.Warn, 10f, "Dealer",
                    $"<b>Tento předmět si bohužel nemůžeš dovolit, chybí ti <color=red>{instance.CurrentPrice - availableCoins}</color> mincí!</b>", true);
                return false;
            }

            Audio.OnPurchasingItem(true);

            ActiveInventory.PurchasedItems.Add(instance);
            ActivePlayer.SendAlert(AlertType.Info, 10f, "Dealer",
                $"<b>Zakoupil si předmět <color=red>{instance.Entry.Item}</color> za <color=yellow>{instance.CurrentPrice}</color> mincí!</b>\n" +
                $"<b>Zbývá ti <color=green>{availableCoins - instance.CurrentPrice}</color> mincí.</b>", true);

            Audio.OnPurchasedItem();

            destroyItem = true;
            return false;
        }

        return true;
    }

    internal void OnDying()
    {
        ActivePlayer.Inventory.UserInventory.Items.Clear();
        ActivePlayer.Inventory.UserInventory.ReserveAmmo.Clear();

        foreach (var item in ActiveInventory.ActiveMapping)
            item.Key.DestroyItem();

        ActiveInventory.ActiveMapping.Clear();
        ActivePlayer.AddStackable(ItemType.Coin, ActiveInventory.CoinCount);

        ActiveInventory.CachedItems.ForEach(x => ActivePlayer.Inventory.UserInventory.Items[x.ItemSerial] = x);

        ActivePlayer.Inventory.UserInventory.ReserveAmmo.AddRange(ActiveInventory.CachedAmmo);
        ActivePlayer.Inventory.Inventory.SendAmmoNextFrame = true;

        Audio.OnPlayerDied();

        ActivePlayer = null!;

        ActiveInventory.ClearInventory();
        ActiveInventory = null!;
    }

    internal void OnInteracted(ExPlayer player)
    {
        if (ActivePlayer?.ReferenceHub != null)
        {
            if (ActivePlayer == player)
            {
                Stop();
                return;
            }

            player.SendAlert(AlertType.Warn, 5f, "Dealer", $"<b>Dealer je momentálně <color=red>obsazený</color>!</b>", true);
            return;
        }

        var inventory = DealerManager.GetDealerInventory(Id, player.UserId, false);

        if (inventory.Items.Count == 0)
        {
            Audio.OnTradeFailedEmptyInventory();

            player.SendAlert(AlertType.Warn, 5f, "Dealer", $"<b>Dealer <color=red>nemá žádné předměty</color> na prodej!</b>", true);
            return;
        }

        ActivePlayer = player;
        ActiveInventory = inventory;

        inventory.ClearInventory();
        inventory.CoinCount = player.GetTotalItemCount(ItemType.Coin);

        inventory.CachedAmmo.AddRange(player.Inventory.UserInventory.ReserveAmmo.Where(x => x.Key.IsAmmo()));
        inventory.CachedItems.AddRange(player.Inventory.UserInventory.Items.Values.Where(x => x is not InventorySystem.Items.Coin.Coin));

        player.Inventory.UserInventory.Items.Clear();
        player.Inventory.UserInventory.ReserveAmmo.Clear();

        player.ClearStackedItems(ItemType.Coin);

        foreach (var item in inventory.Items)
        {
            ItemBase? addedItem = null;

            if (Enum.TryParse<ItemType>(item.Entry.Item, true, out var itemType))
            {
                addedItem = ActivePlayer.Inventory.AddItem(itemType);
            }
            else if (CustomItem.TryGet(item.Entry.Item, out var customItem))
            {
                addedItem = customItem.AddItem(ActivePlayer);
            }

            if (addedItem != null)
                inventory.ActiveMapping[addedItem] = item;
        }

        player.SendAlert(AlertType.Info, 10f, "Dealer",
            $"<b>Začal jsi obchodovat s dealerem! Máš k dispozici <color=yellow>{inventory.CoinCount} mincí</color>!</b>\n" +
            "<b><color=red>Vyhoď předmět na zem pro jeho zakoupení</color></b>\n" +
            "<b><color=yellow>Vyber předmět v inventáři pro zobrazení ceny!</color></b>", true);

        Audio.OnTradeStarted();

        Started?.InvokeSafe(this, player);
    }
}