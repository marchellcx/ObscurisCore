using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabApi.Features.Wrappers;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Core.Configs.Objects;

using LabExtended.Events;
using LabExtended.Events.Player;

using MapGeneration;

using MEC;

using NiveraAPI.Extensions;
using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;

using ObscurisCore.Features.Rays;
using ObscurisCore.Features.Elements.Alerts;

using PlayerStatsSystem;

using ProjectMER.Features;

using UnityEngine;

namespace ObscurisCore.Features.Events.Halloween.Lanterns;

/// <summary>
/// Provides functionality for spawning Jack-O-Lanterns as part of the Halloween event.
/// This class contains configuration options and methods for managing Jack-O-Lantern
/// spawns, determining spawn points, scaling, and awarding points to players for destroying lanterns.
/// </summary>
public static class LanternSpawner
{
    internal static HashSet<ushort> collectorWeapons = new();
    
    private static readonly HashSet<Vector3> occupiedCoors = new();
    private static readonly List<RoomIdentifier> occupiedRooms = new();

    private static FacilityZone[] surface = [FacilityZone.Surface];
    private static FacilityZone[] entranceHeavySurface = [FacilityZone.Entrance, FacilityZone.HeavyContainment, FacilityZone.Surface];
    
    /// <summary>
    /// Represents the number of points awarded for destroying a Jack-O-Lantern during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-points", "The amount of points awarded for destroying a Jack-O-Lantern.")]
    public static int LanternPoints = 1;

    /// <summary>
    /// Defines the number of Jack-O-Lanterns that should spawn during each round of the Halloween event. 
    /// </summary>
    [Config("halloween", "lanterns-per-round", "The amount of Jack-O-Lanterns that should spawn per round.")]
    public static int LanternsPerRound = 20;

    /// <summary>
    /// Represents the number of Jack-O-Lanterns that should spawn per room during the Halloween event.
    /// </summary>
    [Config("halloween", "lanterns-per-room", "The amount of Jack-O-Lanterns that should spawn per room.")]
    public static int LanternsPerRoom = 1;

    /// <summary>
    /// Specifies the number of attempts to spawn a Jack-O-Lantern before aborting the process.
    /// </summary>
    [Config("halloween", "lantern-spawn-tries", "The number of attempts to spawn a Jack-O-Lantern before giving up.")]
    public static int LanternSpawnTries = 10;

    /// <summary>
    /// Specifies the schematic name for the Jack-O-Lantern entity used in the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-schematic", "The schematic name of the Jack-O-Lantern entity.")]
    public static string LanternSchematic = "Lantern";

    /// <summary>
    /// Defines the radius of the Jack-O-Lantern entity used in the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-radius", "The radius of the Jack-O-Lantern entity.")]
    public static float LanternRadius = 1f;

    /// <summary>
    /// Represents the height of the Jack-O-Lantern entity used during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-height", "The height of the Jack-O-Lantern entity.")]
    public static float LanternHeight = 1f;

    /// <summary>
    /// Specifies the prefix used to identify spawn points for Jack-O-Lanterns during the Halloween event.
    /// This prefix is utilized to locate and configure specific spawn locations for event entities.
    /// </summary>
    [Config("halloween", "lantern-spawn-point-prefix", "The prefix used for Jack-O-Lantern spawn points.")]
    public static string LanternSpawnPointPrefix = "LanternSpawn";
    
    /// <summary>
    /// Indicates whether Jack-O-Lantern spawn points should be determined using pocket dimension exit locations during the Halloween event.
    /// </summary>
    [Config("halloween", "lanterns-use-pocket-exit-spawn-points", "Whether to use pocket exit spawn points for Jack-O-Lanterns.")]
    public static bool UsePocketExitSpawnPoints = true;

