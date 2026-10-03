using System.ComponentModel;
using LabExtended.API;
using LabExtended.Extensions;
using MapGeneration;
using ObscurisCore.Features;
using ObscurisCore.Extensions;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ObscurisCore.Utilities.Configs;

/// <summary>
/// Represents a spawn location with configurable properties, including type, weight, and specific spawn details.
/// </summary>
public class SpawnLocation
{
    /// <summary>
    /// The type of spawn location.
    /// </summary>
    public enum LocationType
    {
        /// <summary>
        /// A custom map location.
        /// </summary>
        Custom,
        
        /// <summary>
        /// A locker chamber.
        /// </summary>
        Locker,
        
        /// <summary>
        /// A random position in a room.
        /// </summary>
        Room,
        
        /// <summary>
        /// An item's default spawn position.
        /// </summary>
        ReplaceItem,
    }
    
    /// <summary>
    /// The weight of the spawn location.
    /// </summary>
    [Description("The weight of the spawn location. Higher values indicate a higher chance of being chosen.")]
    public float Weight { get; set; }

    /// <summary>
    /// The type of spawn location.
    /// </summary>
    [Description("The type of spawn location.")]
    public LocationType Type { get; set; } = LocationType.Custom;
    
    /// <summary>
    /// The item type to replace.
    /// </summary>
    [Description("The item type to replace.")]
    public ItemType ItemType { get; set; } = ItemType.None;
    
    /// <summary>
    /// The zone filter for lockers - None is unrestricted.
    /// </summary>
    [Description("The zone filter for lockers or rooms - None is unrestricted.")]
    public FacilityZone Zone { get; set; } = FacilityZone.None;

    /// <summary>
    /// The room to spawn in.
    /// </summary>
    [Description("The room to spawn in.")]
    public RoomName Room { get; set; } = RoomName.Unnamed;
    
    /// <summary>
    /// The shape of the room.
    /// </summary>
    [Description("The shape of the room.")]
    public RoomShape? Shape { get; set; } = null;

    /// <summary>
    /// The location to spawn in.
    /// </summary>
    [Description("The location to spawn in.")]
    public string? Location { get; set; } = null;
    
    /// <summary>
    /// The maximum amount of items that can spawn here.
    /// </summary>
    [Description("Sets the maximum amount of items that can spawn here.")]
    public int MaxAmount { get; set; }

    /// <summary>
    /// The amount of items that have spawned here.
    /// </summary>
    [YamlIgnore]
    public int SpawnCounter { get; set; } = 0;

    /// <summary>
    /// Attempts to determine a position and rotation for a spawn location based on the configured type and parameters.
    /// </summary>
    /// <param name="destroyTargetPickups">
    /// If true, any target pickups related to the spawn location will be destroyed when using the ReplaceItem location type.
    /// </param>
    /// <param name="clearOutChambers">
    /// If true, clears out the contents of spawn chambers when using the Locker location type.
    /// </param>
    /// <param name="position">
    /// Outputs the determined position for the spawn location, if successful.
    /// </param>
    /// <param name="rotation">
    /// Outputs the determined rotation for the spawn location, if successful.
    /// </param>
    /// <returns>
    /// True if a position and rotation could be determined successfully for the current spawn location type, otherwise false.
    /// </returns>
    public bool TryGetPositionAndRotation(bool destroyTargetPickups, bool clearOutChambers, out Vector3 position,
        out Quaternion rotation)
    {
        position = default;
        rotation = default;

        switch (Type)
        {
            case LocationType.Custom:
            {
                if (!MapLocations.TryFind(Location, out position, out rotation)
                    && !MapLocations.TryFindPrefixed(Location, out position, out rotation))
                    return false;

                return true;
            }

            case LocationType.ReplaceItem:
            {
                if (!ExMap.Pickups.TryGetFirst(p => p.Info.ItemId == ItemType, out var pickup))
                    return false;
                
                position = pickup.Position;
                rotation = pickup.Rotation;

                if (destroyTargetPickups)
                    pickup.DestroySelf();

                return true;
            }

            case LocationType.Locker:
            {
                var locker = ExMap.Lockers.GetRandomItem(l => Zone == FacilityZone.None || (l.ParentRoom != null && l.ParentRoom.Zone == Zone));

                if (locker == null)
                    return false;

                var chamber = locker.Chambers.GetRandomItem();

                if (clearOutChambers)
                    chamber.ToBeSpawned.Clear();
                
                position = chamber.Spawnpoint.position;
                rotation = chamber.Spawnpoint.rotation;

                return true;
            }

            case LocationType.Room:
            {
                if (!RoomUtils.TryFindRoom(Room, Zone is FacilityZone.None ? null : Zone, Shape, out var room))
                    return false;

                try
                {
                    position = room.GetSafePosition(ExPlayer.Host);
                    rotation = Quaternion.identity;
                }
                catch
                {
                    position = default;
                    rotation = default;

                    return false;
                }

                return true;
            }
        }

        return false;
    }
}