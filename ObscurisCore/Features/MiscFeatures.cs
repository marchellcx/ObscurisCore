using CustomPlayerEffects;

using InventorySystem.Items.Usables;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;

using LabExtended.Events;
using LabExtended.Events.Player.Snake;

using MapGeneration;
using Mirror;

using NiveraAPI.Extensions;

using NiveraAPI.IO.Configs;
using NiveraAPI.IO.Storage;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Storage;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;

using Respawning;

using System.Text;

using UnityEngine;

namespace ObscurisCore.Features;

/// <summary>
/// Represents a collection of miscellaneous features and configurations for the game.
/// </summary>
public static class MiscFeatures
{
    /// <summary>
    /// The index of the KillObjective.
    /// </summary>
    public const int KillObjectiveIndex = 0;

    /// <summary>
    /// Gets the intensity of the Movement Boost effect upon escaping a pocket dimension.
    /// </summary>
    [Config("misc-features", "pocket-dimension-escape-boost-intensity", "Sets the intensity of the Movement Boost effect upon escaping a pocket dimension.")]
    public static byte PdSpeedIntensity { get; set; } = 20;

    /// <summary>
    /// Gets the duration of the Movement Boost effect upon escaping a pocket dimension.
    /// </summary>
    [Config("misc-features", "pocket-dimension-escape-boost-duration", "Sets the duration of the Movement Boost effect upon escaping a pocket dimension.")]
    public static float PdSpeedDuration { get; set; } = 5f;

    /// <summary>
    /// Gets or sets a value indicating whether the kill feed feature is enabled or disabled.
    /// </summary>
    [Config("misc-features", "kill-feed-enabled", "Enables or disables the kill feed feature.")]
    public static bool KillFeedEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of roles that will not receive kill feed notifications.
    /// </summary>
    [Config("misc-features", "kill-feed-blacklist", "Specifies the roles that will not receive kill feed notifications.")]
    public static RoleTypeId[] KillFeedBlacklist { get; set; } = [];

    [Config("misc-features", "player-info-health-display", "Enables or disables the display of health information in the player info.")]
    public static bool PlayerInfoHealthDisplay { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the snake game over explosion feature is enabled or disabled.
    /// </summary>
    [Config("misc-features", "snake-explosion-enabled", "Enables or disables the snake game over explosion.")]
    public static bool SnakeExplosionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether persistent overwatch is enabled or disabled.
    /// </summary>
    [Config("misc-features", "persistent-overwatch-enabled", "Enables or disables persistent overwatch.")]
    public static bool PersistentOverwatchEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SCP-500 will clear all effects when used.
    /// </summary>
    [Config("misc-features", "scp-500-clear-all-effects-enabled", "Enables or disables SCP-500 clear all effects.")]
    public static bool Scp500ClearAllEffects { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of effects that SCP-500 will not disable.
    /// </summary>
    [Config("misc-features", "scp-500-clear-all-effects-ignore-effects", "List of effects that SCP-500 will not disable.")]
    public static List<string> Scp500IgnoreEffects { get; set; } = new()
    {
        nameof(PocketCorroding),
        nameof(Corroding)
    };

    [Storage("alternative-nicks", true, typeof(ByteReaderWriterSerializer<string>))] private static StorageDirectory altNicks;
    [Storage("persistent-overwatch", true, typeof(ByteReaderWriterSerializer<bool>))] private static StorageDirectory persistentOverwatch;

    /// <summary>
    /// Sets the alternative nickname for the specified user identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose alternative nickname will be set.</param>
    /// <param name="nick">The alternative nickname to associate with the user.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="userId"/> or <paramref name="nick"/> is null or an empty string.
    /// </exception>
    public static void SetAltNick(string userId, string nick)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentNullException(nameof(userId));

        if (string.IsNullOrEmpty(nick))
            throw new ArgumentNullException(nameof(nick));

        var value = altNicks.AddStorageValue(userId, () => nick);

        value.Value = nick;

        if (ExPlayer.TryGetByUserId(userId, out var player))
        {
            player.ReferenceHub.nicknameSync.Network_myNickSync = nick;
            player.SendConsoleMessage($"Updated alternative nick to: {nick}");
        }
    }

    /// <summary>
    /// Removes the alternative nickname associated with the specified user identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose alternative nickname should be removed.</param>
    /// <returns>
    /// True if the alternative nickname was successfully removed; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="userId"/> is null or an empty string.
    /// </exception>
    public static bool RemoveAltNick(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentNullException(nameof(userId));

        return altNicks.RemoveStorageValue(userId, true);
    }

    /// <summary>
    /// Sends a death notification to the specified target, providing details about the attacker and victim.
    /// </summary>
    /// <param name="target">The player receiving the death notification. Cannot be null.</param>
    /// <param name="attackerNick">The nickname of the attacking player.</param>
    /// <param name="attackerRole">The role type of the attacking player.</param>
    /// <param name="victimNick">The nickname of the victim player.</param>
    /// <param name="victimRole">The role type of the victim player.</param>
    /// <exception cref="ArgumentNullException">Thrown when the target is null.</exception>
    public static void SendKillFeedDeath(ExPlayer target, string attackerNick, RoleTypeId attackerRole, string victimNick,
        RoleTypeId victimRole)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        using (var writer = NetworkWriterPool.Get())
        {
            writer.WriteMessageId<ObjectiveCompletionMessage>();

            writer.WriteInt(KillObjectiveIndex); // ObjectiveIndex

            writer.WriteFloat(0f); // InfluenceReward
            writer.WriteFloat(0f); // TimeReward

            writer.WriteString(attackerNick); // AchievingPlayer.Nickname
            writer.WriteRoleType(attackerRole); // AchievingPlayer.Role

            writer.WriteString(victimNick); // VictimFootprint.Nickname
            writer.WriteRoleType(victimRole); // VictimFootprint.Role

            target.Connection.Send(writer);
        }
    }

