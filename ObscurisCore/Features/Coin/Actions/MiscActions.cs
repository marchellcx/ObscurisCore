using System.ComponentModel;

using Cassie;

using Interactables.Interobjects.DoorUtils;

using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;

using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.API.Containers;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Extensions;
using LabExtended.Utilities;

using MapGeneration;

using MEC;

using NorthwoodLib.Pools;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Configs;
using PlayerRoles;

using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.Coin.Actions;

/// <summary>
/// Miscellaneous actions that can be performed by the coin.
/// </summary>
public class MiscActions : CoinAction
{
    /// <summary>
    /// The type of action to perform.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Inverts the controls of the player.
        /// </summary>
        InvertControls,
        
        /// <summary>
        /// Restores the controls of the player.
        /// </summary>
        RestoreControls,
        
        /// <summary>
        /// Applies a random gravity to the player.
        /// </summary>
        RandomGravity,
        
        /// <summary>
        /// Resets the gravity of the player.
        /// </summary>
        ResetGravity,
        
        /// <summary>
        /// Spawns multiple instances of SCP-018.
        /// </summary>
        BouncyBalls,
        
        /// <summary>
        /// Heals the player by a percentage.
        /// </summary>
        HealPercent,
        
        /// <summary>
        /// Switches the spawn positions of NTF and CI.
        /// </summary>
        SwitchSpawns,
        
        /// <summary>
        /// Switches the roles of NTF and CI.
        /// </summary>
        SwitchSides,
        
        /// <summary>
        /// Detonates the Alpha Warhead.
        /// </summary>
        DetonateWarhead,
        
        /// <summary>
        /// Spawns a grenade in every room.
        /// </summary>
        AtomicBomb,
        
        /// <summary>
        /// Locks the room the player is currently in.
        /// </summary>
        RoomLock,
        
        /// <summary>
        /// Blackouts the zone the player is currently in.
        /// </summary>
        ZoneBlackout,
        
