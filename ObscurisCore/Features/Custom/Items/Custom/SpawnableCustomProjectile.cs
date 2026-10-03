using System.ComponentModel;

using InventorySystem.Items.ThrowableProjectiles;

using LabApi.Events.Arguments.ServerEvents;

using LabExtended.Core;
using LabExtended.Events;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;

using NiveraAPI.Utilities;

using NorthwoodLib.Pools;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Configs;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Custom.Items.Custom;

/// <summary>
/// Represents a custom item that can be spawned at specified locations.
/// </summary>
public abstract class SpawnableCustomProjectile : CustomProjectile
{
    /// <summary>
    /// The amount of spawn locations to use.
    /// </summary>
    [Description("Sets the amount of spawn locations to use.")]
    public int SpawnCount { get; set; } = 0;

    /// <summary>
    /// The spawn locations for the item.
    /// </summary>
    [Description("Sets the item's spawn locations.")]
    public List<SpawnLocation> Locations { get; set; } = new()
    {
        new()
    };
    
    /// <summary>
    /// Message to display when the player picks up the item.
    /// </summary>
    [Description("Message to display when the player picks up the item.")]
    public string PickUpMessage { get; set; }

    /// <summary>
    /// Called when the item is added to a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing details about the item being added, including the player and related data.</param>
    public override void OnItemAdded(CustomItemAddedEventArgs args)
    {
        base.OnItemAdded(args);
        
        args.Player.SendFormattedAlert(PickUpMessage, true, AlertType.Info, 5f, "Custom Items");
    }

    /// <summary>
    /// Registers event handlers and performs initialization logic required for the custom item.
    /// This method subscribes the <see cref="SpawnAtLocations"/> method to the <see cref="ExRoundEvents.Started"/> event,
    /// enabling automatic spawning mechanics at the start of a round.
    /// </summary>
    public override void OnRegistered()
    {
        base.OnRegistered();
        
        ExRoundEvents.Started += SpawnAtLocations;
    }

    /// <summary>
    /// Handles cleanup operations when the custom item is unregistered.
    /// This method unsubscribes the <see cref="SpawnAtLocations"/> method
    /// from the <see cref="ExRoundEvents.Started"/> event, ensuring that
    /// the item no longer participates in round start spawn mechanics.
    /// </summary>
    public override void OnUnregistered()
    {
        base.OnUnregistered();
        
        ExRoundEvents.Started -= SpawnAtLocations;
    }

    /// <summary>
    /// Spawns the defined number of items at the specified spawn locations.
    /// The method iterates through the list of available spawn locations with their associated weights,
    /// attempting to spawn items until the specified <see cref="SpawnCount"/> is reached, or until all
    /// suitable spawn locations have been exhausted. It ensures that each location's defined constraints
    /// such as <see cref="SpawnLocation.MaxAmount"/> are respected. If there are insufficient valid
    /// spawn locations to accommodate the desired <see cref="SpawnCount"/>, a warning is logged.
    /// Upon completion, resets the spawn counters of all spawn locations.
    /// </summary>
    public void SpawnAtLocations()
    {
        if (SpawnCount < 1)
            return;

        var spawned = 0;
        var locations = ListPool<SpawnLocation>.Shared.Rent(Locations);

        LockProjectile = false;
        
        try
        {
            while (spawned < SpawnCount && locations.Count > 0)
            {
                var location = locations.GetRandomWeighted(loc => loc.Weight);

                if (location == null)
                    continue;

                if (location.MaxAmount > 0 && location.SpawnCounter >= location.MaxAmount)
                {
                    locations.Remove(location);
                    continue;
                }

                if (!location.TryGetPositionAndRotation(true, true, out var position, out var rotation))
                {
                    locations.Remove(location);
                    continue;
                }

                ApiLog.Info(Name, $"Spawning projectile &3{Name}&r at &3{position}&r");
                
                SpawnItem(position, rotation);

                location.SpawnCounter++;

                spawned++;
            }

            if (spawned < SpawnCount)
                ApiLog.Warn("SpawnableCustomItem",
                    $"Not enough spawn locations for &1{SpawnCount}&r items (&6{Id}&r)!");

            Locations.ForEach(loc => loc.SpawnCounter = 0);
        }
        catch (Exception ex)
        {
            ApiLog.Error("SpawnableCustomItem", $"Failed to spawn items:\n{ex}");
        }

        ListPool<SpawnLocation>.Shared.Return(locations);
    }
}