    /// <summary>
    /// Sends a death notification to players that satisfy the specified condition, providing details about the attacker and victim.
    /// </summary>
    /// <param name="attackerNick">The nickname of the attacking player.</param>
    /// <param name="attackerRole">The role type of the attacking player.</param>
    /// <param name="victimNick">The nickname of the victim player.</param>
    /// <param name="victimRole">The role type of the victim player.</param>
    /// <param name="predicate">A condition to determine which players will receive the death notification. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the predicate is null.</exception>
    public static void SendKillFeedDeathToWhere(string attackerNick, RoleTypeId attackerRole, string victimNick, RoleTypeId victimRole, Predicate<ExPlayer> predicate)
        => ExPlayer.Players.Where(p => p?.ReferenceHub != null && predicate(p))
                           .ForEach(target => SendKillFeedDeath(target, attackerNick, attackerRole, victimNick, victimRole));

    /// <summary>
    /// Sends a death notification to all players, providing details about the attacker and victim.
    /// </summary>
    /// <param name="attackerNick">The nickname of the attacking player.</param>
    /// <param name="attackerRole">The role type of the attacking player.</param>
    /// <param name="victimNick">The nickname of the victim player.</param>
    /// <param name="victimRole">The role type of the victim player.</param>
    public static void SendKillFeedDeathToAll(string attackerNick, RoleTypeId attackerRole, string victimNick, RoleTypeId victimRole) 
        => ExPlayer.Players.ForEach(target => SendKillFeedDeath(target, attackerNick, attackerRole, victimNick, victimRole));