    /// <summary>
    /// Determines the scale of the Jack-O-Lantern schematics spawned during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-scale", "The scale of spawned Jack-O-Lantern schematics.")]
    public static YamlVector3 LanternScale = new(1f, 1f, 1f);

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
    /// Attempts to spawn a Jack-O-Lantern in a valid location using predefined spawning rules and configurations.
    /// If the spawning operation is successful, the resulting lantern object will be initialized and assigned to the output parameter.
    /// </summary>
    /// <param name="lanternObject">
    /// An output parameter that will contain the initialized LanternObject instance if the spawn operation is successful;
    /// otherwise, it will remain uninitialized.
    /// </param>
    /// <returns>
    /// Returns true if a Jack-O-Lantern is successfully spawned and assigned to the output parameter;
    /// otherwise, false, indicating that all spawn attempts have failed.
    /// </returns>
    public static bool TrySpawnLantern(out LanternObject lanternObject)
    {
        lanternObject = null!;

        for (var x = 0; x < LanternSpawnTries; x++)
        {
            if (!TryGetSpawnPoint(out var position, out var rotation))
            {
                continue;
            }

            if (!ObjectSpawner.TrySpawnSchematic(LanternSchematic, position, rotation, LanternScale.Vector, out var schematicObject))
            {
                ApiLog.Warn("LanternSpawner", $"Failed to spawn lantern schematic &3{LanternSchematic}&r!");
                continue;
            }

            lanternObject = new();
            lanternObject.SchematicObject = schematicObject;

            schematicObject.gameObject.AddObject(lanternObject);
            return true;
        }

        ApiLog.Warn("LanternSpawner", $"Failed to spawn a lantern after &3{LanternSpawnTries}&r attempts");
        return false;
    }

    /// <summary>
    /// Attempts to locate a valid spawn point for a Jack-O-Lantern based on defined spawn criteria,
    /// such as predefined spawn points or pocket exit mechanics. This method determines the position
    /// and rotation of a suitable spawn point, ensuring it adheres to conditions like non-occupancy.
    /// </summary>
    /// <param name="position">
    /// An output parameter that will contain the 3D position of the identified spawn point if the operation is successful;
    /// otherwise, it will be set to the default value.
    /// </param>
    /// <param name="rotation">
    /// An output parameter that will contain the rotational information (Quaternion) of the identified spawn point if the operation is successful;
    /// otherwise, it will be set to the default value.
    /// </param>
    /// <returns>
    /// Returns true if a valid spawn point for the Jack-O-Lantern is successfully identified and the output parameters are set;
    /// otherwise, false, indicating that no eligible spawn point was found.
    /// </returns>
    public static bool TryGetSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        bool PrefixPredicate(MapLocations.LocationInfo locationInfo, Vector3 foundPosition, Quaternion foundRotation)
        {
            return !occupiedCoors.Contains(foundPosition);
        }

        if (UsePocketExitSpawnPoints)
        {
            position = ExMap.GetPocketExitPosition(Vector3.zero, LanternRadius, LanternHeight, ValidSpawnZones);
            rotation = Quaternion.identity;

            var colliders = Physics.OverlapSphere(position, 1f);

            if (colliders.Any(c => c.gameObject.TryFindComponent<BasicDoorButton>(out _) || c.gameObject.TryFindComponent<DoorVariant>(out _)))
            {
                ApiLog.Debug("LanternSpawner", $"Spawn point at {position.ToPreciseString()} is invalid due to nearby door.");
                return false;
            }
            
            if (position.TryGetRoom(out var room))
            {
                if (LanternsPerRoom > 0 && occupiedRooms.Count(x => x == room) >= LanternsPerRoom)
                    return false;
                
                occupiedRooms.Add(room);
            }

            if (position != Vector3.zero && occupiedCoors.Add(position))
            {
                return true;
            }

            rotation = default;
            return false;
        }
        
        if (MapLocations.TryFindPrefixed(LanternSpawnPointPrefix, PrefixPredicate, out position, out rotation))
        {
            if (position.TryGetRoom(out var room))
            {
                if (LanternsPerRoom > 0 && occupiedRooms.Count(x => x == room) >= LanternsPerRoom)
                    return false;
                
                occupiedRooms.Add(room);
            }
            
            occupiedCoors.Add(position);
            return true;
        }
        
