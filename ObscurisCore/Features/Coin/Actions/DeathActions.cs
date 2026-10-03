using System.ComponentModel;

using LabExtended.API;
using LabExtended.Utilities;

using LabExtended.Core.Configs.Objects;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Configs;
using PlayerRoles;

using ObscurisCore.Extensions;
using UnityEngine;
using ObscurisCore.Features.Custom.Effects.Misc;

namespace ObscurisCore.Features.Coin.Actions;

/// <summary>
/// Represents an action that can be invoked by flipping a coin and causing a player's death.
/// </summary>
public class DeathActions : CoinAction
{
    /// <summary>
    /// The type of death that will occur.
    /// </summary>
    public enum DeathType : byte
    {
        /// <summary>
        /// Explodes the player.
        /// </summary>
        Explode,
        
        /// <summary>
        /// Spawns grenades at the player's position.
        /// </summary>
        Explosive,
        
        /// <summary>
        /// Disintegrates the player.
        /// </summary>
        Disintegrate,
        
        /// <summary>
        /// Sends the player into space.
        /// </summary>
        Rocket,
        
        /// <summary>
        /// Makes the next door the player interacts with explode.
        /// </summary>
        DoorExplosion,
        
        /// <summary>
        /// Damages the player by a random percentage.
        /// </summary>
        DamagePercent,
        
        /// <summary>
        /// Zombifies the player.
        /// </summary>
        Zombify,
    }

    /// <summary>
    /// The weight for each death type.
    /// </summary>
    [Description("Weight for each death type.")]
    public Dictionary<DeathType, float> Weights { get; set; } = new()
    {
        { DeathType.Explode, 0f },
        { DeathType.Explosive, 0f },
        { DeathType.Disintegrate, 0f },
        { DeathType.Rocket, 0f },
        { DeathType.DoorExplosion, 0f },
        { DeathType.DamagePercent, 0f },
        { DeathType.Zombify, 0f },
    };

    /// <summary>
    /// Message for each death type.
    /// </summary>
    [Description("Message for each death type.")]
    public Dictionary<DeathType, string> Messages { get; set; } = new()
    {
        { DeathType.Explode, "" },
        { DeathType.Explosive, "" },
        { DeathType.Disintegrate, "" },
        { DeathType.Rocket, "" },
        { DeathType.DoorExplosion, "" },
        { DeathType.DamagePercent, "" },
        { DeathType.Zombify, "" },
    };

    [Description("The range of damage percentages to be applied to the player.")]
    public Int32Range DamagePercentRange { get; set; } = new() { MinValue = 1, MaxValue = 100 };
    
    /// <summary>
    /// The amount of grenade explosion effects to spawn during an explosion event.
    /// </summary>
    [Description("The amount of grenade explosion effects to spawn.")]
    public int ExplodeGrenadeAmount { get; set; } = 5;
    
    /// <summary>
    /// The player's ragdoll's velocity multiplier when exploding.
    /// </summary>
    [Description("The player's ragdoll's velocity multiplier when exploding.")]
    public float ExplodeVelocityMultiplier { get; set; } = 10f;
    
    /// <summary>
    /// The reason for the player's death when exploding.'
    /// </summary>
    [Description("The reason for the player's death when exploding.")]
    public string ExplodeDeathReason { get; set; } = "Exploded";

    /// <summary>
    /// The direction in which the player's ragdoll will move when disintegrating.
    /// </summary>
    [Description("The direction of the player's ragdoll when disintegrating.")]
    public YamlVector3 DisintegrateFlyDirection { get; set; } = new(Vector3.up);

    /// <summary>
    /// The number of grenades spawned under the player during an explosive death action.
    /// </summary>
    [Description("The amount of grenades to spawn under the player.")]
    public int ExplosiveGrenadeAmount { get; set; } = 10;

    /// <summary>
    /// The fuse time, in seconds, for grenades spawned during the "Explosive" death action.
    /// </summary>
    [Description("The fuse time of spawned grenades.")]
    public float ExplosiveGrenadeFuse { get; set; } = 1f;

    /// <summary>
    /// The scale of spawned grenades during explosive actions.
    /// </summary>
    [Description("The scale of spawned grenades.")]
    public YamlVector3 ExplosiveGrenadeScale { get; set; } = new(Vector3.one);
    
    /// <summary>
    /// Determines whether the death action is available to the specified player.
    /// </summary>
    /// <param name="player">The player for whom the availability of the death action is being checked.</param>
    /// <returns>
    /// A boolean value indicating whether the death action is available for the specified player.
    /// </returns>
    public override bool IsAvailable(ExPlayer player)
    {
        if (!base.IsAvailable(player))
            return false;

        if (!Weights.Any(kvp => kvp.Value > 0f))
            return false;

        return true;
    }

    /// <summary>
    /// Executes the death action for the specified player.
    /// </summary>
    /// <param name="player">The player who will be affected by the death action.</param>
    public override void Execute(ExPlayer player)
    {
        var type = Weights.GetRandomWeighted(kvp => kvp.Value);

        player.SendFormattedAlert(type.Key, Messages, false, AlertType.Info, 5f, "Coin Manager");
        
        switch (type.Key)
        {
            case DeathType.Explode:
                player.Explode(ExplodeGrenadeAmount, ItemType.GrenadeHE, ExplodeDeathReason, true, true,
                    ExplodeVelocityMultiplier);
                break;
            
            case DeathType.Disintegrate:
                player.Disintegrate(DisintegrateFlyDirection.Vector, true);
                break;
            
            case DeathType.Explosive:
                for (var i = 0; i < ExplodeGrenadeAmount; i++)
                    ExMap.SpawnProjectile(ItemType.GrenadeHE, player.Position, ExplosiveGrenadeScale.Vector,
                        Vector3.zero, player.Rotation, 0f, ExplosiveGrenadeFuse, true, true);
                break;
            
            case DeathType.Rocket:
                player.Effects.GetOrAddCustomEffect<RocketEffect>().Enable();
                break;
            
            case DeathType.DoorExplosion:
                player.Effects.GetOrAddCustomEffect<DoorInteractExplosionEffect>().Enable();
                break;
            
            case DeathType.DamagePercent:
                player.Damage(player.GetHealthAmount(DamagePercentRange.GetRandom()), "Coin");
                break;
            
            case DeathType.Zombify:
                player.Role.Set(RoleTypeId.Scp0492, RoleChangeReason.Revived, RoleSpawnFlags.AssignInventory);
                break;
        }
    }
}