using System.ComponentModel;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;

using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.Extensions;

using MapGeneration.Distributors;

using MEC;

using Mirror;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Configs;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.RandomEvents.Events.Randomizer;

/// <summary>
/// Represents a random event where players' items can be periodically stolen and dropped during gameplay.
/// </summary>
/// <remarks>
/// This event allows for customization of various messages sent to players, such as notifications when the event is enabled,
/// when players join the server, and when their items are about to be stolen. The event also supports configuration for
/// item drop intervals and can handle various states such as activation mid-round and cleanup on round end.
/// </remarks>
public class RandomSizeEvent : RandomEventBase
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public override string Id { get; } = "RandomSize";

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
    /// Defines the range for scaling item sizes during the random size event.
    /// </summary>
    /// <remarks>
    /// This property specifies the minimum and maximum range of item scales that can be applied
    /// during events that randomize the size of items. The actual size for each item is determined
    /// by generating a random value within this range.
    /// </remarks>
    [Description("The range of sizes for items.")]
    public VectorRange ItemSizeRange { get; set; } = new();

    /// <summary>
    /// Specifies the range of possible player sizes during the random size event.
    /// </summary>
    /// <remarks>
    /// This property defines the minimum and maximum scaling values for players across the X, Y, and Z axes.
    /// It allows for customization of player sizes within predefined limits, enabling dynamic adjustments during gameplay.
    /// The range values are typically used to randomly scale players' sizes during the event.
    /// </remarks>
    [Description("The range of sizes for players.")]
    public VectorRange PlayerSizeRange { get; set; } = new();
    
    /// <summary>
    /// The range of sizes for generators.
    /// </summary>
    [Description("The range of sizes for generators.")]
    public VectorRange GeneratorSizeRange { get; set; } = new();
    
    /// <summary>
    /// The range of sizes for lockers.
    /// </summary>
    [Description("The range of sizes for lockers.")]
    public VectorRange LockerSizeRange { get; set; } = new();

    /// <summary>
    /// The range of sizes for pedestals.
    /// </summary>
    [Description("The range of sizes for pedestals.")]
    public VectorRange PedestalSizeRange { get; set; } = new();
    
    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();
        
        PlayerEvents.ChangedRole += OnChangedRole;
        PlayerEvents.DroppedItem += OnItemDropped;
        PlayerEvents.ThrewProjectile += OnThrownProjectile;
        
        ServerEvents.ItemSpawned += OnItemSpawned;

        ExPlayer.Players.ForEach(p => p.SendFormattedAlert(EnabledMessage, true, AlertType.Info, 5f, "Random Size"));

        if (ExRound.IsRunning)
        {
            ExPlayer.Players.ForEach(p => p.Scale = PlayerSizeRange.GetRandom());
            
            ExMap.Pickups.ForEach(p =>
            {
                if (p == null)
                    return;
                
                NetworkServer.UnSpawn(p.gameObject);
                
                p.transform.localScale = ItemSizeRange.GetRandom();
                
                NetworkServer.Spawn(p.gameObject);
            });
            
            Map.Generators.ForEach(g =>
            {
                if (g?.Base == null)
                    return;
                
                NetworkServer.UnSpawn(g.Base.gameObject);
                
                g.Base.transform.localScale = GeneratorSizeRange.GetRandom();
                
                NetworkServer.Spawn(g.Base.gameObject);
            });
            
            ExMap.Lockers.ForEach(l =>
            {
                if (l == null)
                    return;
                
                NetworkServer.UnSpawn(l.gameObject);
                
                l.transform.localScale = LockerSizeRange.GetRandom();
                
                NetworkServer.Spawn(l.gameObject);
            });
            
            UnityEngine.Object.FindObjectsByType<PedestalScpLocker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ForEach(l =>
            {
                if (l == null)
                    return;
                
                NetworkServer.UnSpawn(l.gameObject);
                
                l.transform.localScale = PedestalSizeRange.GetRandom();
                
                NetworkServer.Spawn(l.gameObject);
            });
        }
    }

    /// <summary>
    /// Handles the cleanup logic when the random event is disabled.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        PlayerEvents.ChangedRole -= OnChangedRole;
        PlayerEvents.DroppedItem -= OnItemDropped;
        PlayerEvents.ThrewProjectile -= OnThrownProjectile;
        
        ServerEvents.ItemSpawned -= OnItemSpawned;
    }

    /// <summary>
    /// Executes logic when a player joins the server while the Coin Madness event is enabled.
    /// Sends a formatted alert message to the player to indicate their participation in the event.
    /// </summary>
    /// <param name="player">The player who has just joined the server.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        base.OnPlayerJoined(player);
        
        player.SendFormattedAlert(JoinedMessage, true, AlertType.Info, 5f, "Random Size");
    }

    /// <summary>
    /// Executes logic to randomize object sizes on the map when a new round begins.
    /// This method adjusts the scale of pickups, generators, lockers, and pedestals
    /// based on the predefined size ranges for each category.
    /// </summary>
    public override void OnRoundStarted()
    {
        base.OnRoundStarted();
        
        ExMap.Pickups.ForEach(p =>
        {
            if (p == null)
                return;
                
            NetworkServer.UnSpawn(p.gameObject);
                
            p.transform.localScale = ItemSizeRange.GetRandom();
                
            NetworkServer.Spawn(p.gameObject);
        });
            
        Map.Generators.ForEach(g =>
        {
            if (g?.Base == null)
                return;
                
            NetworkServer.UnSpawn(g.Base.gameObject);
                
            g.Base.transform.localScale = GeneratorSizeRange.GetRandom();
                
            NetworkServer.Spawn(g.Base.gameObject);
        });
            
        ExMap.Lockers.ForEach(l =>
        {
            if (l == null)
                return;
                
            NetworkServer.UnSpawn(l.gameObject);
                
            l.transform.localScale = LockerSizeRange.GetRandom();
                
            NetworkServer.Spawn(l.gameObject);
        });
            
        UnityEngine.Object.FindObjectsByType<PedestalScpLocker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ForEach(l =>
        {
            if (l == null)
                return;
                
            NetworkServer.UnSpawn(l.gameObject);
                
            l.transform.localScale = PedestalSizeRange.GetRandom();
                
            NetworkServer.Spawn(l.gameObject);
        });
    }

    private void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        Timing.CallDelayed(0.5f, () =>
        {
            args.Player.Scale = PlayerSizeRange.GetRandom();
        });
    }

    private void OnItemSpawned(ItemSpawnedEventArgs args)
    {
        if (args.Pickup?.Base == null)
            return;
        
        NetworkServer.UnSpawn(args.Pickup.Base.gameObject);
        
        args.Pickup.Base.transform.localScale = ItemSizeRange.GetRandom();
        
        NetworkServer.Spawn(args.Pickup.Base.gameObject);
    }

    private void OnItemDropped(PlayerDroppedItemEventArgs args)
    {
        if (args.Pickup?.Base == null)
            return;
        
        NetworkServer.UnSpawn(args.Pickup.Base.gameObject);
        
        args.Pickup.Base.transform.localScale = ItemSizeRange.GetRandom();
        
        NetworkServer.Spawn(args.Pickup.Base.gameObject);
    }

    private void OnThrownProjectile(PlayerThrewProjectileEventArgs args)
    {
        if (args.Projectile?.Base == null)
            return;
        
        NetworkServer.UnSpawn(args.Projectile.Base.gameObject);
        
        args.Projectile.Base.transform.localScale = ItemSizeRange.GetRandom();
        
        NetworkServer.Spawn(args.Projectile.Base.gameObject);
    }
}