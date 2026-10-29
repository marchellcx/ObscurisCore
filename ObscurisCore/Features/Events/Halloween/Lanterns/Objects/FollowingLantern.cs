using LabExtended.API;

using LabExtended.Core.Configs.Objects;

using LabExtended.Utilities;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Rays;
using ObscurisCore.Features.Rays.Objects;

using ProjectMER.Features;
using ProjectMER.Features.Objects;

using UnityEngine;

using Utils;

namespace ObscurisCore.Features.Events.Halloween.Lanterns.Objects;

/// <summary>
/// Represents a small lantern that hovers next to a player and smoothly follows them around.
/// The lantern is only visible to the player it follows.
/// </summary>
public class FollowingLantern : RayObject
{
    private Vector3 velocity;

    /// <summary>
    /// A static variable representing the probability, expressed as a percentage,
    /// that a shot lantern will start following the player who shot it.
    /// </summary>
    [Config("halloween", "lantern-follow-chance", "The chance (in percent) that a lantern will start following the player who shot it.")]
    public static float LanternFollowChance = 10f;

    /// <summary>
    /// A static variable that defines the scale of the following lantern.
    /// </summary>
    [Config("halloween", "lantern-follow-scale", "The scale of the lantern that follows the player.")]
    public static YamlVector3 LanternFollowScale = new(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// A static variable that defines how long (in seconds) the lantern follows the player.
    /// A value of zero or less makes the lantern follow the player until they die or leave.
    /// </summary>
    [Config("halloween", "lantern-follow-duration", "How long (in seconds) the lantern follows the player (0 or less = until the player dies or leaves).")]
    public static float LanternFollowDuration = 30f;

    /// <summary>
    /// A static variable that defines how far in front of the camera (in meters) the following lantern hovers.
    /// Negative values place the lantern behind the camera.
    /// </summary>
    [Config("halloween", "lantern-follow-forward-offset", "How far in front of the camera (in meters) the lantern hovers (negative = behind).")]
    public static float LanternFollowForwardOffset = 0.8f;

    /// <summary>
    /// A static variable that defines how far to the side of the camera (in meters) the following lantern hovers.
    /// Negative values place the lantern on the left side.
    /// </summary>
    [Config("halloween", "lantern-follow-side-offset", "How far to the right of the camera (in meters) the lantern hovers (negative = left).")]
    public static float LanternFollowSideOffset = 0.6f;

    /// <summary>
    /// A static variable that defines how high above the camera (in meters) the following lantern hovers.
    /// </summary>
    [Config("halloween", "lantern-follow-height-offset", "How high above the camera (in meters) the lantern hovers.")]
    public static float LanternFollowHeightOffset = 0.15f;

    /// <summary>
    /// A static variable that defines how quickly (in seconds) the lantern catches up with the player.
    /// Lower values make the lantern follow more closely.
    /// </summary>
    [Config("halloween", "lantern-follow-smooth-time", "Approximate time (in seconds) it takes the lantern to catch up with the player. Lower is closer.")]
    public static float LanternFollowSmoothTime = 0.12f;

    /// <summary>
    /// A static variable that defines the amplitude (in meters) of the up and down bobbing of the following lantern.
    /// </summary>
    [Config("halloween", "lantern-follow-bob-amplitude", "The amplitude (in meters) of the lantern's up and down bobbing while following.")]
    public static float LanternFollowBobAmplitude = 0.05f;

    /// <summary>
    /// A static variable that defines the speed of the up and down bobbing of the following lantern.
    /// </summary>
    [Config("halloween", "lantern-follow-bob-speed", "The speed of the lantern's up and down bobbing while following.")]
    public static float LanternFollowBobSpeed = 3f;

    /// <summary>
    /// A static variable that defines the rotation speed (in degrees per second) of the following lantern.
    /// </summary>
    [Config("halloween", "lantern-follow-spin-speed", "The rotation speed (in degrees per second) of the lantern while following.")]
    public static float LanternFollowSpinSpeed = 90f;

    /// <summary>
    /// A static variable representing the probability, expressed as a percentage,
    /// that a lantern will explode when it stops following a player.
    /// </summary>
    [Config("halloween", "lantern-follow-end-explosion-chance", "The chance (in percent) that the lantern will explode when it stops following a player.")]
    public static float LanternFollowEndExplosionChance = 10f;

    /// <summary>
    /// Represents the player being followed by this lantern.
    /// </summary>
    public ExPlayer Target;

    /// <summary>
    /// Represents the schematic object used by the following lantern.
    /// </summary>
    public SchematicObject Schematic;

    /// <summary>
    /// A variable that tracks the elapsed time in seconds since the lantern started following the player.
    /// </summary>
    public float ElapsedTime = 0f;

    /// <summary>
    /// Gets called when the parent object gets destroyed.
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();

        Target = null!;

        Schematic?.Destroy();
        Schematic = null!;
    }

    /// <summary>
    /// Gets called once per frame at the end of all Update calls when the parent object updates.
    /// </summary>
    public override void LateUpdate()
    {
        base.LateUpdate();

        if (Schematic == null || Target?.ReferenceHub == null || !Target.IsAlive)
        {
            Component.RemoveObject(this);
            return;
        }

        ElapsedTime += Time.deltaTime;

        if (LanternFollowDuration > 0f && ElapsedTime >= LanternFollowDuration)
        {
            if (LanternFollowEndExplosionChance > 0f && (LanternFollowEndExplosionChance >= 100f || WeightUtils.GetBool(LanternFollowEndExplosionChance)))
            {
                ExplosionUtils.ServerSpawnEffect(Target.Position, ItemType.GrenadeHE);

                if (WeightUtils.GetBool(50f))
                {
                    Target.Kill("Spooky!");
                }
                else
                {
                    Target.Damage(UnityEngine.Random.Range(1f, Target.Health), "Spooky!");
                }
            }
            
            Component.RemoveObject(this);
            return;
        }

        var camera = Target.CameraTransform;

        var bob = Mathf.Sin(ElapsedTime * LanternFollowBobSpeed) * LanternFollowBobAmplitude;
        
        var target = camera.position
                     + camera.forward * LanternFollowForwardOffset
                     + camera.right * LanternFollowSideOffset
                     + Vector3.up * (LanternFollowHeightOffset + bob);

        Schematic.Position = Vector3.SmoothDamp(Schematic.Position, target, ref velocity, Mathf.Max(LanternFollowSmoothTime, 0.01f));
        Schematic.Rotation = Quaternion.AngleAxis(LanternFollowSpinSpeed * Time.deltaTime, Vector3.up) * Schematic.Rotation;
    }

    /// <summary>
    /// Spawns a new FollowingLantern that follows the given player, starting at the specified position.
    /// Does nothing if the player already has a following lantern.
    /// </summary>
    /// <param name="target">The player who should be followed.</param>
    /// <param name="position">The position the lantern starts at.</param>
    /// <param name="rotation">The rotation the lantern starts with.</param>
    public static void SpawnNew(ExPlayer target, Vector3 position, Quaternion rotation)
    {
        if (target?.ReferenceHub == null)
            return;

        if (!ObjectSpawner.TrySpawnSchematic(LanternSpawner.LanternSchematic, position, rotation, LanternFollowScale.Vector, out var clone))
            return;

        var followingLantern = new FollowingLantern();

        followingLantern.Target = target;
        followingLantern.Schematic = clone;

        clone.gameObject.AddObject(followingLantern);
    }
}
