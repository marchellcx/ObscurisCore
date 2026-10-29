using System.ComponentModel;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Elements.Alerts;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.Core;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons;

/// <summary>
/// Just SCP-1509 without the ability to respawn spectators.
/// </summary>
public class SimpleMachette : CustomItem
{
    /// <inheritdoc/>
    public override string Id { get; } = "machette";

    /// <inheritdoc/>
    public override string Name { get; } = "Simple Machette";

    /// <inheritdoc/>
    public override ItemType PickupType { get; set; } = ItemType.SCP1509;

    /// <inheritdoc/>
    public override ItemType InventoryType { get; set; } = ItemType.SCP1509;

    /// <summary>
    /// Message to display when the player picks up the item.
    /// </summary>
    [Description("Message to display when the player picks up the item.")]
    public string PickUpMessage { get; set; } = string.Empty;

    /// <summary>
    /// Invoked when the custom item is successfully registered.
    /// Allows for initializing event handlers or additional setup required for the custom item.
    /// </summary>
    public override void OnRegistered()
    {
        base.OnRegistered();
        
        PlayerEvents.Scp1509Resurrecting += OnScp1509Resurrecting;
    }

    /// <summary>
    /// Invoked when the custom item is successfully unregistered.
    /// Used to perform cleanup tasks such as deregistering event handlers or releasing resources specific to the custom item.
    /// </summary>
    public override void OnUnregistered()
    {
        base.OnUnregistered();
        
        PlayerEvents.Scp1509Resurrecting -= OnScp1509Resurrecting;
    }

    /// <summary>
    /// Called when the impact grenade is added to a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing details about the item being added, including the player and related data.</param>
    public override void OnItemAdded(CustomItemAddedEventArgs args)
    {
        base.OnItemAdded(args);
        
        args.Player.SendFormattedAlert(PickUpMessage, true, AlertType.Info, 5f, "Custom Items");
    }
    
    private void OnScp1509Resurrecting(PlayerScp1509ResurrectingEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (!args.RevivedPlayer.CastPlayer(out var revivedPlayer))
            return;
        
        if (!CheckItem(args.Item))
            return;

        ApiLog.Debug($"Prevented player SCP-1509 resurrection ({player.ToLogString()}): {revivedPlayer.ToLogString()}");
        
        args.IsAllowed = false;
    }
}