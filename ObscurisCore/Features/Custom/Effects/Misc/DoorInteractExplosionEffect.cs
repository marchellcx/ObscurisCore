using Interactables.Interobjects;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.API.Custom.Effects;

using Mirror;
using ObscurisCore.Extensions;
using System.ComponentModel;

namespace ObscurisCore.Features.Custom.Effects.Misc;

/// <summary>
/// Represents a custom player effect that triggers an explosion upon interacting with a door.
/// This effect can optionally damage or kill the player who interacts with the door. In addition,
/// the door can be destroyed when the effect is applied.
/// </summary>
public class DoorInteractExplosionEffect : CustomPlayerEffect
{
    /// <summary>
    /// Gets or sets the reason for the player's death.
    /// </summary>
    [Description("The reason for the player's death.")]
    public string DeathReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount of grenades to spawn.
    /// </summary>
    [Description("The amount of grenades to spawn.")]
    public int GrenadeAmount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the type of grenade to spawn.
    /// </summary>
    [Description("The type of grenade to spawn.")]
    public ItemType GrenadeType { get; set; } = ItemType.GrenadeHE;

    /// <summary>
    /// Whether or not actual grenades should be spawned.
    /// </summary>
    [Description("Whether or not actual grenades should be spawned.")]
    public bool IsEffectOnly { get; set; } = true;

    /// <summary>
    /// Whether or not the player should be killed.
    /// </summary>
    [Description("Whether or not the player should be killed.")]
    public bool IsDeath { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether doors that cannot be broken should be deleted instead.
    /// </summary>
    [Description("Whether or not doors which are not breakable should be deleted instead.")]
    public bool DeleteNonBreakable { get; set; } = true;

    /// <summary>
    /// Gets or sets the velocity multiplier of the player's ragdoll. 
    /// </summary>
    [Description("Sets the velocity multiplier for the player's ragdoll.")]
    public float VelocityMultiplier { get; set; } = 10f;
    
    /// <inheritdoc />
    public override void ApplyEffects()
    {
        base.ApplyEffects();
        PlayerEvents.InteractedDoor += OnInteracted;
    }

    /// <inheritdoc />
    public override void RemoveEffects()
    {
        base.RemoveEffects();
        PlayerEvents.InteractedDoor -= OnInteracted;
    }

    private void OnInteracted(PlayerInteractedDoorEventArgs args)
    {
        if (Player?.ReferenceHub == null || !IsActive) 
            return;
        
        if (args.Player is not ExPlayer player)
            return;
        
        if (player != Player) 
            return;
        
        if (args.Door?.Base == null) 
            return;

        if (args.Door.Base is BreakableDoor breakableDoor)
            breakableDoor.Network_destroyed = true;
        else if (args.Door.Base is not PryableDoor)
            NetworkServer.Destroy(args.Door.Base.gameObject);
    
        if (GrenadeAmount > 0)
            player.Explode(GrenadeAmount, GrenadeType, DeathReason, IsEffectOnly, IsDeath, VelocityMultiplier);
    }
}