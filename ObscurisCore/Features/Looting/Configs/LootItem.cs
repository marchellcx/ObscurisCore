using LabExtended.API.Custom.Items;
using LabExtended.Core;
using LabExtended.Extensions;
using ObscurisCore.Extensions;
using UnityEngine;
using StringExtensions = ObscurisCore.Extensions.StringExtensions;

namespace ObscurisCore.Features.Looting.Configs;

/// <summary>
/// Represents an item that can be looted.
/// </summary>
public struct LootItem
{
    /// <summary>
    /// Whether the item is an ammo or not.
    /// </summary>
    public readonly bool IsAmmo;

    /// <summary>
    /// Whether the item is an item or not.
    /// </summary>
    public readonly bool IsItem;

    /// <summary>
    /// Whether the item is an action or not.
    /// </summary>
    public readonly bool IsAction;

    /// <summary>
    /// Whether the item is a custom item or not.
    /// </summary>
    public readonly bool IsCustomItem;

    /// <summary>
    /// Whether the item should spawn on the ground or not.
    /// </summary>
    public readonly bool SpawnOnGround;

    /// <summary>
    /// Whether the item should use the player's velocity or not.
    /// </summary>
    public readonly bool UsePlayerVelocity;
    
    /// <summary>
    /// The time in seconds the item should be fused.
    /// </summary>
    public readonly float FuseTime;

    /// <summary>
    /// The force of the item.
    /// </summary>
    public readonly float ThrowForce;
    
    /// <summary>
    /// The scale of the item.
    /// </summary>
    public readonly Vector3 Scale;

    /// <summary>
    /// The velocity of the item.
    /// </summary>
    public readonly Vector3 Velocity;

    /// <summary>
    /// The amount of the item.
    /// </summary>
    public readonly int Amount;

    /// <summary>
    /// The type of the item.
    /// </summary>
    public readonly ItemType Type;
    
    /// <summary>
    /// The action of the item.
    /// </summary>
    public readonly LootFunction? Function;

    /// <summary>
    /// The custom item of the item.
    /// </summary>
    public readonly CustomItem? CustomItem;
    
    /// <summary>
    /// The arguments of the item.
    /// </summary>
    public readonly string[]? Args;
    
    /// <summary>
    /// Creates a new instance of the LootItem struct.
    /// </summary>
    public LootItem(bool isAmmo, bool isItem, bool isAction, bool isCustomItem, bool spawnOnGround, bool usePlayerVelocity, float fuseTime, float throwForce,
        Vector3 scale, Vector3 velocity, int amount, ItemType type, LootFunction? func, CustomItem? customItem, string[]? args)
    {
        IsAmmo = isAmmo;
        IsItem = isItem;
        IsAction = isAction;
        IsCustomItem = isCustomItem;
        SpawnOnGround = spawnOnGround;
        UsePlayerVelocity = usePlayerVelocity;
        FuseTime = fuseTime;
        ThrowForce = throwForce;
        Scale = scale;
        Velocity = velocity;
        Amount = amount;
        Type = type;
        Function = func;
        CustomItem = customItem;
        Args = args;
    }

    /// <summary>
    /// Parses a string array representing item definitions and populates a list of LootItem objects.
    /// </summary>
    /// <param name="parts">An array of string where each entry represents an item definition. Each string is expected to be structured in a specific format.</param>
    /// <param name="items">A list to be populated with LootItem objects parsed from the provided string array.</param>
    public static void ParseItems(string[] parts, List<LootItem> items)
    {
        for (var x = 0; x < parts.Length; x++)
        {
            var part = parts[x].Trim();
            var splits = part.Split(':');

            if (splits.Length < 1)
                continue;

            if (Enum.TryParse(splits[0], true, out ItemType type))
            {
                bool usePlayerVelocity = false;

                Vector3 velocity = default;
                
                if (splits.Length < 2 || !int.TryParse(splits[1], out var amount))
                    amount = 1;
                
                if (splits.Length < 3 || !bool.TryParse(splits[2], out var spawnOnGround))
                    spawnOnGround = false;
                
                if (splits.Length < 4 || !splits[3].TryParseVector3(out var scale))
                    scale = Vector3.one;

                if (splits.Length < 5 || !float.TryParse(splits[4], out var fuseTime))
                    fuseTime = 3f;
                
                if (splits.Length < 6 || !float.TryParse(splits[5], out var throwForce))
                    throwForce = 3f;

                if (splits.Length > 6)
                {
                    if (!bool.TryParse(splits[6], out usePlayerVelocity))
                    {
                        StringExtensions.TryParseVector3(splits[5], out velocity);
                    }
                }
                
                items.Add(new(
                    type.IsAmmo(),
                    true,
                    false,
                    false,
                    spawnOnGround,
                    usePlayerVelocity,
                    fuseTime,
                    throwForce,
                    scale,
                    velocity,
                    amount,
                    type,
                    null,
                    null,
                    null));
            }
            else if (CustomItem.TryGet(splits[0], out var customItem))
            {
                if (splits.Length < 2 || !int.TryParse(splits[1], out var amount))
                    amount = 1;
                
                if (splits.Length < 3 || !bool.TryParse(splits[2], out var spawnOnGround))
                    spawnOnGround = false;
                
                if (splits.Length < 4 || !float.TryParse(splits[3], out var fuseTime))
                    fuseTime = 3f;
                
                items.Add(new(
                    false,
                    false,
                    false,
                    true,
                    spawnOnGround,
                    false,
                    fuseTime,
                    0f,
                    Vector3.one,
                    Vector3.one,
                    amount,
                    ItemType.None,
                    null,
                    customItem,
                    null));
            }
            else if (LootManager.Functions.TryGetValue(splits[0], out var action))
            {
                var amount = 1;
                var args = Array.Empty<string>();

                if (splits.Length > 1)
                {
                    int.TryParse(splits[1], out amount);
                    
                    if (splits.Length > 2)
                        args = splits.Skip(2).ToArray();
                }
                
                items.Add(new(
                    false,
                    false,
                    true,
                    false,
                    false,
                    false,
                    0f,
                    0f,
                    Vector3.one,
                    Vector3.one,
                    amount,
                    ItemType.None,
                    action,
                    null,
                    args));
            }
            else
            {
                ApiLog.Warn("LootManager", $"Could not parse item &3{part}&r: unknown type!");
            }
        }
    }
}