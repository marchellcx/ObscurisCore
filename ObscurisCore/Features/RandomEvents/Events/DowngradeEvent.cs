using System.ComponentModel;
using Interactables.Interobjects.DoorUtils;

using LabExtended.API;

using MapGeneration;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a random event that gives players coins when they spawn.
/// </summary>
public class DowngradeEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "Downgrade";

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

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Downgrade"));

        var room = RoomIdentifier.AllRoomIdentifiers.First(ri => ri != null && ri.Name is RoomName.Lcz914);

        if (room != null && DoorVariant.DoorsByRoom.TryGetValue(room, out var doors))
        {
            foreach (var door in doors)
            {
                if (door != null)
                {
                    door.NetworkTargetState = false;
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                }
            }
        }
    }

    /// <summary>
    /// Handles the cleanup logic when the random event is disabled.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        var room = RoomIdentifier.AllRoomIdentifiers.First(ri => ri != null && ri.Name is RoomName.Lcz914);

        if (room != null && DoorVariant.DoorsByRoom.TryGetValue(room, out var doors))
        {
            foreach (var door in doors)
            {
                if (door != null)
                {
                    door.NetworkTargetState = true;
                    door.ServerChangeLock(DoorLockReason.AdminCommand, false);
                }
            }
        }
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
}