    // Kill Feed Death Notification & Zombie Alert for SCP-049-2 Deaths
    private static void OnDied(PlayerDeathEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (KillFeedEnabled
            && args.Attacker is ExPlayer attacker
            && attacker.IsValidPlayer()
            && !KillFeedBlacklist.Contains(player.Role))
            SendKillFeedDeathToAll(attacker.Nickname, attacker.Role, player.Nickname, args.OldRole);

        if (args.OldRole is RoleTypeId.Scp0492)
        {
            var scpPlayers = ExPlayer.Players.Where(p => p.Role.Is(RoleTypeId.Scp049));

            var deathReason = args.DamageHandler.TranslateDeathReason(args.Attacker != null
                ? $"<color=orange>{args.Attacker.Nickname}</color>"
                : null);

            var deathRoom = args.OldPosition.TryGetRoom(out var room) ? room : null;
            var deathTesla = deathRoom != null && TeslaGate.AllGates.Any(
                g => g != null && g.Room != null && g.Room == deathRoom);

            foreach (var scp in scpPlayers)
            {
                if (scp?.ReferenceHub == null)
                    continue;

                scp.SendAlert(AlertType.Warn, 15f, "Smrt SCP-049-2",
                    $"<b><color=red>SCP-049-2</color> <color=yellow>{player.Nickname}</color> byl</b>\n" +
                    $"<b>{deathReason}</b>" +
                    $"{(deathRoom != null
                        ? $"\n<b>v místnosti <color=yellow>{deathRoom.Name}</color> {(deathTesla ? "s Tesla bránou" : "bez Tesla brány")}</b>\n"
                        : "\n")} <b>ve vzdálenosti <color=red>{Mathf.CeilToInt(Vector3.Distance(args.OldPosition, scp.Position))}</color> metrů</b>.");
            }
        }
    }

    // Snake Game Over Explosion
    private static void OnSnakeGameOver(PlayerSnakeGameOverEventArgs args)
    {
        if (!SnakeExplosionEnabled)
            return;

        args.Player.Explode(1, ItemType.GrenadeHE, "Game Over", true, true, 10f);
    }

    // SCP-500 Clear All Effects
    private static void OnUsedItem(PlayerUsedItemEventArgs args)
    {
        if (!Scp500ClearAllEffects)
            return;

        if (args.UsableItem?.Base == null
            || args.UsableItem.Base is not Scp500
            || args.Player is not ExPlayer player)
            return;

        foreach (var effect in player.ActiveEffects.ToArray())
        {
            if (Scp500IgnoreEffects.Contains(effect.GetType().Name))
                continue;

            effect.ServerDisable();
        }
    }

    // Player Info Health Display
    private static void OnRefreshingCustomInfo(ExPlayer player, StringBuilder builder)
    {
        if (!player.Role.IsAlive)
            return;

        var health = Mathf.CeilToInt(player.Health);
        var maxHealth = Mathf.CeilToInt(player.MaxHealth);

        if (player.Role.Is(RoleTypeId.Scp3114)
            && player.Subroutines.Scp3114Identity.CurIdentity != null
            && player.Subroutines.Scp3114Identity.CurIdentity.Status
                is Scp3114Identity.DisguiseStatus.Active or Scp3114Identity.DisguiseStatus.Equipping)
        {
            if (player.Subroutines.Scp3114Identity.CurIdentity.StolenRole.TryGetRoleTemplate<PlayerRoleBase>(out var role)
                && role is IHealthbarRole healthbarRole)
            {
                health = Mathf.CeilToInt(healthbarRole.MaxHealth * Mathf.CeilToInt((player.Health / player.MaxHealth) * 100f) / 100f);
                maxHealth = Mathf.CeilToInt(healthbarRole.MaxHealth);
            }
            else
            {
                health = Mathf.CeilToInt(Mathf.Clamp(player.Health, 0f, 100f));
                maxHealth = Mathf.CeilToInt(Mathf.Clamp(player.MaxHealth, 0f, 100f));
            }
        }

        builder.AppendLine($"{health} HP / {maxHealth} HP");
    }

    // Pocket Dimension Escape Boost
    private static void OnEscaped(PlayerLeftPocketDimensionEventArgs args)
    {
        if (PdSpeedIntensity == 0 || PdSpeedDuration == 0f)
            return;

        if (!args.IsSuccessful)
            return;

        if (args.Player is not ExPlayer player
            || player.Effects.IsActive<MovementBoost>())
            return;

        player.Effects.EnableEffect<MovementBoost>(PdSpeedIntensity, PdSpeedDuration, true);
    }

    // Persistent Overwatch
    private static void OnRoleChanged(PlayerChangedRoleEventArgs args)
    {
        if (!args.Player.RemoteAdminAccess)
            return;

        if (args.NewRole.RoleTypeId != RoleTypeId.Overwatch)
        {
            if (!persistentOverwatch.TryGetStorageValue<bool>(args.Player.UserId, out var storageValue)
                || !storageValue.Value)
                return;

            storageValue.Value = false;
        }
        else
        {
            var overwatchStatus = persistentOverwatch.AddStorageValue(args.Player.UserId, () => true);

            if (!overwatchStatus.Value)
                overwatchStatus.Value = true;
        }
    }

    // Persistent Overwatch & Alternative Nicknames
    private static void OnPlayerVerified(ExPlayer player)
    {
        if (altNicks.TryGetValue(player.UserId, out string? nick) && !string.IsNullOrEmpty(nick))
        {
            player.ReferenceHub.nicknameSync.Network_myNickSync = nick;
            player.SendConsoleMessage($"Updated alternative nick to: {nick}");
        }

        if (player.RemoteAdminAccess
            && persistentOverwatch.TryGetStorageValue<bool>(player.UserId, out var storageValue)
            && storageValue.Value)
        {
            player.IsInOverwatch = true;
            player.SendConsoleMessage("Persistent Overwatch enabled for this session.");
        }
    }

    private static void Initialize()
    {
        ExPlayerEvents.Verified += OnPlayerVerified;
        ExPlayerEvents.SnakeGameOver += OnSnakeGameOver;

        if (PlayerInfoHealthDisplay)
            ExPlayerEvents.RefreshingCustomInfo += OnRefreshingCustomInfo;

        PlayerEvents.Death += OnDied;
        PlayerEvents.UsedItem += OnUsedItem;
        PlayerEvents.ChangedRole += OnRoleChanged;
        PlayerEvents.LeftPocketDimension += OnEscaped;
    }
}
