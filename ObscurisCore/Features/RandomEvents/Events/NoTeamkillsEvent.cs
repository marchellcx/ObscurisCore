using System.ComponentModel;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.Extensions;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a random event that prevents players from killing teammates.
/// </summary>
public class NoTeamkillsEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "NoTeamkills";

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
    /// The message to be sent to players who attempt to kill teammates during the event.
    /// </summary>
    [Description("The message to be sent to players who attempt to damage teammates during the event.")]
    public string DamageMessage { get; set; }
    
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
    /// 
    /// </summary>
    [Description("Whether or not to use HitboxIdentity to decide enemy roles.")]
    public bool UseHitboxIdentity { get; set; }

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// Registers the necessary event handlers and displays an alert to all players, signaling that the event is active.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();
        
        PlayerEvents.Hurting += OnHurting;
        
        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "No Teamkills"));
    }

    /// <summary>
    /// Overrides the behavior to execute when the random event is disabled.
    /// Removes registered event handlers and performs necessary cleanup to restore the original game state.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        PlayerEvents.Hurting -= OnHurting;
    }

    /// <summary>
    /// Handles the logic when a player joins the server during the "No Teamkills" random event.
    /// Displays a formatted alert to the player indicating that the event is active.
    /// </summary>
    /// <param name="player">The player who joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "No Teamkills");
    }

    private void OnHurting(PlayerHurtingEventArgs args)
    {
        if (!IsActive)
            return;

        if (!args.Attacker.CastPlayer(out var attacker))
            return;

        if (!args.Player.CastPlayer(out var target))
            return;

        if (attacker == target)
            return;

        if (UseHitboxIdentity)
        {
            if (HitboxIdentity.IsEnemy(attacker.ReferenceHub, target.ReferenceHub))
            {
                return;
            }
        }
        else if (attacker.Role.Type.IsEnemy(target.Role.Type))
        {
            return;
        }

        args.IsAllowed = false;
        
        attacker.SendFormattedAlert(DamageMessage, true, AlertType.Info, 5f, "No Teamkills");
    }
}