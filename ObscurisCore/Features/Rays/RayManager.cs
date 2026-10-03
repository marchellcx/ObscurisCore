using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Pickups;

using LabExtended.API;
using LabExtended.Extensions;
using LabExtended.Utilities.Update;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Rays.Objects;

using UnityEngine;

namespace ObscurisCore.Features.Rays;

/// <summary>
/// Provides static methods and events for performing raycast-based searches and interactions in the game, such as
/// detecting item pickups, doors, and players within a specified distance from a player.
/// </summary>
public static class RayManager
{
    private static int layerMask;
    private static int remainingFrames;

    private static PlayerUpdateComponent component = PlayerUpdateComponent.Create();

    /// <summary>
    /// Occurs when a hit is successfully detected by an ExPlayer during a raycast operation.
    /// </summary>
    public static event Action<ExPlayer, RaycastHit> HitSuccess;

    /// <summary>
    /// Event triggered after all raycast operations and associated logic are completed for the current frame.
    /// </summary>
    public static event Action? FrameFinished;

    /// <summary>
    /// Determines the number of frames to skip between consecutive raycast operations in the RayManager.
    /// A value of -1 disables frame skipping, making raycasts occur every frame.
    /// </summary>
    [Config("rayManager", "frameSkip",
        "Number of frames to skip between raycast operations. Set to -1 to disable frame skipping.")]
    public static int FrameSkip { get; set; } = 2;

    /// <summary>
    /// Gets or sets the forward offset value used in raycast operations.
    /// This offset determines how far forward from the source position the raycast begins.
    /// Modifying this value can adjust the starting point of object detection relative to the raycast origin.
    /// </summary>
    [Config("rayManager", "forwardOffset", "Forward offset for raycast operations.")]
    public static float ForwardOffset { get; set; } = 0.1f;

    /// <summary>
    /// Specifies the maximum distance for raycast operations performed by the RayManager.
    /// Objects beyond this distance will not be detected during raycast-driven interactions.
    /// </summary>
    [Config("rayManager", "distance", "Maximum distance to check for collisions.")]
    public static float MaxDistance { get; set; } = 100f;

    /// <summary>
    /// Represents the collection of layer names or identifiers used for filtering raycast operations.
    /// These layers determine which objects are considered during collision checks performed by the raycasting system.
    /// </summary>
    [Config("rayManager", "layers", "Layers to include in raycast operations.")]
    public static string[] Layers { get; set; } =
    [
        "Default",
        "TransparentFX",
        "Ignore Raycast",
        "Water",
        "UI"
    ];

    /// <summary>
    /// Attempts to find an item pickup within a specified distance from the player and returns whether a pickup was
    /// found.
    /// </summary>
    /// <param name="source">The player from which the search for an item pickup originates.</param>
    /// <param name="distance">The maximum distance, in units, to search for an item pickup from the player's position.</param>
    /// <param name="mask">An optional layer mask used to filter which objects are considered during the search. If null, a default
    /// mask is used.</param>
    /// <param name="pickup">When this method returns, contains the found item pickup if one is located within the specified distance;
    /// otherwise, null.</param>
    /// <returns>true if an item pickup is found within the specified distance; otherwise, false.</returns>
    public static bool TryGetPickup(this ExPlayer source, float distance, int? mask, out ItemPickupBase? pickup)
    {
        pickup = null;

        if (!TryCast(source, distance, ForwardOffset, mask ?? layerMask, out var hit))
            return false;

        return hit.collider.gameObject.TryFindComponent<ItemPickupBase>(out pickup);
    }

    /// <summary>
    /// Attempts to find a door within the specified distance from the player and returns information about the door
    /// if found.
    /// </summary>
    /// <param name="source">The player instance from which the search for a door is performed.</param>
    /// <param name="distance">The maximum distance, in units, to search for a door from the player's position.</param>
    /// <param name="mask">An optional layer mask used to filter which objects are considered during the search. If null, a default
    /// mask is used.</param>
    /// <param name="target">When this method returns, contains the found door variant if a door is detected within the specified
    /// distance; otherwise, null.</param>
    /// <returns>true if a door is found within the specified distance; otherwise, false.</returns>
    public static bool TryGetDoor(this ExPlayer source, float distance, int? mask, out DoorVariant? target)
    {
        target = null;

        if (!TryCast(source, distance, ForwardOffset, mask ?? layerMask, out var hit))
            return false;

        return hit.collider.gameObject.TryFindComponent<DoorVariant>(out target);
    }

    /// <summary>
    /// Attempts to find another player within the specified distance from the source player, using an optional
    /// layer mask.
    /// </summary>
    /// <param name="source">The source player from which to search for another player.</param>
    /// <param name="distance">The maximum distance, in units, within which to search for a target player.</param>
    /// <param name="mask">An optional layer mask used to filter which objects are considered during the search. If null, a default
    /// mask is used.</param>
    /// <param name="target">When this method returns, contains the found player if one is detected within the specified distance and
    /// mask; otherwise, null.</param>
    /// <returns>true if a target player is found within the specified distance and mask; otherwise, false.</returns>
    public static bool TryGetPlayer(this ExPlayer source, float distance, int? mask, out ExPlayer? target)
    {
        target = null;

        if (!TryCast(source, distance, ForwardOffset, mask ?? layerMask, out var hit))
            return false;

        if (!hit.collider.gameObject.TryFindComponent<ReferenceHub>(out var hub))
            return false;

        return ExPlayer.TryGet(hub, out target);
    }

