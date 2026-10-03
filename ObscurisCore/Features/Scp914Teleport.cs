using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;

using LabApi.Features.Wrappers;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Extensions;
using LabExtended.Utilities;

using MapGeneration;

using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Audio.Clips;

using PlayerRoles;

using Scp914;

using UnityEngine;

namespace ObscurisCore.Features;

/// <summary>
/// Provides functionality related to SCP-914 teleportation mechanics, including player and SCP teleportation
/// chances, configuration of zones, and associated audio management.
/// </summary>
public static class Scp914Teleport
{
    /// <summary>
    /// Gets or sets the probability that SCP-914 will teleport players during operation.
    /// </summary>
    [Config("scp914Teleport", "chance", "Chance that SCP-914 will teleport players during operation.")]
    public static float PlayerChance { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the chance that a player will be teleported to an SCP during SCP-914 teleportation.    
    /// </summary>
    [Config("scp914Teleport", "scpChance",
        "Chance that a player will be teleported to an SCP during SCP-914 teleportation.")]
    public static float ScpChance { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the knob setting that enables SCP-914 to teleport players.
    /// </summary>
    [Config("scp914Teleport", "setting", "Knob setting that enables SCP-914 to teleport players.")]
    public static Scp914KnobSetting Setting { get; set; } = Scp914KnobSetting.Coarse;

    /// <summary>
    /// Gets or sets the mapping of facility zones to the minimum round duration, in seconds, required for SCP-914
    /// teleportation to be enabled in each zone.
    /// </summary>
    /// <remarks>Modify this dictionary to control which facility zones are eligible for SCP-914
    /// teleportation based on the elapsed round time. Zones become eligible when the round duration meets or
    /// exceeds the specified value.</remarks>
    [Config("scp914Teleport", "zones",
        "Mapping of facility zones to the minimum round duration, in seconds, required for SCP-914 teleportation to be enabled in each zone.")]
    public static Dictionary<FacilityZone, float> Zones { get; set; } = new()
    {
        { FacilityZone.Surface, 300f },
        { FacilityZone.Entrance, 120f },
        { FacilityZone.HeavyContainment, 60f },
        { FacilityZone.LightContainment, 0f },
    };

    /// <summary>
    /// Gets or sets the audio clips used by SCP-914 during teleportation.
    /// </summary>
    [Config("scp914Teleport", "clips", "Audio clips used by SCP-914 during teleportation.")]
    public static ClipConfig<string> ClipsConfig { get; set; } = new();

    /// <summary>
    /// Gets the audio clip manager used by SCP-914 teleportation.
    /// </summary>
    public static ClipManager<string> Clips { get; private set; }

    /// <summary>
    /// Gets the identifier for the LCZ-914 room, if available.
    /// </summary>
    /// <remarks>This property is set internally and may be null if the LCZ-914 room is not present or
    /// has not been initialized. Use this property to reference the LCZ-914 room in scenarios where its presence is
    /// required.</remarks>
    public static RoomIdentifier? Lcz914Room { get; private set; }

    /// <summary>
    /// Determines whether any SCP team player is currently in the LCZ 914 room or in a room directly connected to
    /// it.
    /// </summary>
    /// <remarks>This method checks all players on the SCP team and considers both direct presence in
    /// the LCZ 914 room and presence in any room connected to it.</remarks>
    /// <returns>true if at least one SCP team player is in LCZ 914 or a connected room; otherwise, false.</returns>
    public static bool AnyScpIn914OrNear()
    {
        for (var x = 0; x < ExPlayer.Players.Count; x++)
        {
            var player = ExPlayer.Players[x];

            if (player?.ReferenceHub == null)
                continue;

            if (player.Role.Team != Team.SCPs)
                continue;

            if (player.Position.Room != null)
            {
                if (player.Position.Room.Name != RoomName.Lcz914)
                {
                    if (Lcz914Room != null)
                    {
                        if (Lcz914Room.ConnectedRooms.Any(r => r != null && player.Position.Room == r))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (Lcz914Room != null && Lcz914Room.WorldspaceBounds.Contains(player.Position.Position))
            {
                return true;
            }
        }

        return false;
    }

    private static void OnScp914UpgradedPlayer(Scp914ProcessedPlayerEventArgs args)
    {
        if (args.Player is not ExPlayer player || player.Role.IsScp)
            return;

        if (PlayerChance <= 0f)
            return;

        if (Zones?.Count < 1)
            return;

        if (args.KnobSetting != Setting)
            return;

        if (PlayerChance < 100f && !WeightUtils.GetBool(PlayerChance))
            return;

        var zones = Zones
            .Where(p => p.Value <= 0f || ExRound.Duration.TotalSeconds >= p.Value)
            .Select(p => p.Key)
            .ToArray();

        if (zones?.Length < 1)
            return;

        if (!AnyScpIn914OrNear())
            return;

        Clips ??= new(ClipsConfig, Vector3.zero);

        if (ScpChance > 0f &&
            WeightUtils.GetBool(ScpChance))
        {
            var randomScp = ExPlayer.Players
                .Where(p => p?.ReferenceHub != null && p.Role.Team == Team.SCPs)
                .GetRandomItem();

            if (randomScp?.ReferenceHub != null)
            {
                player.Position.Position = randomScp.PositionAdjustY(0.1f);

                Clips.PlayRandomClip("OnScpTeleported", player.Position);
                return;
            }
        }

        if (player.RandomRoomTeleport(zones))
            Clips.PlayRandomClip("OnTeleported", player.Position);
    }

    private static void OnMapGenerated(MapGeneratedEventArgs args)
    {
        Lcz914Room = Map.Rooms.FirstOrDefault(r => r.Name == RoomName.Lcz914)?.Base;

        if (Lcz914Room == null)
            ApiLog.Error("Scp914Teleport", "Could not find Lcz914.");
        else
            ApiLog.Debug("Scp914Teleport",
                $"LCZ-914 room found at &1{Lcz914Room.WorldspaceBounds.center.ToPreciseString()}&r.");
    }

    private static void Initialize()
    {
        ServerEvents.MapGenerated += OnMapGenerated;
        Scp914Events.ProcessedPlayer += OnScp914UpgradedPlayer;
    }
}