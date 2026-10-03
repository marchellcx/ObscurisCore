using InventorySystem.Items;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp939Events;

using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.Core;

using LabExtended.Events;
using LabExtended.Events.Player;

using LabExtended.Extensions;

using LabExtended.Utilities;
using LabExtended.Utilities.Update;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Dealer.Audio;
using ObscurisCore.Features.Dealer.Inventory;
using ObscurisCore.Features.Dealer.Inventory.Items;

using ObscurisCore.Utilities.Configs;

using PlayerRoles;
using PlayerRoles.Spectating;

using UnityEngine;

namespace ObscurisCore.Features.Dealer;

/// <summary>
/// Manages the spawning logic of dealers.
/// </summary>
public static class DealerManager
{
    /// <summary>
    /// Gets or sets the maximum number of rounds that a dealer's inventory can remain active before it is
    /// refreshed.
    /// </summary>
    [Config("dealer", "max-inventory-age", "Sets the maximum number of rounds a dealer's inventory can remain active before it is refreshed.")]
    public static int MaxInventoryAge { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum amount of dealers that can spawn per round.
    /// </summary>
    [Config("dealer", "max-per-round", "Sets the maximum amount of dealers that can spawn per round.")]
    public static int MaxPerRound { get; set; } = 2;

    /// <summary>
    /// Gets or sets the minimum amount of dealers that have to spawn per round.
    /// </summary>
    [Config("dealer", "min-per-round", "Sets the minimum amount of dealers that have to spawn per round.")]
    public static int MinPerRound { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of seconds to wait before attempting to spawn a dealer after the round starts.
    /// </summary>
    [Config("dealer", "wait-time", "Sets how long (in seconds) the plugin should wait before attempting to spawn a dealer after the round starts.")]
    public static float WaitTime { get; set; } = 100f;

    /// <summary>
    /// Gets or sets the delay, in seconds, between dealer spawn attempts.
    /// </summary>
    [Config("dealer", "spawn-delay", "Sets how long (in seconds) the plugin should wait between dealer spawn attempts.")]
    public static float SpawnDelay { get; set; } = 300f;

    /// <summary>
    /// Gets or sets the maximum distance, in meters, that a player can be from a dealer before their trade is
    /// terminated.
    /// </summary>
    [Config("dealer", "max-distance", "Sets the maximum distance (in meters) a player can be from a dealer before their trade terminates.")]
    public static float MaxDistance { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the maximum distance, in meters, at which a player can be from a dealer  for the player close
    /// audio clip to play.
    /// </summary>
    [Config("dealer", "max-audio-distance", "Sets the maximum distance (in meters) a player can be from a dealer for the player close audio clip to play.")]
    public static float MaxAudioDistance { get; set; } = 5f;

    /// <summary>
    /// Gets or sets the minimum cooldown period, in seconds, required between playbacks of each audio clip type.
    /// </summary>
    [Config("dealer", "clip-cooldown", "Sets the minimum required amount of seconds between playbacks of audio clips.")]
    public static Dictionary<DealerClip, float> ClipCooldown { get; set; } = new()
    {
        [DealerClip.PurchaseFailed] = 5f,
        [DealerClip.PurchaseSuccessful] = 5f,

        [DealerClip.TradeStart] = 0f,

        [DealerClip.TradeEndedNoPurchase] = 0f,
        [DealerClip.TradeEndedWithPurchase] = 0f
    };

    /// <summary>
    /// Gets or sets the collection of audio clip file names associated with each clip type.
    /// </summary>
    [Config("dealer", "clip-names", "Sets clip file names for each audio clip.")]
    public static Dictionary<DealerClip, List<string>> ClipNames { get; set; } = new()
    {
        [DealerClip.PurchaseFailed] = new(),
        [DealerClip.PurchaseSuccessful] = new(),

        [DealerClip.TradeStart] = new(),

        [DealerClip.TradeEndedNoPurchase] = new(),
        [DealerClip.TradeEndedWithPurchase] = new()
    };

    /// <summary>
    /// Gets or sets the list of available spawn position names.
    /// </summary>
    [Config("dealer", "spawn-positions", "Sets the list of available spawn position names.")]
    public static List<string> SpawnPositions { get; set; } = new();

    /// <summary>
    /// Gets or sets the range of items that a dealer can have in their inventory.
    /// </summary>
    [Config("dealer", "inventory-size", "Sets the range for how many items a dealer can have in their inventory.")]
    public static Int32Range InventorySize { get; set; } = new()
    {
        MinValue = 1,
        MaxValue = 3
    };

    /// <summary>
    /// Gets or sets the collection of items available for dealers to sell.
    /// </summary>
    [Config("dealer", "inventory-items", "Sets the list of items that dealers can sell.")]
    public static List<DealerItemEntry> InventoryItems { get; set; } = new()
    {
            new()
            {
                Item = "Medkit",
                Price = 2,
                Rarity = 4,
            },

            new()
            {
                Item = "Painkillers",
                Price = 1,
                Rarity = 5
            },

            new()
            {
                Item = "Flashlight",
                Price = 1,
                Rarity = 5,
            },

            new()
            {
                Item = "Adrenaline",
                Price = 4,
                Rarity = 3,
            },

            new()
            {
                Item = "SCP500",
                Price = 4,
                Rarity = 3,
            },

            new()
            {
                Item = "GunCom45",
                Price = 6,
                Rarity = 2,
            },

            new()
            {
                Item = "GunCOM15",
                Price = 6,
                Rarity = 2,
            },

            new()
            {
                Item = "GunCOM18",
                Price = 6,
                Rarity = 2,
            },

            new()
            {
                Item = "SniperRifle",
                Price = 15,
                Rarity = 1
            }
    };

    private static float remainingStartWait = 0f;
    private static float remainingSpawnWait = 0f;

    /// <summary>
    /// Gets or sets the amount of dealers that should spawn this round.
    /// </summary>
    public static int SpawnThisRound { get; set; } = 0;

    /// <summary>
    /// Gets a randomly generated amount of dealers to spawn this round.
    /// </summary>
    public static int RandomSpawnCount => UnityEngine.Random.Range(MinPerRound, MaxPerRound);

    /// <summary>
    /// Gets a randomly selected spawn position name from the config.
    /// </summary>
    public static string RandomSpawnName
    {
        get
        {
            var random = SpawnPositions.RandomItem();

            while (Dealers.Any(x => !x.IsDestroyed && x.Id == random))
                random = SpawnPositions.RandomItem();

            return random;
        }
    }

    /// <summary>
    /// A list of dealers that have spawned this round.
    /// </summary>
    public static List<DealerInstance> Dealers { get; } = new();

    /// <summary>
    /// A list of player unique inventories.
    /// </summary>
    public static Dictionary<string, Dictionary<string, DealerInventory>> Inventories { get; } = new();

    /// <summary>
    /// Gets called when a new dealer instance is spawned.
    /// </summary>
    public static event Action<DealerInstance>? Spawned;

    /// <summary>
    /// Spawns a new dealer NPC at the specified position and rotation.
    /// </summary>
    /// <remarks>The spawned dealer is initialized in god mode and is added to the global dealer list.
    /// The Spawned event is invoked after the dealer is created.</remarks>
    /// <param name="position">The world position where the dealer NPC will be spawned.</param>
    /// <param name="rotation">The orientation to assign to the dealer NPC upon spawning.</param>
    /// <returns>A DealerInstance representing the newly spawned dealer NPC.</returns>
    public static DealerInstance SpawnDealer(Vector3 position, Quaternion rotation, string id)
    {
        var npc = new ExPlayer("Dealer", true);
        var dealer = new DealerInstance(npc, id);

        npc.Role.Set(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);

        TimingUtils.AfterSeconds(() =>
        {
            npc.Position.Set(position);
            npc.Rotation.Set(rotation);

            npc.IsGodModeEnabled = true;

            npc.Toggles.IsVisibleInRemoteAdmin = false;

            SpectatableVisibilityManager.SetHidden(npc.ReferenceHub, true);

            Dealers.Add(dealer);

            dealer.Initialize();

            Spawned?.InvokeSafe(dealer);
        }, 0.2f);

        return dealer;
    }

    /// <summary>
    /// Retrieves the inventory for a specified dealer and user, optionally forcing a refresh of the inventory data.
    /// </summary>
    /// <remarks>If the inventory is older than the maximum allowed age or if forceRefresh is set to
    /// true, the inventory data is refreshed before being returned. Otherwise, cached inventory data is
    /// used.</remarks>
    /// <param name="dealerId">The unique identifier of the dealer whose inventory is to be retrieved. Cannot be null.</param>
    /// <param name="userId">The unique identifier of the user for whom the inventory is requested. Cannot be null.</param>
    /// <param name="forceRefresh">true to force a refresh of the inventory data; otherwise, false to use cached data if available.</param>
    /// <returns>A DealerInventory object representing the current inventory for the specified dealer and user.</returns>
    public static DealerInventory GetDealerInventory(string dealerId, string userId, bool forceRefresh = false)
    {
        if (!Inventories.TryGetValue(dealerId, out var inventories))
            Inventories[dealerId] = inventories = new();

        if (inventories.TryGetValue(userId, out var inventory))
        {
            if (forceRefresh || (ExRound.RoundNumber - inventory.RoundNumber) >= MaxInventoryAge)
            {
                inventory.ResetInventory();
                inventory.RoundNumber = ExRound.RoundNumber;

                RefreshInventory(inventory);
            }

            return inventory;
        }
        else
        {
            inventories[userId] = inventory = new();

            inventory.ResetInventory();
            inventory.RoundNumber = ExRound.RoundNumber;

            RefreshInventory(inventory);
            return inventory;
        }
    }

    /// <summary>
    /// Determines whether the specified player is currently engaged in a trading session.
    /// </summary>
    /// <remarks>A player is considered to be trading if they are actively engaged with a dealer that
    /// is not destroyed  and is marked as active. If the player is trading, the associated dealer is returned via
    /// the  <paramref name="targetDealer"/> parameter.</remarks>
    /// <param name="player">The player to check for an active trading session.</param>
    /// <param name="targetDealer">When this method returns, contains the <see cref="DealerInstance"/> associated with the trading session,  if
    /// the player is trading; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the player is currently trading with a dealer; otherwise, <see langword="false"/>.</returns>
    public static bool IsTrading(this ExPlayer player, out DealerInstance targetDealer)
    {
        targetDealer = null!;

        if (player?.ReferenceHub == null)
            return false;

        for (var i = 0; i < Dealers.Count; i++)
        {
            var dealer = Dealers[i];

            if (dealer.IsDestroyed || !dealer.IsActive)
                continue;

            if (dealer.ActivePlayer != player)
                continue;

            targetDealer = dealer;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified item is currently present in any dealer's active inventory.
    /// </summary>
    /// <remarks>If the item is not found in any dealer's active inventory or if the item is null, the
    /// method returns false and sets owningDealer to null.</remarks>
    /// <param name="item">The item to check for ownership by a dealer. Cannot be null.</param>
    /// <param name="owningDealer">When this method returns, contains the dealer that owns the item if found; otherwise, null.</param>
    /// <returns>true if the item is found in a dealer's active inventory; otherwise, false.</returns>
    public static bool IsDealerItem(this ItemBase item, out DealerInstance owningDealer)
    {
        owningDealer = null!;

        if (item == null)
            return false;

        for (var i = 0; i < Dealers.Count; i++)
        {
            var dealer = Dealers[i];

            if (dealer.IsDestroyed || !dealer.IsActive)
                continue;

            if (dealer.ActiveInventory.ActiveMapping.ContainsKey(item))
            {
                owningDealer = dealer;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified item is currently present in any dealer's active inventory.
    /// </summary>
    /// <remarks>If the item is not found in any dealer's active inventory, both out parameters are
    /// set to their default values. This method does not consider destroyed dealers or dealers without an active
    /// inventory.</remarks>
    /// <param name="item">The item to check for association with a dealer's active inventory. Cannot be null.</param>
    /// <param name="owningDealer">When this method returns, contains the dealer instance that owns the item if found; otherwise, null.</param>
    /// <param name="dealerItem">When this method returns, contains the dealer item instance associated with the specified item if found;
    /// otherwise, the default value.</param>
    /// <returns>true if the item is found in a dealer's active inventory; otherwise, false.</returns>
    public static bool IsDealerItem(this ItemBase item, out DealerInstance owningDealer, out DealerItemInstance dealerItem)
    {
        owningDealer = null!;
        dealerItem = default;

        if (item == null)
            return false;

        for (var i = 0; i < Dealers.Count; i++)
        {
            var dealer = Dealers[i];

            if (dealer.IsDestroyed || !dealer.IsActive)
                continue;

            if (dealer.ActiveInventory.ActiveMapping.TryGetValue(item, out dealerItem))
            {
                owningDealer = dealer;
                return true;
            }
        }

        return false;
    }

    private static float GetChanceForRarity(byte rarityLevel)
    {
        rarityLevel = (byte)Mathf.Clamp(rarityLevel, 0f, 5f);

        switch (rarityLevel)
        {
            case 0: return 5f;
            case 1: return 10f;
            case 2: return 30f;
            case 3: return 50f;
            case 4: return 70f;
            case 5: return 80f;

            default: throw new ArgumentOutOfRangeException(nameof(rarityLevel)); // this can literally never happen
        }
    }

    private static void RefreshInventory(DealerInventory inventory)
    {
        var count = InventorySize.GetRandom();

        while (inventory.Items.Count < count)
        {
            var entry = InventoryItems.GetRandomWeighted(x => GetChanceForRarity(x.Rarity));

            if (entry != null)
            {
                var price = entry.Price;
                var discount = 0;

                if (entry.DiscountChance > 0 && WeightUtils.GetBool(entry.DiscountChance))
                {
                    discount = entry.DiscountRange.GetRandom();

                    if (discount > 0)
                        price -= price * (discount / 100);
                }

                inventory.Items.Add(new(entry, entry.Price, price, discount));
            }
        }
    }

    private static void OnUpdate()
    {
        if (!ExRound.IsRunning)
            return;

        if (SpawnThisRound == 0)
            return;

        if (Dealers.Count >= MaxPerRound)
            return;

        if (SpawnPositions.Count < 1)
            return;

        if (remainingStartWait > 0f)
        {
            remainingStartWait -= Time.deltaTime;
            return;
        }

        if (remainingSpawnWait > 0f)
        {
            remainingSpawnWait -= Time.deltaTime;
            return;
        }

        var spawnName = RandomSpawnName;

        if (MapLocations.TryFind(spawnName, out var position, out var rotation))
        {
            SpawnDealer(position, rotation, spawnName);
            SpawnThisRound--;

            remainingSpawnWait = SpawnDelay;
        }
        else
        {
            ApiLog.Error("Dealer Manager", $"Attempted to spawn a dealer at the position &1{spawnName}&r, but it does not exist! Please check your config.");
        }
    }

    private static void OnRoundWaiting()
    {
        Dealers.Clear(); // just in case

        SpawnThisRound = RandomSpawnCount;

        remainingStartWait = WaitTime;
        remainingSpawnWait = 0f;
    }

    private static void OnRoundRestarting()
    {
        foreach (var dealer in Dealers.ToArray())
        {
            dealer.DestroyInstance();
        }
    }

    private static void OnInteracted(PlayerSearchedToyEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        foreach (var dealer in Dealers)
        {
            if (dealer.IsDestroyed)
                continue;

            if (dealer.Interactable?.Base == null)
                continue;

            if (dealer.Interactable.Base != args.Interactable.Base)
                continue;

            dealer.OnInteracted(player);
            break;
        }
    }

    private static void OnSelectedItem(PlayerSelectedItemEventArgs args)
    {
        foreach (var dealer in Dealers)
        {
            if (dealer.IsDestroyed || !dealer.IsActive)
                continue;

            if (dealer.ActivePlayer != args.Player)
                continue;

            dealer.OnSelected(args.NewItem?.Base ?? null);
        }
    }

    private static void OnDroppingItem(PlayerDroppingItemEventArgs args)
    {
        foreach (var dealer in Dealers)
        {
            if (dealer.IsDestroyed || !dealer.IsActive)
                continue;

            if (dealer.ActivePlayer != args.Player)
                continue;

            args.IsAllowed = dealer.OnDropping(args.Item?.Base ?? null, out var destroyItem);

            if (!args.IsAllowed)
            {
                if (destroyItem)
                    args.Item?.Base.DestroyItem();

                break;
            }
        }
    }

    private static void OnCuffing(PlayerCuffingEventArgs args)
    {
        if (args.Target is not ExPlayer player)
            return;

        if (!player.IsTrading(out _) && !Dealers.Any(x => !x.IsDestroyed && x.Player == player))
            return;

        args.IsAllowed = false;
    }

    private static void OnDying(PlayerDyingEventArgs args)
    {
        if (!args.IsAllowed || args.Player is not ExPlayer player)
            return;

        if (!player.IsTrading(out var dealer))
            return;

        dealer.OnDying();
    }

    private static void OnPickingUpItem(PlayerPickingUpItemEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (!player.IsTrading(out var _))
            return;

        args.IsAllowed = false;
    }

    private static void OnPickingUpAmmo(PlayerPickingUpAmmoEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (!player.IsTrading(out var _))
            return;

        args.IsAllowed = false;
    }

    private static void OnPickingUpArmor(PlayerPickingUpArmorEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (!player.IsTrading(out var _))
            return;

        args.IsAllowed = false;
    }

    private static void OnPickingUpScp330(PlayerPickingUpScp330EventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (!player.IsTrading(out var _))
            return;

        args.IsAllowed = false;
    }

    private static void OnUsingItem(PlayerUsingItemEventArgs args)
    {
        if (!args.UsableItem.Base.IsDealerItem(out var dealer))
            return;

        args.IsAllowed = false;
    }

    private static void OnThrowingProjectile(PlayerThrowingProjectileEventArgs args)
    {
        if (!args.ThrowableItem.Base.IsDealerItem(out var dealer))
            return;

        args.IsAllowed = false;
    }

    private static void OnShootingWeapon(PlayerShootingWeaponEventArgs args)
    {
        if (!args.FirearmItem.Base.IsDealerItem(out var dealer))
            return;

        args.IsAllowed = false;
    }

    private static void OnScp939Attacking(Scp939AttackingEventArgs args)
    {
        if (args.Target is not ExPlayer player)
            return;

        if (!Dealers.Any(x => !x.IsDestroyed && x.Player == player))
            return;

        args.IsAllowed = false;
    }

    private static void Initialize()
    {
        ServerEvents.RoundRestarted += OnRoundRestarting;

        PlayerUpdateHelper.OnUpdate += OnUpdate;

        PlayerEvents.Dying += OnDying;
        PlayerEvents.Cuffing += OnCuffing;
        PlayerEvents.UsingItem += OnUsingItem;
        PlayerEvents.SearchedToy += OnInteracted;
        PlayerEvents.DroppingItem += OnDroppingItem;
        PlayerEvents.ShootingWeapon += OnShootingWeapon;
        PlayerEvents.ThrowingProjectile += OnThrowingProjectile;

        PlayerEvents.PickingUpItem += OnPickingUpItem;
        PlayerEvents.PickingUpAmmo += OnPickingUpAmmo;
        PlayerEvents.PickingUpArmor += OnPickingUpArmor;
        PlayerEvents.PickingUpScp330 += OnPickingUpScp330;

        ExPlayerEvents.SelectedItem += OnSelectedItem;
        ExRoundEvents.WaitingForPlayers += OnRoundWaiting;

        Scp939Events.Attacking += OnScp939Attacking;
    }
}