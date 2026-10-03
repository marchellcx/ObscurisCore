using System.ComponentModel;

using LabExtended.API;
using LabExtended.API.Custom.Gamemodes;
using LabExtended.Core;
using LabExtended.Utilities;
using LabExtended.Extensions;

using LabExtended.Events.Round;
using MEC;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;

using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a custom random event in the game wherein players face off in a Boss Fight scenario.
/// </summary>
public class BossFightEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "BossFight";

    /// <summary>
    /// Determines whether the event can be combined with other events in a group.
    /// If true, this event may occur alongside other compatible events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = true;
    
    /// <summary>
    /// Determines whether the event can be activated during the mid-round.
    /// </summary>
    public override bool CanActivateMidRound { get; set; } = false;

    /// <summary>
    /// Determines whether the event should be disabled when the round ends.
    /// </summary>
    public override bool ShouldDisableOnRoundEnd { get; set; } = true;
    
    /// <summary>
    /// The message to be sent to players when the event is enabled.
    /// </summary>
    [Description("The message to be sent to players when the event is enabled.")]
    public string EnabledMessage { get; set; }

    /// <summary>
    /// The message to be sent to players when they join the server with the event enabled.
    /// </summary>
    [Description("The message to be sent to players when they join the server with the event enabled.")]
    public string JoinedMessage { get; set; }

    /// <summary>
    /// The possible SCP roles and their health values.
    /// </summary>
    [Description("The possible SCP roles and their health values.")]
    public Dictionary<RoleTypeId, float> Roles { get; set; } = new()
    {
        { RoleTypeId.Scp173, 10000f },
    };

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Boss Fight"));
    }

    /// <summary>
    /// Executes logic when a player joins the server while the Double Trouble event is enabled.
    /// Sends a formatted alert message to the player to indicate their participation in the event.
    /// </summary>
    /// <param name="player">The player who has just joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Boss Fight");
    }

    /// <summary>
    /// Executes the logic to modify player roles at the start of a round
    /// when the random event is active and configured to use the round start trigger.
    /// Specifically, this method evaluates the roles of SCP players and reassigns them
    /// to predetermined roles based on specified conditions and probabilities.
    /// </summary>
    public override void OnRoundStarted()
    {
        base.OnRoundStarted();

        ApiLog.Debug($"OnRoundStarted IsActive: {IsActive}");
        
        if (!IsActive)
            return;

        Timing.CallDelayed(1f, () =>
        {
            ApiLog.Debug("Selecting SCP");

            var scps = ExPlayer.Players.Where(p => p.Role.IsScp);

            if (scps.Count() < 1)
            {
                ApiLog.Warn("No SCPs found");
                return;
            }

            var target = scps.GetRandomItem();
            var role = Roles.GetRandomItem();

            ApiLog.Debug($"Selected player {target.ToLogString()} with role &1{role.Key}&r and health &1{role.Value}&r HP");

            target.Role.Set(role.Key, RoleChangeReason.RoundStart, RoleSpawnFlags.All);

            target.MaxHealth = role.Value;
            target.Health = role.Value;

            ApiLog.Debug("Setting other SCPs");

            foreach (var other in scps)
            {
                if (other != target)
                {
                    ApiLog.Debug($"Setting role for {other.ToLogString()}");

                    other.Role.Set(WeightUtils.GetBool(30f)
                        ? RoleTypeId.FacilityGuard
                        : (WeightUtils.GetBool(40f)
                            ? RoleTypeId.Scientist
                            : RoleTypeId.ClassD), RoleChangeReason.RoundStart, RoleSpawnFlags.All);
                }
            }
        });
    }
}