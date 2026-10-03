using System.ComponentModel;

using LabExtended.API;
using LabExtended.Extensions;

using MapGeneration;

using MEC;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.RandomEvents.Events;

/// <summary>
/// Represents a random event where players' items can be periodically stolen and dropped during gameplay.
/// </summary>
/// <remarks>
/// This event allows for customization of various messages sent to players, such as notifications when the event is enabled,
/// when players join the server, and when their items are about to be stolen. The event also supports configuration for
/// item drop intervals and can handle various states such as activation mid-round and cleanup on round end.
/// </remarks>
public class StolenItemsEvent : RandomEventBase
{
    private float remainingTime;
    
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "StolenItems";

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
    /// The message to be sent to players when their items are about to be stolen.
    /// </summary>
    [Description("The message to be sent to players when their items are about to be stolen.")]
    public string StealingMessage { get; set; }

    /// <summary>
    /// Specifies the time interval, in seconds, between each occurrence of the item drop event.
    /// </summary>
    [Description("The interval between each item drop.")]
    public float Interval { get; set; } = 120f;

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Stolen Items"));

        remainingTime = Interval;
    }

    /// <summary>
    /// Handles the cleanup logic when the random event is disabled.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();

        remainingTime = 0f;
    }

    /// <summary>
    /// Executes logic when a player joins the server while the Coin Madness event is enabled.
    /// Sends a formatted alert message to the player to indicate their participation in the event.
    /// </summary>
    /// <param name="player">The player who has just joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Stolen Items");
    }

    /// <summary>
    /// Updates the state and logic of the random event periodically, including handling item stealing mechanics
    /// and notifying players about the event's progress.
    /// </summary>
    public override void OnUpdate()
    {
        base.OnUpdate();

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = Interval + 3f;
            
            ExPlayer.Players.ForEach(p => p.SendFormattedAlert(StealingMessage, true, AlertType.Info, 5f, "Stolen Items"));

            Timing.CallDelayed(3f, () =>
            {
                foreach (var player in ExPlayer.Players)
                {
                    var pickups = player.Inventory.DropItems();

                    foreach (var pickup in pickups)
                    {
                        pickup.Position = RoomIdentifier.AllRoomIdentifiers.GetRandomItem(ri => ri != null)
                                                                           .GetSafePosition(player);
                    }
                }
            });
        }
    }
}