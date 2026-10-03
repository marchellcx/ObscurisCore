using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;

using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.Events;
using LabExtended.Utilities;

using Mirror;

using NiveraAPI.Extensions;
using NiveraAPI.IO.Configs;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.Spectating;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Custom.Roles.ChaosSpy;

/// <summary>
/// Manages the chaos spy role.
/// </summary>
public static class ChaosSpyManager
{
    /// <summary>
    /// All active chaos spy instances.
    /// </summary>
    public static List<ExPlayer> Spies { get; } = new();

    #region Configs
    /// <summary>
    /// The weight of spawning a chaos spy.
    /// </summary>
    [Config("chaosSpy", "spawnWeight", "The weight of spawning a chaos spy.")]
    public static float SpawnWeight { get; set; } = 1f;

    /// <summary>
    /// The minimum size of a wave.
    /// </summary>
    [Config("chaosSpy", "minWaveSize", "The minimum size of a wave.")]
    public static int MinWaveSize { get; set; } = 5;
    
    /// <summary>
    /// The message to be displayed when a chaos spy spawns.
    /// </summary>
    [Config("chaosSpy", "spawnMessage", "The message to be displayed when a chaos spy spawns.")]
    public static string SpawnMessage { get; set; }
    
    /// <summary>
    /// The message to be displayed when a spy attacks another spy.
    /// </summary>
    [Config("chaosSpy", "friendlyAttackMessage", "The message to be displayed when a spy attacks another spy.")]
    public static string SpyFriendlyAttackMessage { get; set; }
    
    /// <summary>
    /// The message to be displayed when a spy kills another player.
    /// </summary>
    [Config("chaosSpy", "killedBySpyMessage", "The message to be displayed when a spy kills another player.")]
    public static string KilledBySpyMessage { get; set; }
    
    /// <summary>
    /// The message to be displayed when a friendly player attacks a spy.
    /// </summary>
    [Config("chaosSpy", "attackingSpyMessage", "The message to be displayed when a friendly player attacks a spy.")]
    public static string AttackingSpyMessage { get; set; }
    
    /// <summary>
    /// The message to be displayed when a spy attacks a friendly player.
    /// </summary>
    [Config("chaosSpy", "attackedBySpyMessage", "The message to be displayed when a spy attacks a friendly player.")]
    public static string AttackedBySpyMessage { get; set; }
    
    /// <summary>
    /// The message to be displayed when a spy is found.
    /// </summary>
    [Config("chaosSpy", "foundSpyMessage", "The message to be displayed when a spy is found.")]
    public static string FoundSpyMessage { get; set; }

    /// <summary>
    /// The message to be displayed when a spy is found in Overwatch.
    /// </summary>
    [Config("chaosSpy", "overwatchSpyMessage", "The message to be displayed when a spy is found in Overwatch.")]
    public static string OverwatchSpyMessage { get; set; }
    #endregion

    /// <summary>
    /// Attempts to assign the Chaos Spy role to a specified player.
    /// This method validates the player's eligibility, updates their visibility
    /// as a Chaos Spy, and triggers necessary updates for the Chaos Spy system.
    /// </summary>
    /// <param name="player">The player to be assigned the Chaos Spy role.</param>
    /// <returns>Returns true if the player is successfully assigned the Chaos Spy role; otherwise, false.</returns>
    public static bool SpawnChaosSpy(ExPlayer player)
    {
        if (!player.IsValidPlayer())
            return false;

        if (!player.IsNTF)
            player.Role.Set(RoleTypeId.NtfPrivate, RoleChangeReason.None, RoleSpawnFlags.UseSpawnpoint);
        
        player.SendFormattedAlert(SpawnMessage, true, AlertType.Info, 5f, "Chaos Spy");

        Spies.Add(player);
        return true;
    }

    private static void OnSpectating(PlayerChangedSpectatorEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (!args.NewTarget.CastPlayer(out var target))
            return;

        if (!player.IsInOverwatch)
            return;

        if (!Spies.Contains(target))
            return;
        
        player.SendFormattedAlert(OverwatchSpyMessage, true, AlertType.Warn, 5f, "Chaos Spy");
    }

    private static void OnHurt(PlayerHurtEventArgs args)
    {
        if (!args.Attacker.CastPlayer(out var attacker))
            return;

        if (!args.Player.CastPlayer(out var target))
            return;

        if (attacker == target)
            return;
        
        var isAttackerSpy = Spies.Contains(attacker);
        var isTargetSpy = Spies.Contains(target);

        if (isAttackerSpy)
        {
            if (isTargetSpy)
            {
                attacker.SendFormattedAlert(SpyFriendlyAttackMessage, true, AlertType.Warn, 5f, "Chaos Spy");
            }
            else if (target.Role.Team is Team.FoundationForces or Team.Scientists or Team.OtherAlive)
            {
                target.SendFormattedAlert(AttackedBySpyMessage, true, AlertType.Warn, 5f, "Chaos Spy");
                
                Spies.Remove(attacker);
            }
        }
        else if (isTargetSpy)
        {
            if (isAttackerSpy)
            {
                attacker.SendFormattedAlert(SpyFriendlyAttackMessage, true, AlertType.Warn, 5f, "Chaos Spy");
            }
        }
    }

    private static void OnDied(PlayerDeathEventArgs args)
    {
        if (!args.Attacker.CastPlayer(out var attacker))
            return;
        
        if (!args.Player.CastPlayer(out var player))
            return;
        
        if (Spies.Contains(attacker))
            player.SendFormattedAlert(KilledBySpyMessage, true, AlertType.Warn, 5f, "Chaos Spy");
        
        Spies.Remove(attacker);
        Spies.Remove(player);
    }
    
    private static void OnRespawned(WaveRespawnedEventArgs args)
    {
        if (MinWaveSize > 0 && args.Players.Count < MinWaveSize)
            return;

        if (SpawnWeight <= 0f || (SpawnWeight < 100f && !WeightUtils.GetBool(SpawnWeight)))
            return;
        
        SpawnChaosSpy((ExPlayer)args.Players.GetRandomItem(p => p.Role is RoleTypeId.NtfPrivate) ?? (ExPlayer)args.Players.GetRandomItem());
    }

    private static void OnLeft(ExPlayer player)
    {
        Spies.Remove(player);
    }

    private static void OnWaiting()
    {
        Spies.Clear();
    }

    private static RoleTypeId GetVisibleRole(ReferenceHub player, ReferenceHub receiver, RoleTypeId role,
        NetworkWriter writer)
    {
        if (Spies.Any(p => p.ReferenceHub == player)
            && (receiver.roleManager.CurrentRole.Team is Team.ChaosInsurgency
                || player == receiver
                || Spies.Any(p => p.ReferenceHub == receiver)
                || (receiver.serverRoles.IsInOverwatch
                    && receiver.roleManager.CurrentRole is SpectatorRole spectatorRole
                    && spectatorRole.SyncedSpectatedNetId == player.netId)))
            return RoleTypeId.ChaosRepressor;

        return role;
    }
    
    private static void Initialize()
    {
        FpcServerPositionDistributor.RoleSyncEvent += GetVisibleRole;
        
        PlayerEvents.Hurt += OnHurt;
        PlayerEvents.Death += OnDied;
        PlayerEvents.ChangedSpectator += OnSpectating;
        
        ServerEvents.WaveRespawned += OnRespawned;

        ExPlayerEvents.Left += OnLeft;
        ExRoundEvents.WaitingForPlayers += OnWaiting;
    }
}