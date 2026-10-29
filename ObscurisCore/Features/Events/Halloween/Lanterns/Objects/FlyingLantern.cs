using LabExtended.API;

using LabExtended.Core.Configs.Objects;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Rays;
using ObscurisCore.Features.Rays.Objects;

using ProjectMER.Features;
using ProjectMER.Features.Objects;

using UnityEngine;

namespace ObscurisCore.Features.Events.Halloween.Lanterns.Objects;

/// <summary>
/// Represents a flying lantern object used in the Halloween event.
/// This class defines the behavior and properties of a lantern that can fly toward a target player,
/// including its movement, rotation, and appearance during its flight sequence.
/// </summary>
public class FlyingLantern : RayObject
{
    /// <summary>
    /// A static variable that defines the scale of the lantern when it flies toward the player's camera.
    /// This parameter determines the size adjustment applied to the lantern during its movement
    /// towards the player's perspective, ensuring visual consistency and effect.
    /// </summary>
    [Config("halloween", "lantern-fly-scale", "The scale of the lantern when it flies into the player's camera.")]
    public static YamlVector3 LanternFlyScale = new(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// A static variable that defines how long (in seconds) it takes the lantern to reach the player's camera.
    /// </summary>
    [Config("halloween", "lantern-fly-duration", "How long (in seconds) it takes the lantern to fly into the player's camera.")]
    public static float LanternFlyDuration = 0.6f;

    /// <summary>
    /// A static variable that defines how far in front of the camera (in meters) the lantern ends its flight.
    /// </summary>
    [Config("halloween", "lantern-fly-camera-offset", "How far in front of the camera (in meters) the lantern ends its flight.")]
    public static float LanternFlyCameraOffset = 0.25f;

    /// <summary>
    /// A static variable that defines the rotation speed (in degrees per second) of the lantern while it is flying into the camera.
    /// </summary>
    [Config("halloween", "lantern-fly-spin-speed", "The rotation speed (in degrees per second) of the lantern while flying into the camera.")]
    public static float LanternFlySpinSpeed = 720f;

    /// <summary>
    /// A static variable representing the probability, expressed as a percentage,
    /// that a lantern will fly into the player's camera during gameplay.
    /// </summary>
    [Config("halloween", "lantern-fly-chance", "The chance (in percent) that a lantern will fly into the player's camera.")]
    public static float LanternFlyChance = 10f;
    
    /// <summary>
    /// Gets a value indicating whether the attached network behaviours should be hidden.
    /// </summary>
    public override bool ShouldHideAttachedBehaviours { get => true; }

    /// <summary>
    /// Represents the target player associated with the flying lantern.
    /// </summary>
    public ExPlayer Target;

    /// <summary>
    /// Represents the schematic object used by the flying lantern during its functionality.
    /// </summary>
    public SchematicObject Schematic;

    /// <summary>
    /// The initial starting position of the flying lantern in world space coordinates.
    /// </summary>
    public Vector3 StartPosition = Vector3.zero;

    /// <summary>
    /// A variable that tracks the elapsed time in seconds since the lantern's flight began.
    /// </summary>
    public float ElapsedTime = 0f;

    /// <summary>
    /// Gets called when the object gets added.
    /// </summary>
    public override void Start()
    {
        base.Start();
        
        foreach (var toy in Schematic.AdminToyBases)
            AttachBehaviour(toy);
        
        ShowAttachedBehaviours(Target);
    }

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

        if (Schematic != null && Target?.ReferenceHub != null)
        {
            ElapsedTime += Time.deltaTime;

            var duration = Mathf.Max(LanternFlyDuration, 0.01f);
            var progress = Mathf.Clamp01(ElapsedTime / duration);

            if (progress >= 1f)
            {
                Component.RemoveObject(this);
                return;
            }

            var camera = Target.CameraTransform;
            var target = camera.position + camera.forward * LanternFlyCameraOffset;
            var eased = progress * progress;

            Schematic.Position = Vector3.Lerp(StartPosition, target, eased);
            Schematic.Rotation = Quaternion.AngleAxis(LanternFlySpinSpeed * Time.deltaTime, Vector3.up) * Schematic.Rotation;
        }
    }

    /// <summary>
    /// Spawns a new FlyingLantern object at the specified position and rotation,
    /// targeting the given player.
    /// </summary>
    /// <param name="target">The player who is the target of the FlyingLantern.</param>
    /// <param name="position">The spawn position of the FlyingLantern.</param>
    /// <param name="rotation">The spawn rotation of the FlyingLantern.</param>
    public static void SpawnNew(ExPlayer target, Vector3 position, Quaternion rotation)
    {
        if (!ObjectSpawner.TrySpawnSchematic(LanternSpawner.LanternSchematic, position, rotation, LanternFlyScale.Vector, out var clone))
            return;

        var flyingLantern = new FlyingLantern();

        flyingLantern.Target = target;
        flyingLantern.Schematic = clone;
        flyingLantern.StartPosition = position;
        
        clone.gameObject.AddObject(flyingLantern);
    }
}