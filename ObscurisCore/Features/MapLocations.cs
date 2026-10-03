using AdminToys;

using LabExtended.API;
using LabExtended.API.Toys;

using LabExtended.Core;
using LabExtended.Core.Configs.Objects;

using MapGeneration;

using Serialization;

using UnityEngine;

namespace ObscurisCore.Features;

/// <summary>
/// Provides functionality to manage and manipulate locations on a map.
/// </summary>
public static class MapLocations
{
    /// <summary>
    /// Represents information about a location on the map.
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// The position of the location.
        /// </summary>
        public YamlVector3 Position { get; set; } = new(Vector3.one);
        
        /// <summary>
        /// The index of the room.
        /// </summary>
        public int? RoomIndex { get; set; }

        /// <summary>
        /// The angle of the location.
        /// </summary>
        public float? Angle { get; set; } = null;

        /// <summary>
        /// The parent of the room.
        /// </summary>
        public string? Parent { get; set; } = null;
        
        /// <summary>
        /// The name of the room.
        /// </summary>
        public RoomName Name { get; set; } = RoomName.Unnamed;

        /// <summary>
        /// The shape of the room.
        /// </summary>
        public RoomShape? Shape { get; set; } = null;

        /// <summary>
        /// The zone of the room.
        /// </summary>
        public FacilityZone? Zone { get; set; } = null;
    }

    /// <summary>
    /// A dictionary containing location data mapped to their respective identifiers.
    /// </summary>
    /// <remarks>
    /// Each key in the dictionary corresponds to a unique identifier for a location.
    /// The value is an instance of <see cref="MapLocations.LocationInfo"/> that holds detailed information about the location,
    /// such as position, rotation, name, shape, and zone.
    /// </remarks>
    public static Dictionary<string, LocationInfo> Locations { get; } = new();

    /// <summary>
    /// Spawns a primitive object at the location specified by its unique name.
    /// </summary>
    /// <param name="name">
    /// The unique name of the map location where the primitive object should be spawned. Must not be null or empty.
    /// </param>
    /// <returns>
    /// An instance of <see cref="PrimitiveToy"/> if the location is found and the object is successfully spawned; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is null or empty.
    /// </exception>
    public static PrimitiveToy? SpawnPrimitive(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!TryFind(name, out var position, out var rotation))
            return null;

        return new PrimitiveToy(position, rotation, PrimitiveType.Cube,
            PrimitiveFlags.Collidable | PrimitiveFlags.Visible)
        {
            Color = Color.red
        };
    }

    /// <summary>
    /// Removes the specified map location by its unique name.
    /// </summary>
    /// <param name="name">
    /// The unique name of the map location to be removed. Must not be null or empty.
    /// </param>
    /// <returns>
    /// <c>true</c> if the map location with the specified name is found and successfully removed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is null or empty.
    /// </exception>
    public static bool Remove(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!Locations.Remove(name))
            return false;
        
        Save();
        return true;  
    }
    
    /// <summary>
    /// Modifies the details of a specified map location using the provided modification delegate.
    /// </summary>
    /// <param name="name">
    /// The unique name of the map location to be modified. Must not be null or empty.
    /// </param>
    /// <param name="modify">
    /// A delegate function that performs the modification on the <see cref="LocationInfo"/> object of the specified map location.
    /// </param>
    /// <returns>
    /// <c>true</c> if the map location with the specified name is found and successfully modified; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> is null or empty, or if <paramref name="modify" /> is null.
    /// </exception>
    public static bool Modify(string name, Action<LocationInfo> modify)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (modify == null)
            throw new ArgumentNullException(nameof(modify));
        
        if (!TryFind(name, out var info))
            return false;
        
        modify(info);
        
        Save();
        return true;   
    }

    /// <summary>
    /// Spawns an object at the specified map location using the provided spawn function.
    /// </summary>
    /// <param name="name">
    /// The unique name of the map location where the object should be spawned. Must not be null or empty.
    /// </param>
    /// <param name="spawnFunction">
    /// A delegate function that handles the spawning logic, invoked with the position and rotation of the map location.
    /// </param>
    /// <returns>
    /// <c>true</c> if the map location with the specified name is found and the spawn function is executed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> is null or empty, or if <paramref name="spawnFunction" /> is null.
    /// </exception>
    public static bool SpawnAt(string name, Action<Vector3, Quaternion> spawnFunction)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (spawnFunction == null)
            throw new ArgumentNullException(nameof(spawnFunction));

        if (!TryFind(name, out var position, out var rotation))
            return false;
        
        spawnFunction(position, rotation);
        return true;
    }

    /// <summary>
    /// Spawns an object at the first map location whose name begins with the specified prefix using the provided spawn function.
    /// </summary>
    /// <param name="prefix">
    /// The prefix of the map location name to search for. Must not be null or empty.
    /// </param>
    /// <param name="spawnFunction">
    /// A delegate function that handles the spawning logic, invoked with the position and rotation of the matching map location.
    /// </param>
    /// <returns>
    /// <c>true</c> if a map location with a name starting with the specified prefix is found and the spawn function is executed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="prefix" /> is null or empty, or if <paramref name="spawnFunction" /> is null.
    /// </exception>
    public static bool SpawnAtPrefixed(string prefix, Action<Vector3, Quaternion> spawnFunction)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentNullException(nameof(prefix));

        if (spawnFunction == null)
            throw new ArgumentNullException(nameof(spawnFunction));
        
        if (!TryFindPrefixed(prefix, out var position, out var rotation))
            return false;
        
        spawnFunction(position, rotation);
        return true;   
    }

    /// <summary>
    /// Attempts to find a map location whose name starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">
    /// The prefix used to search for a matching map location. Must not be null or empty.
    /// </param>
    /// <param name="position">
    /// When this method returns, contains the position of the found map location, if a match is found; otherwise, the default value for <see cref="Vector3" />.
    /// </param>
    /// <param name="rotation">
    /// When this method returns, contains the rotation of the found map location, if a match is found; otherwise, the default value for <see cref="Quaternion" />.
    /// </param>
    /// <returns>
    /// <c>true</c> if a map location whose name starts with the specified prefix is found, and its position and rotation are successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="prefix" /> is null or empty.
    /// </exception>
    public static bool TryFindPrefixed(string prefix, out Vector3 position, out Quaternion rotation)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentNullException(nameof(prefix));
        
        foreach (var kvp in Locations)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                if (TryFind(kvp.Key, out position, out rotation))
                {
                    return true;
                }
            }
        }

        position = default;
        rotation = default;

        return false;
    }

    /// <summary>
    /// Attempts to find a map location by name and retrieves its position and rotation if found.
    /// </summary>
    /// <param name="name">
    /// The unique name of the map location to search for. Must not be null or empty.
    /// </param>
    /// <param name="position">
    /// When this method returns, contains the position of the map location if found, or the default value of <see cref="Vector3"/> if not found.
    /// </param>
    /// <param name="rotation">
    /// When this method returns, contains the rotation of the map location if found, or the default value of <see cref="Quaternion"/> if not found.
    /// </param>
    /// <returns>
    /// <c>true</c> if a map location with the specified name is found; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> is null or empty.
    /// </exception>
    public static bool TryFind(string name, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = default;

        if (!TryFind(name, out var info))
            return false;

        if (!RoomUtils.TryFindRoom(info.Name, info.Zone, info.Shape, out var room))
        {
            if (info.RoomIndex.HasValue && !string.IsNullOrEmpty(info.Parent))
            {
                ApiLog.Debug("MapLocations", $"Attempting to find room by index &1{info.RoomIndex}&r for location &1{name}&r (Parent: &6{info.Parent ?? "(null)&r)"}");
                
                var parent = GameObject.Find(info.Parent);

                if (parent == null)
                {
                    ApiLog.Warn("MapLocations", $"Failed to find parent &1{info.Parent}&r");
                    return false;
                }

                var component = parent.GetComponentAtIndex(info.RoomIndex.Value);

                if (component == null)
                {
                    ApiLog.Warn("MapLocations", $"Failed to find component at index &1{info.RoomIndex}&r");
                    return false;
                }

                if (component is not RoomIdentifier roomIdentifier)
                {
                    ApiLog.Warn("MapLocations", $"Component at index &1{info.RoomIndex}&r is not a RoomIdentifier (&6{component.GetType()}&r)");
                    return false;
                }

                room = roomIdentifier;
            }
            else
            {
                ApiLog.Warn("MapLocations", $"Failed to find room for location &1{name}&r");
                return false;
            }
        }

        if (room == null)
        {
            ApiLog.Warn("MapLocations", $"Failed to find room for location &1{name}&r");
            return false;
        }
        
        rotation = Quaternion.Euler(0f, room.transform.rotation.eulerAngles.y + info.Angle ?? 0f, 0f);
        position = room.transform.TransformPoint(info.Position.Vector);

        return true;
    }

    /// <summary>
    /// Attempts to find a location by its unique name and retrieves its associated information.
    /// </summary>
    /// <param name="name">
    /// The unique name of the location to find. Must not be null or empty.
    /// </param>
    /// <param name="info">
    /// When this method returns, contains the <see cref="LocationInfo"/> associated with the specified name, if the name is found; otherwise, <c>null</c>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the location with the specified name is found; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is null or empty.
    /// </exception>
    public static bool TryFind(string name, out LocationInfo info)
        => Locations.TryGetValue(name, out info);

    /// <summary>
    /// Saves location information to the map using the player's current position and room.
    /// </summary>
    /// <param name="name">
    /// The unique name for the location to be saved. Must not be null or empty.
    /// </param>
    /// <param name="player">
    /// The player whose position and room will be used for saving the location. Must not be null.
    /// </param>
    /// <param name="angle">
    /// The optional rotation angle of the location. Can be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> or <paramref name="player" /> is null or empty.
    /// </exception>
    public static void Save(string name, ExPlayer player, float? angle = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (player == null)
            throw new ArgumentNullException(nameof(player));
        
        Save(name, player.Position.Room, player.Position, angle);
    }

    /// <summary>
    /// Saves location information to the map.
    /// </summary>
    /// <param name="name">
    /// The unique name for the location to be saved. Must not be null or empty.
    /// </param>
    /// <param name="room">
    /// The room associated with the location being saved. Must not be null.
    /// </param>
    /// <param name="position">
    /// The position of the location within the room, specified as a Vector3.
    /// </param>
    /// <param name="angle">
    /// The optional rotation angle of the location. Can be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> or <paramref name="room" /> is null or empty.
    /// </exception>
    public static void Save(string name, RoomIdentifier room, Vector3 position, float? angle = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (room == null)
            throw new ArgumentNullException(nameof(room));

        Locations[name] = new()
        {
            Position = new(room.transform.InverseTransformPoint(position)),

            Angle = angle,

            Name = room.Name,
            Shape = room.Shape,
            Zone = room.Zone,

            Parent = room.transform.parent?.name,
            RoomIndex = room.GetComponentIndex()
        };
        
        Save();
    }

    /// <summary>
    /// Saves location information to the map.
    /// </summary>
    /// <param name="name">
    /// The unique identifier for the location. Must not be null or empty.
    /// </param>
    /// <param name="position">
    /// The position of the location within the map, specified as a Vector3.
    /// </param>
    /// <param name="roomIndex">
    /// The optional index of the room associated with the location. Can be null.
    /// </param>
    /// <param name="angle">
    /// The optional rotation angle of the location. Can be null.
    /// </param>
    /// <param name="room">
    /// The name of the room associated with the location. Must not be <see cref="RoomName.Unnamed" />.
    /// </param>
    /// <param name="shape">
    /// The optional shape of the room associated with the location. Can be null.
    /// </param>
    /// <param name="zone">
    /// The optional facility zone associated with the location. Can be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="room" /> is <see cref="RoomName.Unnamed" />.
    /// </exception>
    public static void Save(string name, Vector3 position, int? roomIndex, float? angle, RoomName room,
        RoomShape? shape, FacilityZone? zone)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (room is RoomName.Unnamed)
            throw new ArgumentException("Room name cannot be Unnamed.", nameof(room));

        Locations[name] = new LocationInfo
        {
            Position = new YamlVector3(position),
            
            Angle = angle,
            Name = room,
            Shape = shape,
            Zone = zone,
            RoomIndex = roomIndex
        };
        
        Save();
    }

    /// <summary>
    /// Saves location information to a persistent storage file.
    /// </summary>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the save process.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the application does not have sufficient permission to access the save file.
    /// </exception>
    /// <remarks>
    /// This method serializes all saved locations into a JSON file stored in the application's root directory.
    /// If an error occurs during the save process, it is logged, and no file changes are made.
    /// </remarks>
    public static void Save()
    {
        try
        {
            var path = Path.Combine(ObscurisPlugin.RootDirectory, "locations.yml");
            var yml = YamlParser.Serializer.Serialize(Locations);

            File.WriteAllText(path, yml);
        }
        catch (Exception ex)
        {
            ApiLog.Error("MapLocations", $"Error while saving locations:\n{ex}");
        }
    }

    private static void Initialize()
    {
        try
        {
            var path = Path.Combine(ObscurisPlugin.RootDirectory, "locations.yml");

            if (!File.Exists(path))
                return;

            var yml = File.ReadAllText(path);
            var dict = YamlParser.Deserializer.Deserialize<Dictionary<string, LocationInfo>>(yml);

            if (dict == null)
            {
                ApiLog.Warn("MapLocations", "Failed to deserialize locations.json!");
                return;
            }

            foreach (var kvp in dict)
            {
                ApiLog.Debug("MapLocations", $"Loaded location &1{kvp.Key}&r");

                Locations[kvp.Key] = kvp.Value;
            }

            ApiLog.Info("MapLocations", $"Loaded &1{Locations.Count}&r locations");
        }
        catch (Exception ex)
        {
            ApiLog.Error("MapLocations", $"Error while loading locations:\n{ex}");
        }
    }
}