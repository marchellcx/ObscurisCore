using System.ComponentModel;

using CustomPlayerEffects;

using Interactables.Interobjects.DoorUtils;

using LabApi.Features.Wrappers;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;
using LabExtended.Extensions;

using MapGeneration;

using NorthwoodLib.Pools;

using PlayerRoles;

using ObscurisCore.Extensions;

using UnityEngine;

using CheckpointDoor = Interactables.Interobjects.CheckpointDoor;

namespace ObscurisCore.Features.Coin.Actions;

/// <summary>
/// Represents an action that can be performed by flipping a coin to teleport the player.
/// </summary>
public class TeleportActions : CoinAction
{
    /// <summary>
    /// The position of the escape exit.
    /// </summary>
    public static Vector3 EscapeExitPosition = new(136.3f, 296f, -23.2f);
    
    /// <summary>
    /// The type of teleportation to perform.
    /// </summary>
    public enum TeleportType
    {
        /// <summary>
        /// Teleports the player to a random SCP.
        /// </summary>
        ToScp,
        
        /// <summary>
        /// Teleports the player to their spawn.
        /// </summary>
        ToSpawn,
        
        /// <summary>
        /// Teleports the player to a random gate.
        /// </summary>
        ToGate,
        
        /// <summary>
        /// Teleports the player to a random location.
        /// </summary>
        ToRandom,
        
        /// <summary>
        /// Teleports the player to the pocket dimension.
        /// </summary>
        ToPocket,
        
        /// <summary>
        /// Teleports the player to the escape zone.
        /// </summary>
        ToEscape,
        
        /// <summary>
        /// Teleports the player to a random checkpoint.
        /// </summary>
        ToCheckpoint,
    }

    /// <summary>
    /// The weights of the teleport actions.
    /// </summary>
    [Description("Sets the weights of the teleport actions.")]
    public Dictionary<TeleportType, float> Weights { get; set; } = new()
    {
        { TeleportType.ToScp, 0f },
        { TeleportType.ToSpawn, 0f },
        { TeleportType.ToGate, 0f },
        { TeleportType.ToRandom, 0f },
        { TeleportType.ToPocket, 0f },
        { TeleportType.ToEscape, 0f },
        { TeleportType.ToCheckpoint, 0f },
    };

    /// <summary>
    /// The messages to be sent to players when they perform the actions.
    /// </summary>
    [Description("Sets the messages to be sent to players when they perform the actions.")]
    public Dictionary<TeleportType, string> Messages { get; set; } = new()
    {
        { TeleportType.ToScp, "" },
        { TeleportType.ToSpawn, "" },
        { TeleportType.ToGate, "" },
        { TeleportType.ToRandom, "" },
        { TeleportType.ToPocket, "" },
        { TeleportType.ToEscape, "" },
        { TeleportType.ToCheckpoint, "" },
    };

    /// <summary>
    /// Whether the actions can be combined with other actions.
    /// </summary>
    public override bool CanBeCombined => true;
    
    private Dictionary<ExPlayer, TeleportType> actions = new();

    /// <summary>
    /// Enables the teleport action.
    /// </summary>
    public override void Enable()
    {
        base.Enable();
        
        ExPlayerEvents.Left += player => actions.Remove(player);
        ExRoundEvents.WaitingForPlayers += actions.Clear;
    }

