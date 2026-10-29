using System.ComponentModel;

using LabExtended.API;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Custom.Effects.Misc;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents the Midas Touch random event.
/// The event enables a special Midas Touch effect for players.
/// </summary>
public class MidasTouchEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the Midas Touch event.
    /// </summary>
    public override string Id { get => "MidasTouch"; }

    /// <summary>
    /// Gets or sets a value indicating whether this event can be grouped with other events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = true;

    /// <summary>
    /// Indicates whether the Midas Touch event can be activated during an ongoing round.
    /// If set to true, the event can be initiated in the middle of a round.
    /// </summary>
    public override bool CanActivateMidRound { get; set; } = true;
    
    /// <summary>
    /// Indicates whether the Midas Touch event should be automatically disabled at the end of a round.
    /// </summary>
    public override bool ShouldDisableOnRoundEnd { get; set; } = true;

    /// <summary>
    /// The message to be sent to players when the event is enabled.
    /// </summary>
    [Description("The message to be sent to players when the event is enabled.")]
    public string EnabledMessage { get; set; } = string.Empty;

    /// <summary>
    /// The message to be sent to players when they join the server with the Coin Madness event enabled.
    /// </summary>
    [Description("The message to be sent to players when they join the server with the event enabled.")]
    public string JoinedMessage { get; set; } = string.Empty;

    /// <summary>
    /// Handles the actions to be performed when the Midas Touch event is enabled.
    /// Sends a formatted alert to all players and enables the Midas Touch effect for them.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();
        
        ExPlayer.Players.ForEach(p =>
        {
            p.SendFormattedAlert(EnabledMessage, true);
            p.Effects.GetOrAddCustomEffect<MidasTouchEffect>().Enable();
        });
    }

    /// <summary>
    /// Handles the actions to be performed when the Midas Touch event is disabled.
    /// Disables the Midas Touch effect for all players.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        ExPlayer.Players.ForEach(p => p.Effects.GetCustomEffect<MidasTouchEffect>()?.Disable());
    }

    /// <summary>
    /// Handles the logic for when a player joins the server while the Midas Touch event is enabled.
    /// Sends a formatted alert to the player and applies the Midas Touch effect.
    /// </summary>
    /// <param name="player">The player who joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true);
        player.Effects.GetOrAddCustomEffect<MidasTouchEffect>().Enable();
    }
}