    /// <summary>
    /// Attempts to perform a raycast from the player's camera position in the forward direction, optionally
    /// offsetting the direction, and returns whether a collider was hit.
    /// </summary>
    /// <remarks>The method modifies the ray direction by the specified forward offset before
    /// performing the raycast. The hit information is only valid if the method returns true.</remarks>
    /// <param name="player">The player from whose camera position and orientation the raycast is performed.</param>
    /// <param name="distance">The maximum distance, in world units, that the ray should check for collisions.</param>
    /// <param name="forwardOffset">The amount by which to offset the ray's forward direction along each axis before casting. Must be zero or
    /// positive.</param>
    /// <param name="layerMask">A bitmask that specifies which layers to include in the raycast collision check.</param>
    /// <param name="hit">When this method returns, contains information about the raycast hit if a collider was detected; otherwise,
    /// contains default values.</param>
    /// <returns>true if the raycast hit a collider; otherwise, false.</returns>
    public static bool TryCast(this ExPlayer player, float distance, float forwardOffset, int layerMask,
        out RaycastHit hit)
    {
        hit = default;

        var position = player.Position.Position;
        var direction = player.CameraTransform.forward;

        if (forwardOffset > 0f)
        {
            if (direction.x > 0f)
                direction.x += forwardOffset;

            if (direction.y > 0f)
                direction.y += forwardOffset;

            if (direction.z > 0f)
                direction.z += forwardOffset;
        }

        if (!Physics.Raycast(position, direction, out hit, distance, layerMask))
            return false;

        if (hit.collider == null)
            return false;

        return true;
    }

    /// <summary>
    /// Adds a specified <see cref="RayObject"/> to a <see cref="GameObject"/>, ensuring the object is associated
    /// with a <see cref="RayComponent"/>. If the <see cref="GameObject"/> does not already have a <see cref="RayComponent"/>
    /// attached, one is automatically added.
    /// </summary>
    /// <param name="gameObject">The <see cref="GameObject"/> to which the <see cref="RayObject"/> will be added.</param>
    /// <param name="rayObject">The <see cref="RayObject"/> to associate with the <see cref="GameObject"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameObject"/> or <paramref name="rayObject"/> is null.</exception>
    public static void AddObject(this GameObject gameObject, RayObject rayObject)
    {
        if (gameObject == null)
            throw new ArgumentNullException(nameof(gameObject));

        if (rayObject is null)
            throw new ArgumentNullException(nameof(rayObject));

        if (gameObject.TryGetComponent<RayComponent>(out var rayComponent))
        {
            rayComponent.AddObject(rayObject);
            return;
        }

        rayComponent = gameObject.AddComponent<RayComponent>();
        rayComponent.AddObject(rayObject);
    }

    /// <summary>
    /// Removes a specified ray object from a game object's RayComponent and destroys the RayComponent if no objects remain.
    /// </summary>
    /// <param name="gameObject">The game object from which the specified ray object will be removed.</param>
    /// <param name="rayObject">The ray object to be removed from the game object's RayComponent.</param>
    /// <returns>true if the ray object was successfully removed; otherwise, false if the game object does not have a RayComponent or the component is invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameObject"/> or <paramref name="rayObject"/> is null.</exception>
    public static bool RemoveObject(this GameObject gameObject, RayObject rayObject)
    {
        if (gameObject == null)
            throw new ArgumentNullException(nameof(gameObject));

        if (rayObject is null)
            throw new ArgumentNullException(nameof(rayObject));

        if (!gameObject.TryGetComponent<RayComponent>(out var rayComponent))
            return false;

        rayComponent.RemoveObject(rayObject);

        if (rayComponent.Objects.Count < 1)
            UnityEngine.Object.Destroy(rayComponent);

        return true;
    }

    private static void OnUpdate()
    {
        if (ExPlayer.Count < 1)
            return;

        if (FrameSkip > 0)
        {
            if (remainingFrames > 0)
            {
                remainingFrames--;
                return;
            }

            remainingFrames = FrameSkip;
        }

        for (var i = 0; i < ExPlayer.Count; i++)
        {
            var player = ExPlayer.Players[i];

            if (player?.ReferenceHub == null || !player.IsAlive)
                continue;

            if (!TryCast(player, MaxDistance, ForwardOffset, layerMask, out var hit))
                continue;

            HitSuccess?.InvokeSafe(player, hit);

            if (!hit.collider.gameObject.TryFindComponent<RayComponent>(out var component))
                continue;

            component.OnHit(player);
        }

        FrameFinished?.InvokeSafe();
    }

    private static void Initialize()
    {
        if (FrameSkip == -1)
            return;

        layerMask = LayerMask.GetMask(Layers);

        component.OnLateUpdate += OnUpdate;
    }
}