    /// <summary>
    /// Determines if the teleport action is available for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the availability of the teleport action is being checked.</param>
    /// <returns>Returns true if the teleport action is available for the player; otherwise, false.</returns>
    public override bool IsAvailable(ExPlayer player)
    {
        if (!base.IsAvailable(player))
            return false;
        
        var list = ListPool<TeleportType>.Shared.Rent();

        foreach (var kvp in Weights)
        {
            if (kvp.Value <= 0f)
                continue;

            var available = false;

            switch (kvp.Key)
            {
                case TeleportType.ToScp:
                    available = ExPlayer.Players.Any(p => p != player && p.IsSCP);
                    break;
                
                case TeleportType.ToSpawn:
                    if (player.Role.Type is RoleTypeId.ClassD or RoleTypeId.Scientist)
                        available = !Warhead.IsDetonated && !Decontamination.IsDecontaminating;
                    else if (player.Role.Type is RoleTypeId.FacilityGuard)
                        available = !Warhead.IsDetonated;
                    else
                        available = true;

                    break;
                
                case TeleportType.ToGate:
                    available = RoomIdentifier.AllRoomIdentifiers.Any(ri => ri != null
                                                                            && DoorVariant.DoorsByRoom.TryGetValue(ri,
                                                                                out var doors)
                                                                            && doors.Any(d =>
                                                                                (d.DoorName ?? d.name)
                                                                                .Contains("GATE")));
                    break;
                
                case TeleportType.ToRandom:
                    available = RoomIdentifier.AllRoomIdentifiers.Any(ri => ri != null
                                                                            && DoorVariant.DoorsByRoom.TryGetValue(ri,
                                                                                out var doors)
                                                                            && doors.Any(d =>
                                                                                d.IsConsideredOpen() ||
                                                                                d.RequiredPermissions.CheckPermissions(
                                                                                    player.ReferenceHub, d, out _)));
                    break;
                
                case TeleportType.ToEscape:
                    available = DoorVariant.AllDoors
                        .Any(d => d != null && (d.DoorName ?? d.name).Contains("ESCAPE") && d.Rooms?.Length > 0);
                    break;
                
                case TeleportType.ToCheckpoint:
                    available = DoorVariant.AllDoors
                        .Any(d => d != null && d is CheckpointDoor && d.Rooms?.Length > 0);
                    break;
                
                default:
                    available = true;
                    break;
            }
            
            if (!available)
                continue;
            
            list.Add(kvp.Key);
        }

        if (list.Count > 0)
        {
            actions[player] = list.GetRandomWeighted(act => Weights[act]);
            
            ListPool<TeleportType>.Shared.Return(list);
            return true;
        }
        
        ListPool<TeleportType>.Shared.Return(list);
        return false;
    }

    /// <summary>
    /// Executes the teleport action for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the teleport action is executed.</param>
    public override void Execute(ExPlayer player)
    {
        base.Execute(player);

        if (!actions.TryGetValue(player, out var type))
        {
            ApiLog.Warn("CoinManager", $"Could not find action for player {player.ToLogString()}!");
            return;
        }
        
        actions.Remove(player);
        
        player.SendFormattedAlert(type, Messages, false);

        switch (type)
        {
            case TeleportType.ToScp:
                player.Position.Position = ExPlayer.Players.GetRandomItem(p => p != player && p.IsSCP).PositionAdjustY(0.5f);
                break;
            
            case TeleportType.ToSpawn:
                player.RandomSpawnPositionTeleport([player.Role.Team]);
                break;
            
            case TeleportType.ToGate:
                player.Position.Position = RoomIdentifier.AllRoomIdentifiers.GetRandomItem(ri => ri != null
                                                && DoorVariant.DoorsByRoom.TryGetValue(ri, out var doors)
                                                && doors.Any(d => (d.DoorName ?? d.name).Contains("GATE")))
                                                                            .GetSafePosition(player).AdjustY(0.5f);
                break;
            
            case TeleportType.ToRandom:
                player.Position.Position = RoomIdentifier.AllRoomIdentifiers.GetRandomItem(ri => ri != null
                                                && DoorVariant.DoorsByRoom.TryGetValue(ri, out var doors)
                                                && doors.Any(d => d.IsConsideredOpen() || d.RequiredPermissions.CheckPermissions(player.ReferenceHub, d, out _)))
                                                                            .GetSafePosition(player).AdjustY(0.5f);
                break;
            
            case TeleportType.ToPocket:
                player.Effects.EnableEffect<PocketCorroding>(1);
                break;
            
            case TeleportType.ToEscape:
                player.Position.Position = EscapeExitPosition;
                break;
            
            case TeleportType.ToCheckpoint:
                player.Position.Position = DoorVariant.AllDoors
                    .GetRandomItem(d => d != null && d is CheckpointDoor)
                    .Rooms.RandomItem().GetSafePosition(player).AdjustY(0.5f);
                break;
        }
    }
}