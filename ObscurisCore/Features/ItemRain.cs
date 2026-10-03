using System.Diagnostics;

using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API;
using LabExtended.Extensions;

using ObscurisCore.Extensions;

using UnityEngine;

namespace ObscurisCore.Features;

/// <summary>
/// Provides functionality to spawn items repeatedly for a specified player, simulating an "item rain" effect in the
/// game environment.
/// </summary>
/// <remarks>Attach this component to a player to enable periodic spawning of items at their location. Use
/// the static methods to start or stop the effect for a given player. The effect can be customized by setting the
/// item type, spawn count, and delay between drops. If the player does not have a valid reference hub, the
/// component will be automatically removed.</remarks>
public class ItemRain : MonoBehaviour
{
    private Stopwatch watch = new();
    private int droppedItems = 0;
    private bool isProjectile;

    /// <summary>
    /// The player targeted by this instance.
    /// </summary>
    public ExPlayer Player { get; private set; }

    /// <summary>
    /// Gets or sets the amount of items to drop.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the delay between each drop in milliseconds.
    /// </summary>
    public int Delay { get; set; }

    /// <summary>
    /// Gets or sets the limit of items to drop. A value of 0 means no limit.
    /// </summary>
    public int Limit { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fuse time for grenades.
    /// </summary>
    public float Fuse { get; set; } = 3f;

    /// <summary>
    /// Gets or sets the item type to drop.
    /// </summary>
    public ItemType Item { get; set; }

    /// <summary>
    /// Gets or sets the scale of the dropped item.
    /// </summary>
    public Vector3 Scale { get; set; } = Vector3.one;

    /// <summary>
    /// Whether or not to spawn grenades as active projectiles.
    /// </summary>
    public bool SpawnActive { get; set; }

    /// <summary>
    /// Whether or not the rain is in progress.
    /// </summary>
    public bool IsRunning => watch.IsRunning;

    /// <summary>
    /// Enables the item rain.
    /// </summary>
    public void Enable()
    {
        droppedItems = 0;

        isProjectile = Item.TryGetItemPrefab(out var prefab) && prefab is ThrowableItem;

        watch.Restart();
    }

    /// <summary>
    /// Disables the item rain.
    /// </summary>
    public void Disable()
    {
        watch.Stop();
        watch.Reset();
    }

    void Start()
    {
        Player = ExPlayer.Get(gameObject)!;

        if (Player?.ReferenceHub == null)
            Destroy(this);
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        if (Limit > 0 && droppedItems >= Limit)
        {
            Disable();
            return;
        }

        if (Player?.ReferenceHub == null)
            return;

        if (!Player.Role.IsAlive)
            return;

        if (Count < 1 || Item == ItemType.None)
            return;

        if (Delay > 0 && watch.ElapsedMilliseconds < Delay)
            return;

        watch.Restart();

        for (var x = 0; x < Count; x++)
        {
            if (SpawnActive && isProjectile)
            {
                if (ExMap.SpawnProjectile(Item, Player.PositionAdjustY(0.5f), Scale, Vector3.zero, Player.Rotation, 0f, Fuse) != null)
                {
                    droppedItems++;
                }
            }
            else
            {
                if (ExMap.SpawnItem(Item, Player.PositionAdjustY(0.5f), Scale, Player.Rotation) != null)
                {
                    droppedItems++;
                }
            }

            if (Limit <= 0 || droppedItems < Limit)
            {
                continue;
            }

            Disable();
            break;
        }
    }

    void OnDestroy()
    {
        Disable();
    }

    /// <summary>
    /// Stops the active item rain effect for the specified player if it matches the given item type.
    /// </summary>
    /// <remarks>If the player does not have an active item rain effect for the specified item type,
    /// no action is taken and the method returns <see langword="false"/>.</remarks>
    /// <param name="player">The player whose item rain effect should be checked and potentially stopped. Must not be null and must have
    /// a valid ReferenceHub.</param>
    /// <param name="type">The item type to match against the player's current item rain effect.</param>
    /// <param name="destroyRain">Indicates whether the item rain component should be destroyed after disabling it. If <see langword="true"/>,
    /// the component is destroyed; otherwise, it is only disabled. The default is <see langword="true"/>.</param>
    /// <returns>Returns <see langword="true"/> if an item rain effect matching the specified type was found and stopped;
    /// otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="player"/> is null or does not have a valid ReferenceHub.</exception>
    public static bool StopItemRainIf(ExPlayer player, ItemType type, bool destroyRain = true)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentException("Player is not valid.", nameof(player));

        if (player.TryGetComponent<ItemRain>(out var itemRain) && itemRain.Item == type)
        {
            itemRain.Disable();

            if (destroyRain)
                Destroy(itemRain);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Stops the item rain effect for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the item rain effect should be stopped. Must not be null and must have a valid reference
    /// hub.</param>
    /// <param name="destroyRain">Indicates whether the item rain component should be destroyed after disabling it. If <see langword="true"/>,
    /// the component is destroyed; otherwise, it is only disabled. The default value is <see langword="true"/>.</param>
    /// <returns>Returns <see langword="true"/> if the item rain effect was active and has been stopped; otherwise, <see
    /// langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="player"/> is null or does not have a valid reference hub.</exception>
    public static bool StopItemRain(ExPlayer player, bool destroyRain = true)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentException("Player is not valid.", nameof(player));

        if (player.TryGetComponent<ItemRain>(out var itemRain))
        {
            itemRain.Disable();

            if (destroyRain)
                Destroy(itemRain);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts an item rain effect for the specified player, spawning multiple items of the given type at set
    /// intervals.
    /// </summary>
    /// <remarks>If an item rain effect is already active for the player, its parameters are updated
    /// and the effect is restarted. Otherwise, a new item rain effect is created and started.</remarks>
    /// <param name="player">The player for whom the item rain effect will be started. Must not be null and must reference a valid
    /// player.</param>
    /// <param name="type">The type of item to spawn during the item rain.</param>
    /// <param name="amount">The number of items to spawn in each rain cycle. Must be greater than zero.</param>
    /// <param name="delay">The delay, in seconds, between each item spawn. Set to 0 for immediate spawning.</param>
    /// <param name="limit">The maximum number of items to spawn in total. Set to 0 for no limit.</param>
    /// <param name="spawnActive">A value indicating whether the spawned items should be active upon creation. Set to <see langword="true"/>
    /// to spawn active items; otherwise, <see langword="false"/>.</param>
    /// <param name="fuseTime">The fuse time, in seconds, for each spawned item. Determines how long the item exists before expiring.</param>
    /// <param name="scale">The scale to apply to each spawned item. If null, the default scale is used.</param>
    /// <returns>An <see cref="ItemRain"/> instance representing the active item rain effect for the player.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="player"/> is null or does not reference a valid player.</exception>
    public static ItemRain StartItemRain(ExPlayer player, ItemType type, int amount = 1, int delay = 0, int limit = 0,
        bool spawnActive = true, float fuseTime = 3f, Vector3? scale = null)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentException("Player is not valid.", nameof(player));

        if (player.TryGetComponent<ItemRain>(out var itemRain))
        {
            itemRain.Item = type;
            itemRain.Count = amount;
            itemRain.Delay = delay;
            itemRain.Scale = scale ?? Vector3.one;
            itemRain.Limit = limit;
            itemRain.SpawnActive = spawnActive;
            itemRain.Fuse = fuseTime;

            itemRain.Enable();
            return itemRain;
        }

        itemRain = player.GameObject!.AddComponent<ItemRain>();

        itemRain.Item = type;
        itemRain.Count = amount;
        itemRain.Delay = delay;
        itemRain.Scale = scale ?? Vector3.one;
        itemRain.Limit = limit;
        itemRain.SpawnActive = spawnActive;
        itemRain.Fuse = fuseTime;

        itemRain.Enable();
        return itemRain;
    }
}