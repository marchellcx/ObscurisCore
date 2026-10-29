using System.ComponentModel;

using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;

using LabExtended.API.Custom.Effects;

using LabExtended.Events;
using LabExtended.Events.Player;

using ObscurisCore.Extensions;

using Utils;
using UnityEngine;

namespace ObscurisCore.Features.Custom.Effects.Misc;

/// <summary>
/// The MidasTouchEffect applies a custom effect to a player, triggering explosions
/// or explosion-based visuals when specific actions are performed by the player such as
/// using an item, dying, or dropping an item.
/// </summary>
public class MidasTouchEffect : CustomPlayerEffect
{
    /// <summary>
    /// Gets or sets a value indicating whether grenades should be spawned
    /// at the point of explosion during the effect execution.
    /// Controls whether grenades are instantiated instead of default explosion effects.
    /// </summary>
    [Description("Whether or not grenades should be spawned at point of explosion.")]
    public bool SpawnGrenades { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of explosions to spawn during the effect execution.
    /// Determines how many explosion instances will occur at the target position.
    /// </summary>
    [Description("The amount of explosions to spawn.")]
    public int ExplosionAmount { get; set; } = 1;

    /// <summary>
    /// Triggers an explosion or creates an explosion effect at the specified position, based on the configuration of the effect.
    /// </summary>
    /// <param name="position">The position where the explosion or effect should occur.</param>
    public void ExplodePosition(Vector3 position)
    {
        for (var x = 0; x < ExplosionAmount; x++)
        {
            if (SpawnGrenades)
            {
                ExplosionUtils.ServerExplode(position, Player!.Footprint, ExplosionType.Grenade);
            }
            else
            {
                ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
            }
        }
    }
    
    private static void OnPlayerUsedItem(PlayerUsedItemEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        var midasTouchEffect = player.Effects.GetOrAddCustomEffect<MidasTouchEffect>(false);

        if (!midasTouchEffect.IsActive)
            return;
        
        midasTouchEffect.ExplodePosition(player.Position);
    }
    
    private static void OnPlayerDied(PlayerDeathEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        var midasTouchEffect = player.Effects.GetOrAddCustomEffect<MidasTouchEffect>(false);

        if (!midasTouchEffect.IsActive)
            return;
        
        midasTouchEffect.ExplodePosition(args.OldPosition);
    }
    
    private static void OnPlayerDroppedItem(PlayerDroppedItemEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        var midasTouchEffect = player.Effects.GetOrAddCustomEffect<MidasTouchEffect>(false);

        if (!midasTouchEffect.IsActive)
            return;
        
        midasTouchEffect.ExplodePosition(args.Pickup.Position);
    }

    private static void OnPlayerThrownItem(PlayerThrewItemEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        var midasTouchEffect = player.Effects.GetOrAddCustomEffect<MidasTouchEffect>(false);

        if (!midasTouchEffect.IsActive)
            return;
        
        midasTouchEffect.ExplodePosition(args.Pickup.Position);
    }
    
    private static void OnPlayerShotFirearm(PlayerShotFirearmEventArgs args)
    {
        var midasTouchEffect = args.Player.Effects.GetOrAddCustomEffect<MidasTouchEffect>(false);

        if (!midasTouchEffect.IsActive)
            return;
        
        midasTouchEffect.ExplodePosition(args.Hit.point);
    }

    private static void Initialize()
    {
        PlayerEvents.Death += OnPlayerDied;
        PlayerEvents.UsedItem += OnPlayerUsedItem;
        PlayerEvents.ThrewItem += OnPlayerThrownItem;
        PlayerEvents.DroppedItem += OnPlayerDroppedItem;
        
        ExPlayerEvents.ShotFirearm += OnPlayerShotFirearm;
    }
}