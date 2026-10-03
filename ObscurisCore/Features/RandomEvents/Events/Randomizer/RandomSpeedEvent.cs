using System.ComponentModel;
using System.Globalization;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabExtended.API;
using LabExtended.Utilities;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.RandomEvents.Events.Randomizer;

/// <summary>
/// Represents a random event that adjusts player movement speed to a randomized value within a specified range.
/// Players will experience changes to their speed for a set duration when this event is active.
/// </summary>
public class RandomSpeedEvent : RandomEventBase
{
    private float currentSpeed;
    private float remainingTime;
    
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "RandomSpeed";

    /// <summary>
    /// Whether the event can be grouped with other events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = true;
    
    /// <summary>
    /// Whether the event can be activated during the mid-round.
    /// </summary>
    public override bool CanActivateMidRound { get; set; } = true;

    /// <summary>
    /// Whether the event should be disabled when the round ends.
    /// </summary>
    public override bool ShouldDisableOnRoundEnd { get; set; } = true;

    /// <summary>
    /// Defines the range for random speed value generation in the event.
    /// </summary>
    [Description("Sets the range of random speed value generation.")]
    public RangeFloat SpeedRange { get; set; } = new()
    {
        MinValue = 0.2f, 
        MaxValue = 2.0f 
    };

    /// <summary>
    /// The duration, in seconds, for which the speed change effect remains active during the random event.
    /// </summary>
    [Description("Sets the duration of the speed change in seconds.")]
    public float ChangeDuration { get; set; } = 30f;
    
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
    /// The message to be sent to players when their speed changes during the event.
    /// </summary>
    [Description("The message to be sent to players when their speed changes during the event. (Use $Speed for the new speed.)")]
    public string SpeedChangedMessage { get; set; }

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// Registers the necessary event handlers and displays an alert to all players, signaling that the event is active.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();
        
        PlayerEvents.ChangedRole += OnChangedRole;

        currentSpeed = SpeedRange.Random;
        remainingTime = ChangeDuration;
        
        ExPlayer.Players.ForEach(p =>
        {
            p.ChangeSpeedByMultiplier(currentSpeed);
            p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Random Speed");
        });
    }

    /// <summary>
    /// Overrides the behavior to execute when the random event is disabled.
    /// Removes registered event handlers and performs necessary cleanup to restore the original game state.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();

        currentSpeed = 0f;
        remainingTime = 0f;
        
        PlayerEvents.ChangedRole -= OnChangedRole;
        
        ExPlayer.Players.ForEach(p =>
        {
            p.ResetSpeed();
        });
    }

    /// <summary>
    /// Handles the logic when a player joins the server during the "No Teamkills" random event.
    /// Displays a formatted alert to the player indicating that the event is active.
    /// </summary>
    /// <param name="player">The player who joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);

        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Random Speed");
    }

    /// <summary>
    /// Called every update cycle while the random event is active.
    /// Handles the logic for updating the timer and applying new speed values to players once the timer expires.
    /// </summary>
    public override void OnUpdate()
    {
        base.OnUpdate();

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            currentSpeed = SpeedRange.Random;
            remainingTime = ChangeDuration;
            
            ExPlayer.Players.ForEach(p =>
            {
                p.ChangeSpeedByMultiplier(currentSpeed);
                p.SendFormattedAlert(SpeedChangedMessage.Replace("$Speed", currentSpeed.ToString(CultureInfo.InvariantCulture)), true, AlertType.Info, 5f, "Random Speed");
            });
        }
    }

    private void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        if (!IsActive)
            return;
        
        if (!args.NewRole.RoleTypeId.IsAlive())
            return;
        
        if (!args.Player.CastPlayer(out var player))
            return;
        
        player.ChangeSpeedByMultiplier(currentSpeed);
    }
}