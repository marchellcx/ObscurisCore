using System.ComponentModel;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

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
    public string PickUpMessage { get; set; }

    /// <summary>
    /// Called when the impact grenade is added to a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing details about the item being added, including the player and related data.</param>
    public override void OnItemAdded(CustomItemAddedEventArgs args)
    {
        base.OnItemAdded(args);
        
        args.Player.SendFormattedAlert(PickUpMessage, true, AlertType.Info, 5f, "Custom Items");
    }
}