        ApiLog.Warn("LanternSpawner", "Failed to find a valid spawn point");

        position = default;
        rotation = default;
        
        return false;
    }
    
    /// <summary>
    /// Removes all Jack-O-Lantern objects currently active in the game world by iterating through
    /// RayComponent instances and checking their managed objects for Jack-O-Lantern entities.
    /// Identified Jack-O-Lantern objects will be removed from their corresponding RayComponent containers.
    /// </summary>
    public static void DestroyLanterns()
    {
        var components = UnityEngine.Object.FindObjectsByType<RayComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (var x = 0; x < components.Length; x++)
        {
            var component = components[x];

            if (component.Objects.TryGetFirst<LanternObject>(out var lanternObject))
            {
                component.RemoveObject(lanternObject);
            }
        }
    }

    private static void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        if (occupiedCoors.Count < 1)
            return;
        
        Timing.CallDelayed(1.5f, () =>
        {
            if (!args.Player.CastPlayer(out var player))
                return;
            
            if (!player.Role.IsAlive || player.Role.IsScp || player.Role.IsTutorial)
                return;

            if (player.Inventory.Firearms.Any())
                return;

            var item = player.Inventory.AddItem(ItemType.GunCOM15);

            collectorWeapons.Add(item.ItemSerial);
            
            player.SendAlert(AlertType.Info, 5f, "Collector Weapon", "Získal jsi <color=red>Collector Weapon</color>\nTato zbraň slouží pouze ke sbírání <color=orange>dýní</color>, nedává damage hráčům!");
        });
    }

    private static void OnSelected(PlayerSelectedItemEventArgs args)
    {
        if (args.NewItem != null && collectorWeapons.Contains(args.NewItem.Serial))
        {
            args.Player.Toggles.HasUnlimitedAmmo = true;
            args.Player.SendAlert(AlertType.Info, 3f, "Collector Weapon", "Držíš <color=red>Collector Weapon</color>\nTato zbraň slouží pouze ke sbírání <color=orange>dýní</color>, nedává damage hráčům!");
        }
        else
        {
            args.Player.Toggles.HasUnlimitedAmmo = false;
        }
    }

    private static void OnHurt(PlayerHurtingEventArgs args)
    {
        if (args.Attacker?.ReferenceHub != null
            && args.DamageHandler is FirearmDamageHandler firearmDamageHandler
            && collectorWeapons.Contains(firearmDamageHandler.Firearm.ItemSerial))
            args.IsAllowed = false;
    }
    
    private static void OnShooting(PlayerShootingFirearmEventArgs args)
    {
        if (collectorWeapons.Contains(args.Firearm.ItemSerial) && args.TargetPlayer != null)
        {
            args.IsAllowed = false;
            args.Player.SendAlert(AlertType.Info, 5f, "Collector Weapon", "<color=red>S touto zbraní lze pouze sbírat dýně!</color>", true);
        }
    }

    private static void OnRoundStarted()
    {
        ApiLog.Info("LanternSpawner", $"Spawning &6{LanternsPerRound}&r lantern(s)");

        var count = 0;
        
        for (var x = 0; x < LanternsPerRound; x++)
        {
            if (TrySpawnLantern(out _))
            {
                count++;
            }
        }
        
        ApiLog.Info("LanternSpawner", $"Spawned &6{count}&r lantern(s)!");
    }

    private static void OnRoundWaiting()
    {
        collectorWeapons.Clear();
        occupiedCoors.Clear();
        occupiedRooms.Clear();
    }

    private static void Initialize()
    {
        ExRoundEvents.Started += OnRoundStarted;
        ExRoundEvents.WaitingForPlayers += OnRoundWaiting;

        ExPlayerEvents.SelectedItem += OnSelected;
        ExPlayerEvents.ShootingFirearm += OnShooting;

        PlayerEvents.Hurting += OnHurt;
        PlayerEvents.ChangedRole += OnChangedRole;
    }
}