using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;

using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.Core;

using MapGeneration;

using ObscurisCore.Features;
using ObscurisCore.Extensions;

using UnityEngine;

namespace ObscurisCore.Utilities;

/// <summary>
/// The <c>SpawnDispatcher</c> class provides utilities for managing spawn operations
/// in a game or simulation environment. It defines and enforces rules around
/// occupied spaces, allowable spawn zones, and the spawning of objects in specific locations.
/// </summary>
public class SpawnDispatcher
{
    private static FacilityZone[] surface = [FacilityZone.Surface];
    private static FacilityZone[] entranceHeavySurface = [FacilityZone.Entrance, FacilityZone.HeavyContainment, FacilityZone.Surface];

    /// <summary>
    /// Represents a collection of three-dimensional coordinates that are currently occupied.
    /// </summary>
    public static HashSet<Vector3> OccupiedCoords = new();

    /// <summary>
    /// Maintains a collection of room identifiers that are currently occupied.
    /// </summary>
    public static List<RoomIdentifier> OccupiedRooms = new();
    
    /// <summary>
    /// Represents the valid facility zones where Jack-O-Lanterns can spawn,
    /// dynamically determined based on the current game state such as warhead detonation
    /// or decontamination events.
    /// </summary>
    public static FacilityZone[] ValidSpawnZones
    {
        get
        {
            if (Warhead.IsDetonated)
                return surface;
            
            if (Decontamination.IsDecontaminating)
                return entranceHeavySurface;

            return PlayerExtensions.AllZones;
        }
    }

    /// <summary>
    /// Defines the maximum number of attempts the system will make to find a valid spawn point.
    /// </summary>
    public int SpawnAttempts = 10;

    /// <summary>
    /// Specifies the maximum number of objects allowed to spawn within a single room.
    /// </summary>
    public int ObjectsPerRoom = 1;

    /// <summary>
    /// Defines the vertical size of spawnable objects, used for spatial validation during spawn point determination.
    /// </summary>
    public float ObjectHeight = 0.5f;

    /// <summary>
    /// Defines the radius used for object placement calculations during spawn operations.
    /// </summary>
    public float ObjectRadius = 0.5f;

    /// <summary>
    /// Whether or not to use Pocket Dimension exit positions as potential spawn points.
    /// </summary>
    public bool UsePocketDimensionExit = true;

    /// <summary>
    /// Specifies a prefix used to filter spawn locations by identifier or name.
    /// </summary>
    public string SpawnLocationPrefix = string.Empty;

    /// <summary>
    /// Attempts to spawn the specified number of objects by delegating the spawning action to the given method.
    /// Returns the total count of successfully spawned objects.
    /// </summary>
    /// <param name="objectCount">The number of objects to attempt to spawn.</param>
    /// <param name="spawnDelegate">The delegate method to execute for spawning each object, provided with the spawn position and rotation.</param>
    /// <returns>
    /// The number of objects that were successfully spawned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="spawnDelegate"/> is null.</exception>
    public int TrySpawnObjects(int objectCount, Action<Vector3, Quaternion> spawnDelegate)
    {
        if (spawnDelegate == null)
            throw new ArgumentNullException(nameof(spawnDelegate));

        if (objectCount < 1)
            return 0;

        var count = 0;
        
        for (var x = 0; x < objectCount; x++)
        {
            if (!TryGetSpawnPoint(out var position, out var rotation))
                continue;
            
            spawnDelegate(position, rotation);

            count++;
        }

        return count;
    }

    /// <summary>
    /// Attempts to spawn a single object by identifying an available spawn point
    /// and invoking the delegate method to spawn the object at the specified position and rotation.
    /// </summary>
    /// <param name="spawnDelegate">
    /// The method to execute for spawning the object. It is provided with the spawn position and rotation.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the object was successfully spawned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="spawnDelegate"/> is null.
    /// </exception>
    public bool TrySpawnObject(Action<Vector3, Quaternion> spawnDelegate)
    {
        if (spawnDelegate == null)
            throw new ArgumentNullException(nameof(spawnDelegate));

        if (!TryGetSpawnPoint(out var position, out var rotation))
            return false;

        spawnDelegate(position, rotation);
        return true;
    }

    /// <summary>
    /// Attempts to find a valid spawn point based on the specified criteria and updates the position and rotation accordingly.
    /// </summary>
    /// <param name="position">The position of the spawn point that will be set if a valid spawn point is found.</param>
    /// <param name="rotation">The rotation of the spawn point that will be set if a valid spawn point is found.</param>
    /// <returns>
    /// True if a valid spawn point is found and successfully assigned; otherwise, false.
    /// </returns>
    public bool TryGetSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        bool PrefixPredicate(MapLocations.LocationInfo locationInfo, Vector3 foundPosition, Quaternion foundRotation)
        {
            return !OccupiedCoords.Contains(foundPosition);
        }

        bool InternalGetSpawnPoint(out Vector3 pos, out Quaternion rot)
        {
            if (UsePocketDimensionExit)
            {
                pos = ExMap.GetPocketExitPosition(Vector3.zero, ObjectRadius, ObjectHeight, ValidSpawnZones);
                rot = Quaternion.identity;

                var colliders = Physics.OverlapSphere(pos, ObjectRadius);

                if (colliders.Any(c => c.gameObject.TryFindComponent<BasicDoorButton>(out _) || c.gameObject.TryFindComponent<DoorVariant>(out _)))
                {
                    ApiLog.Debug("SpawnDispatcher", $"Spawn point at {pos.ToPreciseString()} is invalid due to nearby door.");
                    return false;
                }

                if (pos.TryGetRoom(out var room))
                {
                    if (ObjectsPerRoom > 0 && OccupiedRooms.Count(x => x == room) >= ObjectsPerRoom)
                        return false;

                    OccupiedRooms.Add(room);
                }

                if (pos != Vector3.zero && OccupiedCoords.Add(pos))
                {
                    return true;
                }

                rot = default;
                return false;
            }

            if (MapLocations.TryFindPrefixed(SpawnLocationPrefix, PrefixPredicate, out pos, out rot))
            {
                if (pos.TryGetRoom(out var room))
                {
                    if (ObjectsPerRoom > 0 && OccupiedRooms.Count(x => x == room) >= ObjectsPerRoom)
                        return false;

                    OccupiedRooms.Add(room);
                }

                OccupiedCoords.Add(pos);
                return true;
            }

            ApiLog.Warn("SpawnDispatcher", "Failed to find a valid spawn point");

            pos = default;
            rot = default;

            return false;
        }

        for (var x = 0; x < SpawnAttempts; x++)
        {
            if (InternalGetSpawnPoint(out position, out rotation))
            {
                return true;
            }
        }

        position = default;
        rotation = default;
        
        return false;
    }
}