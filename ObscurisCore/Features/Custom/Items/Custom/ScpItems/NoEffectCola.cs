using System.ComponentModel;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;

using LabExtended.Attributes;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Custom.Items.Custom.ScpItems;

/// <summary>
/// Represents a custom cola item that has no effect.
/// </summary>
[LoaderIgnore]
public class NoEffectCola : CustomItem
{
    private ItemType colaType;

    /// <inheritdoc/>
    public override string Id { get; }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override ItemType PickupType
    {
        get => colaType;
        set => colaType = value;
    }

    /// <inheritdoc/>
    public override ItemType InventoryType
    {
        get => colaType;
        set => colaType = value;
    }
    
    /// <summary>
    /// Creates a new NoEffectCola.
    /// </summary>
    /// <param name="colaType">The type of cola to create.</param>
    /// <exception cref="ArgumentException">Thrown if the cola type is invalid.</exception>
    public NoEffectCola(ItemType colaType)
    {
        if (colaType is not ItemType.SCP207 and not ItemType.AntiSCP207)
            throw new ArgumentException("Invalid cola type", nameof(colaType));
        
        this.colaType = colaType;

        Id = colaType == ItemType.SCP207 ? "noeffect_cola" : "noeffect_anti_cola";
        Name = colaType == ItemType.SCP207 ? "No Effect Cola" : "No Effect Anti-Cola";
    }
    
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

    private static void Initialize()
    {
        new NoEffectCola(ItemType.SCP207).Register();
        new NoEffectCola(ItemType.AntiSCP207).Register();
    }
}