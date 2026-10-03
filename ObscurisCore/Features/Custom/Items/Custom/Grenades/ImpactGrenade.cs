using ObscurisCore.Features.Custom.Items.Custom;

namespace ObscurisCore.Features.Custom.Items.Custom.Grenades;

/// <summary>
/// Represents a custom grenade that explodes when it collides with something.
/// </summary>
public class ImpactGrenade : SpawnableCustomProjectile
{
    /// <summary>
    /// The unique identifier for the grenade.
    /// </summary>
    public override string Id { get; } = "impact_grenade";
    
    /// <summary>
    /// The name of the grenade.
    /// </summary>
    public override string Name { get; } = "Impact Grenade";

    /// <summary>
    /// The type of item that the grenade can be picked up as.
    /// </summary>
    public override ItemType PickupType { get; set; } = ItemType.GrenadeHE;
    
    /// <summary>
    /// The type of item that the grenade can be stored in.
    /// </summary>
    public override ItemType InventoryType { get; set; } = ItemType.GrenadeHE;

    /// <summary>
    /// Whether the grenade should explode when it collides with something.
    /// </summary>
    public override bool ExplodeOnCollision { get; set; } = true;
}