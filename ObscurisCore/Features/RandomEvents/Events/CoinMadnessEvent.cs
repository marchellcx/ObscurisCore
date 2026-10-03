using System.ComponentModel;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;

using MEC;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a random event that gives players coins when they spawn.
/// </summary>
public class CoinMadnessEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "coinMadness";

    /// <summary>
    /// Determines whether the event can be combined with other events in a group.
    /// If true, this event may occur alongside other compatible events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = true;
    
    /// <summary>
    /// Determines whether the event can be activated during the mid-round.
    /// </summary>
    public override bool CanActivateMidRound { get; set; } = true;

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
    /// The message to be sent to players when they spawn with the event enabled.
    /// </summary>
    [Description("The message to be sent to players when they spawn with the event enabled.")]
    public string SpawnedMessage { get; set; }

    /// <summary>
    /// The message to be sent to players when they join the server with the Coin Madness event enabled.
    /// </summary>
    [Description("The message to be sent to players when they join the server with the event enabled.")]
    public string JoinedMessage { get; set; }

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Coin Madness"));
        
        PlayerEvents.ChangedRole += OnSpawned;
    }

    /// <summary>
    /// Handles the cleanup logic when the random event is disabled.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        PlayerEvents.ChangedRole -= OnSpawned;
    }

    /// <summary>
    /// Executes logic when a player joins the server while the Coin Madness event is enabled.
    /// Sends a formatted alert message to the player to indicate their participation in the event.
    /// </summary>
    /// <param name="player">The player who has just joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Coin Madness");
    }

    private void OnSpawned(PlayerChangedRoleEventArgs args)
    {
        Timing.CallDelayed(0.5f, () =>
        {
            if (!args.NewRole.RoleTypeId.IsAlive())
                return;
            
            if (!args.Player.CastPlayer(out var player))
                return;

            var amount = 8 - player.Inventory.ItemCount;

            if (amount < 1)
                return;
            
            player.SendFormattedAlert(SpawnedMessage, true, AlertType.Info, 5f, "Coin Madness");

            for (var x = 0; x < amount; x++)
                player.Inventory.AddItem(ItemType.Coin);
        });
    }
}