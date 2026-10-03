using System.ComponentModel;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Utilities;
using LabExtended.Extensions;

using MEC;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;

using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a random event in the game called "Double Trouble."
/// This event modifies the game by altering player roles and sending specific
/// messages to players, depending on their state and participation in the event.
/// </summary>
public class DoubleTroubleEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "DoubleTrouble";

    /// <summary>
    /// Determines whether the event can be combined with other events in a group.
    /// If true, this event may occur alongside other compatible events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = false;
    
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
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Double Trouble"));
    }

    /// <summary>
    /// Executes logic when a player joins the server while the Double Trouble event is enabled.
    /// Sends a formatted alert message to the player to indicate their participation in the event.
    /// </summary>
    /// <param name="player">The player who has just joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Double Trouble");
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

        ApiLog.Debug($"OnRoundStart IsActive: {IsActive}");

        if (!IsActive)
            return;
        
        Timing.CallDelayed(1f, () =>
        {
            ApiLog.Debug("Selecting SCPs");

            var scps = ExPlayer.Players.Where(p => p.Role.IsScp).ToPooledList();

            if (scps.Count < 2)
            {
                ApiLog.Warn("Not enough SCPs");

                scps.ReturnToPool();
                return;
            }

            var scpOne = scps.RandomItem();
            
            scps.Remove(scpOne);
            
            var scpTwo = scps.RandomItem();

            scps.Remove(scpTwo);
            
            ApiLog.Debug($"Selected SCPs {scpOne.ToLogString()} and {scpTwo.ToLogString()}, remaining SCPs: {scps.Count}");
            
            scps.ForEach(p =>
            {
                ApiLog.Debug($"Setting role for other SCP {p.ToLogString()}");
                
                p.Role.Set(WeightUtils.GetBool(30f)
                    ? RoleTypeId.FacilityGuard
                    : (WeightUtils.GetBool(40f)
                        ? RoleTypeId.Scientist
                        : RoleTypeId.ClassD), RoleChangeReason.RoundStart, RoleSpawnFlags.All);
            });
            
            scps.ReturnToPool();

            scpOne.Role.Set(RoleTypeId.Scp939, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
            scpTwo.Role.Set(RoleTypeId.Scp939, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
            
            ApiLog.Debug("Spawned SCPs");
        });
    }
}