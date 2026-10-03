using System.ComponentModel;

using InventorySystem.Items.Firearms.Attachments;

using LabExtended.API;
using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons;

/// <summary>
/// A custom sniper rifle item with configurable properties such as damage, ammo capacity, and attachments.
/// </summary>
public class SniperRifle : CustomFirearm
{
    public static SniperRifle Instance { get; private set; }

    /// <inheritdoc/>
    public override string Id { get; } = "sniperrifle";

    /// <inheritdoc/>
    public override string Name { get; } = "Sniper Rifle";

    /// <inheritdoc/>
    public override int MaxAmmo { get; set; } = 1;

    /// <inheritdoc/>
    public override bool CanChangeAttachments { get; set; } = false;

    /// <inheritdoc/>
    public override ItemType PickupType { get; set; } = ItemType.GunE11SR;

    /// <inheritdoc/>
    public override ItemType InventoryType { get; set; } = ItemType.GunE11SR;

    /// <inheritdoc/>
    public override AttachmentName[]? DefaultAttachments { get; set; } =
    [
        AttachmentName.ScopeSight,
        AttachmentName.LightweightStock,
        AttachmentName.SoundSuppressor,
        AttachmentName.StandardMagFMJ,
        AttachmentName.RifleBody
    ];

    /// <summary>
    /// Gets or sets the damage the sniper rifle deals.
    /// </summary>
    [Description("Sets the damage of the sniper rifle.")]
    public float Damage { get; set; } = 250f;
    
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

    /// <inheritdoc/>
    public override float ModifyDamage(ExPlayer target, float damage)
    {
        return Damage;
    }

    public override void OnRegistered()
    {
        base.OnRegistered();

        Instance = this;
    }
}