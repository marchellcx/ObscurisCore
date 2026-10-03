using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.API.Custom.Teams;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;
using LabExtended.Extensions;

using MapGeneration;

using ObscurisCore.Features.Loadouts;

using PlayerRoles;

using ProjectMER.Features;
using ProjectMER.Features.Objects;

using UnityEngine;

using NiveraAPI.IO.Configs;

namespace ObscurisCore.Features.Custom.Teams.SerpentsHand;

/// <summary>
/// Serpents Hand team handler.
/// </summary>
public class SerpentsHandTeam : CustomTeamHandler<SerpentsHandWave>
{
    /// <summary>
    /// Gets a value indicating whether or not the Serpent's Hand wave is enabled.
    /// </summary>
    [Config("serpents-hand", "enabled", "Whether or not the Serpent's Hand wave is enabled.")]
    public static bool Enabled { get; set; }

    /// <summary>
    /// Gets the maximum amount of players in a wave.
    /// </summary>
    [Config("serpents-hand", "max-players", "The maximum amount of players in a Serpent's Hand wave.")]
    public static int MaxPlayers { get; set; }

    /// <summary>
    /// Gets the minimum amount of players in a wave.
    /// </summary>
    [Config("serpents-hand", "min-players", "The minimum amount of players in a Serpent's Hand wave.")]
    public static int MinPlayers { get; set; }

    /// <summary>
    /// Gets the name of the hole schematic.
    /// </summary>
    [Config("serpents-hand", "hole-schematic", "The name of the hole schematic to spawn.")]
    public static string HoleSchematicName { get; set; } = "SerpentsHandHole";

    /// <summary>
    /// Gets the name of the hole position.
    /// </summary>
    [Config("serpents-hand", "hole-position", "The name of the hole position to spawn.")]
    public static string HolePositionName { get; set; } = "SerpentsHandHolePosition";

    /// <summary>
    /// Gets the name of the spawn position.
    /// </summary>
    [Config("serpents-hand", "spawn-position", "The name of the spawn position to spawn.")]
    public static string SpawnPositionName { get; set; } = "SerpentsHandSpawnPosition";

    /// <summary>
    /// Gets the CASSIE announcement.
    /// </summary>
    [Config("serpents-hand", "cassie-message", "Whether or not to play the CASSIE announcement.")]
    public static bool CassieMessage { get; set; } = true;

    /// <summary>
    /// Gets the current spawn position.
    /// </summary>
    public static Vector3 SpawnPosition { get; private set; }

    /// <summary>
    /// Gets the spawned hole.
    /// </summary>
    public static SchematicObject? HoleObject { get; private set; }

    /// <summary>
    /// Whether or not a wave was spawned this round.
    /// </summary>
    public static bool WasSpawned { get; private set; }

    /// <summary>
    /// Gets the unique identifier for this team.
    /// </summary>
    public override string Id { get; } = "serpents-hand";

    /// <inheritdoc cref="CustomTeamHandler.Name"/>
    public override string Name { get; set; } = "Serpent's Hand";

    /// <inheritdoc cref="CustomTeamHandler.IsSpawnable"/>
    public override bool IsSpawnable(ExPlayer player)
        => player.CanBeRespawned;

    /// <inheritdoc cref="CustomTeamHandler.SelectRole"/>
    public override RoleTypeId SelectRole(ExPlayer player, Dictionary<ExPlayer, RoleTypeId> selectedRoles)
        => RoleTypeId.Tutorial;

    /// <inheritdoc cref="CustomTeamHandler.OnRegistered"/>
    public override void OnRegistered()
    {
        base.OnRegistered();

        if (Enabled)
        {
            PlayerEvents.Death += OnDied;
            PlayerEvents.ChangedRole += OnChangedRole;

            ExRoundEvents.Started += OnStarted;
        }

        LoadoutManager.Ensure("SerpentsHand", new LoadoutDefinition()
            .WithAmmo(ItemType.Ammo556x45, 120)
            .WithItems(ItemType.GunE11SR, ItemType.GrenadeHE, ItemType.KeycardChaosInsurgency, ItemType.Adrenaline, ItemType.SCP500, ItemType.SCP1344, ItemType.ArmorHeavy));
    }

    private void OnStarted()
    {
        WasSpawned = false;
        HoleObject = null;

        if (!MapLocations.TryFindPrefixed(HolePositionName, out Vector3 holePos, out Quaternion _))
        {
            ApiLog.Warn("Serpent's Hand", "Failed to find a suitable hole position!");
            return;
        }

        if (!MapLocations.TryFindPrefixed(SpawnPositionName, out Vector3 spawnPos, out Quaternion _))
        {
            ApiLog.Warn("Serpent's Hand", "Failed to find a suitable spawn point!");
            return;
        }

        SpawnPosition = spawnPos;

        if (!ObjectSpawner.TrySpawnSchematic(HoleSchematicName, holePos, out var spawnedHole))
        {
            ApiLog.Warn("Serpent's Hand", "Could not spawn the hole schematic!");
            return;
        }

        HoleObject = spawnedHole;
    }

    private void OnDied(PlayerDeathEventArgs args)
    {
        if (WasSpawned)
            return;

        if (!ExRound.IsRunning)
            return;

        if (!args.OldRole.IsScp(false))
            return;

        if (SpawnPosition.GetZone() is not FacilityZone.Surface && Warhead.IsDetonated)
            return;

        if (SpawnPosition.GetZone() is FacilityZone.LightContainment && Decontamination.IsDecontaminating)
            return;

        TimingUtils.AfterSeconds(() =>
        {
            WasSpawned = Spawn(MinPlayers, MaxPlayers, player => player != args.Player).SpawnedWave != null;
        }, 0.2f);
    }

    private void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        if (!string.IsNullOrEmpty(args.Player.CustomInfo) && args.Player.CustomInfo == Name)
        {
            args.Player.CustomInfo = string.Empty;
            args.Player.InfoArea &= ~PlayerInfoArea.CustomInfo;
        }
    }

    private static void Initialize()
    {
        CustomTeamRegistry.Register<SerpentsHandTeam>();
    }
}