        /// <summary>
        /// Unlocks a random door lock.
        /// </summary>
        RandomUnlock,
    }

    /// <summary>
    /// Whether the actions can be combined.
    /// </summary>
    public override bool CanBeCombined => true;

    /// <summary>
    /// The weights of the actions.
    /// </summary>
    [Description("Sets the weights of the actions.")]
    public Dictionary<ActionType, float> Weights { get; set; } = new()
    {
        { ActionType.InvertControls, 0f },
        { ActionType.RestoreControls, 0f },
        { ActionType.RandomGravity, 0f },
        { ActionType.ResetGravity, 0f },
        { ActionType.BouncyBalls, 0f },
        { ActionType.HealPercent, 0f },
        { ActionType.SwitchSpawns, 0f },
        { ActionType.SwitchSides, 0f },
        { ActionType.DetonateWarhead, 0f },
        { ActionType.AtomicBomb, 0f },
        { ActionType.RoomLock, 0f },
        { ActionType.ZoneBlackout, 0f },
        { ActionType.RandomUnlock, 0f },
    };
    
    /// <summary>
    /// The messages to be sent to players when they perform the actions.
    /// </summary>
    [Description("Sets the messages to be sent to players when they perform the actions.")]
    public Dictionary<ActionType, string> Messages { get; set; } = new()
    {
        { ActionType.InvertControls, "" },
        { ActionType.RestoreControls, "" },
        { ActionType.RandomGravity, "" },
        { ActionType.ResetGravity, "" },
        { ActionType.BouncyBalls, "" },
        { ActionType.HealPercent, "" },
        { ActionType.SwitchSpawns, "" },
        { ActionType.SwitchSides, "" },
        { ActionType.DetonateWarhead, "" },
        { ActionType.AtomicBomb, "" },
        { ActionType.RoomLock, "" },
        { ActionType.ZoneBlackout, "" },
        { ActionType.RandomUnlock, "" },
    };

    /// <summary>
    /// Indicates whether the CASSIE message for the bouncy balls action should be played to the target player.
    /// </summary>
    [Description("Whether or not to play the CASSIE bouncy balls message to the target player.")]
    public bool BouncyBallsCassie { get; set; } = true;
    
    /// <summary>
    /// The amount of SCP-018 to spawn.
    /// </summary>
    [Description("The amount of SCP-018 to spawn.")]
    public Int32Range BouncyBallsAmount { get; set; } = new() { MinValue = 1, MaxValue = 5 };

    /// <summary>
    /// The percentage of health to be restored to the player.
    /// </summary>
    [Description("The percentage of health to be restored to the player.")]
    public Int32Range HealPercent { get; set; } = new() { MinValue = 1, MaxValue = 100 };

    /// <summary>
    /// The range of gravity to be applied to the player.
    /// </summary>
    [Description("The range of gravity to be applied to the player.")]
    public VectorRange GravityRange { get; set; } = new();
    
    /// <summary>
    /// The message to be sent to players when they switch spawns (CI/NTF).
    /// </summary>
    [Description("The message to be sent to players when they switch spawns (CI/NTF).")]
    public string SwitchSpawnsMessage { get; set; } = "";

    /// <summary>
    /// The message to be sent to players when they switch sides.
    /// </summary>
    [Description("The message to be sent to players when they switch sides.")]
    public string SwitchSidesMessage { get; set; } = "";
    
    /// <summary>
    /// The message to be sent to players when the warhead starts the detonation sequence.
    /// </summary>
    [Description("The message to be sent to players when the warhead starts the detonation sequence.")]
    public string DetonateWarheadMessage { get; set; } = "";

    /// <summary>
    /// The amount of grenades to spawn in every room.
    /// </summary>
    [Description("The amount of grenades to spawn in every room.")]
    public Int32Range AtomicBombGrenadeAmount { get; set; } = new() { MinValue = 1, MaxValue = 5 };

    /// <summary>
    /// The fuse time, in seconds, for grenades spawned as part of the atomic bomb action.
    /// </summary>
    [Description("The fuse time of grenades spawned in the atomic bomb.")]
    public float AtomicBombFuseTime { get; set; } = 3f;

    /// <summary>
    /// The message to send to players when the atomic bomb action is triggered.
    /// </summary>
    [Description("The message to send to other players when the atomic bomb is detonated.")]
    public string AtomicBombMessage { get; set; }

    /// <summary>
    /// The amount of seconds to wait before the room lock can be unlocked.
    /// </summary>
    [Description("The amount of seconds to wait before the room lock can be unlocked.")]
    public float RoomLockUnlockDelay { get; set; } = 20f;
    
    /// <summary>
    /// The duration of the zone blackout, in seconds. Set to 0 to disable.
    /// </summary>
    [Description("The duration of the zone blackout, in seconds. Set to 0 to disable.")]
    public float ZoneBlackoutDuration { get; set; } = 10f;
    
    /// <summary>
    /// The message to send to players when a random door lock is unlocked (supported variables are $Zone, $Room, $Door, $Distance).
    /// </summary>
    [Description("The message to send to players when a random door lock is unlocked (supported variables are $Zone, $Room, $Door, $Distance).")]
    public string RandomUnlockMessage { get; set; } = "";

    /// <summary>
    /// The amount of seconds to wait before the gravity is reset. Set to 0 to disable.
    /// </summary>
    [Description("The amount of seconds to wait before the gravity is reset. Set to 0 to disable.")]
    public float GravityResetDelay { get; set; } = 10f;
    
    /// <summary>
    /// The amount of seconds to wait before the controls are reset. Set to 0 to disable.
    /// </summary>
    [Description("The amount of seconds to wait before the controls are reset. Set to 0 to disable.")]
    public float ControlsResetDelay { get; set; } = 10f;
    
    private bool spawnsSwitched;
    private Dictionary<ExPlayer, ActionType> actions = new();

    /// <summary>
    /// Enables the action.
    /// </summary>
    public override void Enable()
    {
        base.Enable();
        
        ExPlayerEvents.Left += player => actions.Remove(player);
        
        ExRoundEvents.WaitingForPlayers += () =>
        {  
            actions.Clear();
            
            spawnsSwitched = false;
        };
        
        ServerEvents.WaveRespawned += OnRespawned;
    }

    /// <summary>
    /// Determines whether the action is available for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the action availability is being checked.</param>
    /// <returns>A boolean value indicating whether the action is available for the given player.</returns>
    public override bool IsAvailable(ExPlayer player)
    {
        if (!base.IsAvailable(player))
            return false;
        
        actions.Remove(player);
        
        var list = ListPool<ActionType>.Shared.Rent();

        foreach (var kvp in Weights)
        {
            if (kvp.Value <= 0f)
                continue;
            
            var available = false;

            switch (kvp.Key)
            {
                case ActionType.InvertControls:
                    available = player.Scale == Vector3.one;
                    break;
                
                case ActionType.RestoreControls:
                    available = Mathf.Approximately(player.Scale.z, -1f);
                    break;
                
                case ActionType.RandomGravity:
                    available = player.Gravity == PositionContainer.DefaultGravity;
                    break;
                
                case ActionType.ResetGravity:
                    available = player.Gravity != PositionContainer.DefaultGravity;
                    break;
                
                case ActionType.SwitchSpawns:
                    available = !spawnsSwitched;
                    break;
                
                case ActionType.SwitchSides:
                    available = ExPlayer.Players.Any(p => p.Role.Team is Team.ChaosInsurgency)
                                && ExPlayer.Players.Any(p => p.Role.Team is Team.FoundationForces && p.Role.Type is not RoleTypeId.FacilityGuard);
                    break;
                
                case ActionType.HealPercent:
                    available = player.GetHealthPercent() < (100 - HealPercent.MinValue);
                    break;
                
                case ActionType.DetonateWarhead:
                    available = !Warhead.IsDetonated && !Warhead.IsDetonationInProgress;
                    break;
                
                case ActionType.RoomLock:
                    available = player.Position.Room != null && DoorVariant.DoorsByRoom.ContainsKey(player.Position.Room);
                    break;
                
                case ActionType.ZoneBlackout:
                    available = player.Position.Room != null && ZoneBlackoutDuration > 0f &&
                                player.Position.Room.Zone is FacilityZone.Entrance 
                                    or FacilityZone.HeavyContainment
                                    or FacilityZone.LightContainment;
                    break;
                
                case ActionType.RandomUnlock:
                    available = DoorVariant.AllDoors.Any(d => d.PermissionsPolicy.RequiredPermissions != DoorPermissionFlags.None 
                                                              && !d.PermissionsPolicy.RequiredPermissions.HasFlag(DoorPermissionFlags.ScpOverride));
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
            
            ListPool<ActionType>.Shared.Return(list);
            return true;
        }
        
        ListPool<ActionType>.Shared.Return(list);
        return false;
    }

    /// <summary>
    /// Determines whether the action should be forced based on the player's current gravity condition.
    /// </summary>
    /// <param name="player">The player whose gravity condition is being checked.</param>
    /// <returns>Returns true if the player's gravity is not set to the default value; otherwise, false.</returns>
    public override bool ShouldForce(ExPlayer player)
    {
        if (player.Gravity != PositionContainer.DefaultGravity)
        {
            actions[player] = ActionType.ResetGravity;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes the action associated with the coin for the specified player.
    /// </summary>
    /// <param name="player">The player on whom the action will be applied.</param>
    public override void Execute(ExPlayer player)
    {
        if (!actions.TryGetValue(player, out var action))
        {
            ApiLog.Warn("CoinManager", $"Could not find action for player {player.ToLogString()}!");
            return;
        }
        
        actions.Remove(player);

        if (action != ActionType.RandomUnlock)
            player.SendFormattedAlert(action, Messages, false, AlertType.Info, 5f, "Coin Manager");
        
        switch (action)
        {
            case ActionType.InvertControls:
                player.Scale = new(1f, 1f, -1f);
                
                if (ControlsResetDelay > 0f)
                    Timing.CallDelayed(ControlsResetDelay, () => player.Scale = new(1f, 1f, 1f));
                
                break;
            
            case ActionType.RestoreControls:
                player.Scale = Vector3.one;
                break;
            
            case ActionType.RandomGravity:
                player.Gravity = GravityRange.GetRandom();

                if (GravityResetDelay > 0f)
                    Timing.CallDelayed(GravityResetDelay, () => player.Gravity = PositionContainer.DefaultGravity);
                
                break;
            
            case ActionType.ResetGravity:
                player.Gravity = PositionContainer.DefaultGravity;
                break;
            
            case ActionType.HealPercent:
                player.Health = Mathf.Min(player.MaxHealth, player.GetHealthAmount(HealPercent.GetRandom()));
                break;
            
            case ActionType.SwitchSpawns:
                spawnsSwitched = true;
                break;

            case ActionType.RoomLock:
            {
                if (player.Position.Room != null && DoorVariant.DoorsByRoom.TryGetValue(player.Position.Room, out var doors))
                {
                    foreach (var door in doors)
                    {
                        door.NetworkTargetState = false;
                        door.ServerChangeLock(DoorLockReason.AdminCommand, true);

                        if (RoomLockUnlockDelay > 0f)
                        {
                            Timing.CallDelayed(RoomLockUnlockDelay, () =>
                            {
                                if (door == null) // The round may end before this interval which would result in an NRE
                                    return;
                                
                                door.ServerChangeLock(DoorLockReason.AdminCommand, false);
                            });
                        }
                    }
                }

                break;
            }

            case ActionType.ZoneBlackout:
            {
                if (player.Position.Room != null)
                {
                    foreach (var room in RoomIdentifier.AllRoomIdentifiers)
                    {
                        if (room == null)
                            continue;
                        
                        if (room.Zone != player.Position.Room.Zone)
                            continue;

                        foreach (var light in room.LightControllers)
                        {
                            if (light == null)
                                continue;
                            
                            light.ServerFlickerLights(ZoneBlackoutDuration);
                        }
                    }
                }

                break;
            }

            case ActionType.RandomUnlock:
            {
                var door = DoorVariant.AllDoors.GetRandomItem(d => 
                    d.PermissionsPolicy.RequiredPermissions != DoorPermissionFlags.None
                    && !d.PermissionsPolicy.RequiredPermissions.HasFlag(DoorPermissionFlags.ScpOverride));

                if (door == null)
                    return;
                
                door.NetworkTargetState = true;
                door.ServerChangeLock(DoorLockReason.AdminCommand, true);

                var zoneStr = door.Rooms[0].Zone.ToString().SpaceByUpperCase().Trim();
                var roomStr = door.Rooms[0].Name.ToString().SpaceByUpperCase().Trim();
                var doorStr = (door.DoorName ?? door.name).SpaceByUpperCase().Trim();
                var distanceStr = Mathf.CeilToInt(Vector3.Distance(player.Position, door.transform.position)) .ToString();
                
                player.SendFormattedAlert(RandomUnlockMessage
                    .Replace("$Zone", zoneStr)
                    .Replace("$Room", roomStr)
                    .Replace("$Door", doorStr)
                    .Replace("$Distance", distanceStr), true, AlertType.Info, 5f, "Coin Manager");
                break;
            }

            case ActionType.DetonateWarhead:
            {
                ExPlayer.Players.ForEach(p =>
                {
                    if (p != player)
                    {
                        p.SendFormattedAlert(DetonateWarheadMessage, true, AlertType.Info, 5f, "Coin Manager");
                    }
                });
                
                Warhead.Start(true, false, player);
                break;
            }

            case ActionType.BouncyBalls:
            {
                var amount = BouncyBallsAmount.GetRandom();

                for (var x = 0; x < amount; x++)
                    ExMap.SpawnProjectile(ItemType.SCP018, player.Position, Vector3.one, player.Velocity,
                        player.Rotation, 3f, 10f);
                
                if (BouncyBallsCassie)
                    player.Send(new CassieTtsPayload("XMAS_BOUNCYBALLS"));

                break;
            }

            case ActionType.AtomicBomb:
            {
                ExPlayer.Players.ForEach(p =>
                {
                    if (p != player)
                    {
                        p.SendFormattedAlert(AtomicBombMessage, true, AlertType.Info, 5f, "Coin Manager");
                    }
                });
                
                var amount = AtomicBombGrenadeAmount.GetRandom();

                foreach (var ri in RoomIdentifier.AllRoomIdentifiers)
                {
                    if (ri == null)
                        continue;

                    for (var x = 0; x < amount; x++)
                        ExMap.SpawnProjectile(ItemType.GrenadeHE, ri.GetSafePosition(player), Vector3.one, Vector3.zero,
                            player.Rotation, 0f, AtomicBombFuseTime);
                }

                break;
            }

            case ActionType.SwitchSides:
            {
                var ciPlayers = ExPlayer.Players.Where(p => p.Role.Team is Team.ChaosInsurgency).ToList();
                var ntfPlayers = ExPlayer.Players.Where(p => p.Role.Team is Team.FoundationForces && p.Role.Type is not RoleTypeId.FacilityGuard).ToList();
                
                var captainCount = ntfPlayers.Count(p => p.Role.Type is RoleTypeId.NtfCaptain);
                var sergeantCount = ntfPlayers.Count(p => p.Role.Type is RoleTypeId.NtfSergeant);
                
                ciPlayers.ForEach(p =>
                {
                    p.SendFormattedAlert(SwitchSidesMessage, true, AlertType.Info, 5f, "Coin Manager");
                    
                    if (captainCount > 0)
                    {
                        p.Role.Set(RoleTypeId.NtfCaptain, RoleChangeReason.None, RoleSpawnFlags.None);
                        
                        captainCount--;
                        return;
                    }

                    if (sergeantCount > 0)
                    {
                        p.Role.Set(RoleTypeId.NtfSergeant, RoleChangeReason.None, RoleSpawnFlags.None);
                        
                        sergeantCount--;
                        return;
                    }
                    
                    p.Role.Set(RoleTypeId.NtfPrivate, RoleChangeReason.None, RoleSpawnFlags.None);
                });
                
                ntfPlayers.ForEach(p =>
                {
                    p.SendFormattedAlert(SwitchSidesMessage, true, AlertType.Info, 5f, "Coin Manager");
                    p.Role.Set(Team.ChaosInsurgency.GetRandomRole(), RoleChangeReason.None, RoleSpawnFlags.None);
                });

                break;
            }
        }
    }

    private void OnRespawned(WaveRespawnedEventArgs args)
    {
        if (!spawnsSwitched)
            return;

        if (args.Wave.Faction is Faction.FoundationEnemy)
        {
            args.Players.ForEach(p =>
            {
                if (!p.CastPlayer(out var player))
                    return;
                
                player.SendFormattedAlert(SwitchSpawnsMessage, true, AlertType.Info, 5f, "Coin Manager");
                player.RandomSpawnPositionTeleport([Team.FoundationForces], null,
                    [RoleTypeId.FacilityGuard]);
            });
        }
        else if (args.Wave.Faction is Faction.FoundationStaff)
        {
            args.Players.ForEach(p =>
            {
                if (!p.CastPlayer(out var player))
                    return;
                
                player.SendFormattedAlert(SwitchSpawnsMessage, true, AlertType.Info, 5f, "Coin Manager");
                player.RandomSpawnPositionTeleport([Team.ChaosInsurgency]);
            });
        }
    }
}