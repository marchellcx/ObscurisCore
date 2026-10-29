using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Pickups;

using LabExtended.API;
using LabExtended.Extensions;

using LabExtended.Events;
using LabExtended.Events.Player;

using LabExtended.Utilities.Update;

using MEC;

using NiveraAPI.IO.Configs;
using NiveraAPI.Pooling;

using ObscurisCore.Features.Rays.Enums;
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
    
    private static RaycastHit[] frameBuffer;
    
    private static FixedArrayPool<RaycastHit> bufferPool;

    internal static List<RayObject> allObjects = new();
    internal static PlayerUpdateComponent component = PlayerUpdateComponent.Create();

    /// <summary>
    /// Occurs when a hit is successfully detected by an ExPlayer during a raycast operation.
    /// </summary>
    public static event Action<ExPlayer, RaycastHit>? HitSuccess;

    /// <summary>
    /// Event triggered after all raycast operations and associated logic are completed for the current frame.
    /// </summary>
    public static event Action? FrameFinished;

    /// <summary>
    /// Determines the number of frames to skip between consecutive raycast operations in the RayManager.
    /// A value of -1 disables frame skipping, making raycasts occur every frame.
    /// </summary>
    [Config("ray-manager", "frame-skip", "Number of frames to skip between raycast operations. Set to -1 to disable raycasts.")]
    public static int FrameSkip = 2;

    /// <summary>
    /// Specifies the maximum distance for raycast operations performed by the RayManager.
    /// Objects beyond this distance will not be detected during raycast-driven interactions.
    /// </summary>
    [Config("ray-manager", "distance", "Maximum distance to check for collisions.")]
    public static float MaxDistance = 100f;

    /// <summary>
    /// Specifies the maximum number of collision results to return during raycast operations conducted by the RayManager.
    /// </summary>
    [Config("ray-manager", "max-hits", "Maximum number of hits to return from raycast operations.")]
    public static int MaxHits = 10;

    /// <summary>
    /// Indicates whether raycast operations should consider all layers in the scene.
    /// If set to <c>true</c>, all layers are included regardless of specific layer configuration.
    /// If set to <c>false</c>, raycast operations will use only the specified layers.
    /// </summary>
    [Config("ray-manager", "use-all-layers", "Whether to use all layers for raycast operations.")]
    public static bool UseAllLayers = true;

    /// <summary>
    /// Represents the collection of layer names or identifiers used for filtering raycast operations.
    /// These layers determine which objects are considered during collision checks performed by the raycasting system.
    /// </summary>
    [Config("ray-manager", "layers", "Layers to include in raycast operations.")]
    public static string[] Layers =
    [
        "Default",
        "TransparentFX",
        "Ignore Raycast",
        "Water",
        "UI"
    ];

    /// <summary>
    /// Provides a read-only list of all currently registered RayObject instances managed by the RayManager.
    /// </summary>
    public static IReadOnlyList<RayObject> AllObjects => allObjects;

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
    public static bool TryRayCastPickup(this ExPlayer source, float? distance, int? mask, out ItemPickupBase? pickup)
        => source.TryRayCastComponent<ItemPickupBase>(distance, mask, null, null, out pickup);

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
    public static bool TryRayCastDoor(this ExPlayer source, float? distance, int? mask, out DoorVariant? target)
    {
        DoorVariant? ButtonSelector(RaycastHit hit)
        {
            if (hit.collider.gameObject.TryFindComponent<BasicDoorButton>(out var button) && button.ParentDoor != null)
                return button.ParentDoor;
            
            return null;
        }
        
        return source.TryRayCastComponent<DoorVariant>(distance, mask, null, ButtonSelector, out target);
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
    public static bool TryRayCastPlayer(this ExPlayer source, float? distance, int? mask, out ExPlayer? target)
    {
        target = null;

        if (!source.TryRayCastComponent<ReferenceHub>(distance, mask, null, null, out var referenceHub))
            return false;

        return ExPlayer.TryGet(referenceHub, out target);
    }

    /// <summary>
    /// Attempts to perform a raycast operation from the player's position in the direction of the player's camera,
    /// and determines if a component of the specified type is found. If found, returns the associated component.
    /// </summary>
    /// <typeparam name="T">The type of the component to search for during the raycast.</typeparam>
    /// <param name="player">The player whose position and camera direction are used to perform the raycast.</param>
    /// <param name="distance">The maximum distance to perform the raycast. If null, a default maximum distance is used.</param>
    /// <param name="mask">An optional layer mask specifying which layers should be included in the raycast. If null, a default mask is used.</param>
    /// <param name="predicate">An optional predicate to filter the raycast results by custom criteria. If null, no additional filtering is done.</param>
    /// <param name="componentSelector">A function that extracts the desired component from a raycast hit, if one exists.</param>
    /// <param name="component">When this method returns, contains the found component of type <typeparamref name="T"/> if a match is located; otherwise, the default value for the specified type.</param>
    /// <returns>true if a component of type <typeparamref name="T"/> is found that matches the filtering criteria; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="player"/> is null or does not have a valid reference hub.</exception>
    public static bool TryRayCastComponent<T>(this ExPlayer player, float? distance, int? mask, Predicate<RaycastHit>? predicate, Func<RaycastHit, T?>? componentSelector, out T component)
    {
        var buffer = bufferPool.Rent();

        try
        {
            var result = player.TryRayCastComponent(buffer, distance, mask, predicate, componentSelector, out _, out _, out component);

            bufferPool.Return(buffer);
            return result;
        }
        finally
        {
            bufferPool.Return(buffer);
        }
    }

    /// <summary>
    /// Attempts to perform a raycast operation from the player's position in the direction of the player's camera,
    /// and determines if a component of the specified type is found. If found, returns the associated component.
    /// </summary>
    /// <typeparam name="T">The type of the component to search for during the raycast.</typeparam>
    /// <param name="player">The player whose position and camera direction are used to perform the raycast.</param>
    /// <param name="buffer">The buffer to use for storing the raycast results.</param>
    /// <param name="distance">The maximum distance to perform the raycast. If null, a default maximum distance is used.</param>
    /// <param name="mask">An optional layer mask specifying which layers should be included in the raycast. If null, a default mask is used.</param>
    /// <param name="predicate">An optional predicate to filter the raycast results by custom criteria. If null, no additional filtering is done.</param>
    /// <param name="componentSelector">A function that extracts the desired component from a raycast hit, if one exists.</param>
    /// <param name="count">The amount of successful raycast hits.</param>
    /// <param name="index">When this method returns, contains the index of the found component of type <typeparamref name="T"/> if a match is located; otherwise, -1.</param>
    /// <param name="component">When this method returns, contains the found component of type <typeparamref name="T"/> if a match is located; otherwise, the default value for the specified type.</param>
    /// <returns>true if a component of type <typeparamref name="T"/> is found that matches the filtering criteria; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="player"/> is null or does not have a valid reference hub.</exception>
    public static bool TryRayCastComponent<T>(this ExPlayer player, RaycastHit[] buffer, float? distance, int? mask, Predicate<RaycastHit>? predicate, Func<RaycastHit, T?>? componentSelector, out int count, out int index, out T component)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        index = -1;
        component = default!;

        var isHub = typeof(T) == typeof(ReferenceHub);

        var position = player.Position.Position;
        var direction = player.CameraTransform.forward;
        
        count = Physics.RaycastNonAlloc(position, direction, buffer, distance ?? MaxDistance, mask ?? layerMask);
        
        if (count < 1)
        {
            return false;
        }

        for (var x = 0; x < count; x++)
        {
            var raycast = buffer[x];

            if (raycast.collider == null)
            {
                continue;
            }

            if (predicate != null && !predicate(raycast))
            {
                continue;
            }

            if (raycast.collider.gameObject.TryFindComponent<ReferenceHub>(out var hub))
            {
                if (isHub && hub != player.ReferenceHub)
                {
                    component = (T)(object)hub; // kinda ugly but whatevs

                    index = x;
                    return true;
                }

                if (hub == player.ReferenceHub)
                {
                    continue;
                }
            }

            if (raycast.collider.gameObject.TryFindComponent<HitboxIdentity>(out var hitboxIdentity) && hitboxIdentity.TargetHub != null)
            {
                if (isHub && hitboxIdentity.TargetHub != player.ReferenceHub)
                {
                    component = (T)(object)hitboxIdentity.TargetHub;

                    index = x;
                    return true;
                }

                if (hitboxIdentity.TargetHub == player.ReferenceHub)
                {
                    component = (T)(object)hitboxIdentity.TargetHub;

                    index = x;
                    return true;
                }

                if (hitboxIdentity.TargetHub == player.ReferenceHub)
                {
                    continue;
                }
            }

            if (componentSelector != null)
            {
                component = componentSelector(raycast)!;

                if (component != null)
                {
                    index = x;
                    return true;
                }

                continue;
            }

            if (!raycast.collider.gameObject.TryFindComponent(out component))
            {
                continue;
            }
            
            index = x;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to cast a ray from the player's position in the direction they are facing, within a specified distance,
    /// and determines if a hit occurs that satisfies an optional conditional predicate.
    /// </summary>
    /// <param name="player">The player initiating the raycast.</param>
    /// <param name="distance">The maximum distance the raycast can travel.</param>
    /// <param name="layerMask">An optional layer mask used to filter objects for the raycast. If null, a default mask is used.</param>
    /// <param name="predicate">An optional condition that the hit must satisfy. If null, all hits will be considered valid.</param>
    /// <param name="hit">When this method returns, contains the RaycastHit data if a successful hit occurs; otherwise, default.</param>
    /// <returns>true if a valid hit is detected based on the provided parameters; otherwise, false.</returns>
    public static bool TryRayCast(this ExPlayer player, float? distance, int? layerMask, Predicate<RaycastHit>? predicate, out int count, out RaycastHit hit)
    {
        var buffer = bufferPool.Rent();

        try
        {
            var result = player.TryRayCast(buffer, distance, layerMask, predicate, out count, out hit);
            return result;
        }
        finally
        {
            bufferPool.Return(buffer);
        }
    }

    /// <summary>
    /// Attempts to cast a ray from the player's position in the direction they are facing, within a specified distance,
    /// and determines if a hit occurs that satisfies an optional conditional predicate.
    /// </summary>
    /// <param name="player">The player initiating the raycast.</param>
    /// <param name="buffer">The buffer to store the hits in.</param>
    /// <param name="distance">The maximum distance the raycast can travel.</param>
    /// <param name="layerMask">An optional layer mask used to filter objects for the raycast. If null, a default mask is used.</param>
    /// <param name="predicate">An optional condition that the hit must satisfy. If null, all hits will be considered valid.</param>
    /// <param name="count">When this method returns, contains the number of hits detected.</param>
    /// <param name="hit">When this method returns, contains the RaycastHit data if a successful hit occurs; otherwise, default.</param>
    /// <returns>true if a valid hit is detected based on the provided parameters; otherwise, false.</returns>
    public static bool TryRayCast(this ExPlayer player, RaycastHit[] buffer, float? distance, int? layerMask, Predicate<RaycastHit>? predicate, out int count, out RaycastHit hit)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));
        
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        
        hit = default;

        var position = player.Position.Position;
        var direction = player.CameraTransform.forward;

        count = Physics.RaycastNonAlloc(position, direction, buffer, distance ?? MaxDistance, layerMask ?? RayManager.layerMask);

        if (count < 1)
            return false;
        
        for (var x = 0; x < count; x++)
        {
            var raycast = buffer[x];

            if (raycast.collider == null)
                continue;

            if (predicate != null && !predicate(raycast))
                continue;

            if (raycast.collider.gameObject.TryFindComponent<ReferenceHub>(out var hub)
                && hub == player.ReferenceHub)
                continue;

            if (raycast.collider.gameObject.TryFindComponent<HitboxIdentity>(out var hitboxIdentity)
                && hitboxIdentity.TargetHub != null
                && hitboxIdentity.TargetHub == player.ReferenceHub)
                continue;

            hit = raycast;
            return true;
        }

        return false;
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

        var layer = 1 << gameObject.layer;
        
        if (!UseAllLayers)
            layerMask |= layer;

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

    private static void OnShooting(PlayerShootingFirearmEventArgs args)
    {
        if (args.Hit.collider != null)
        {
            if (args.Hit.collider.TryGetComponent<RayComponent>(out var rayComponent)
                || args.Hit.collider.gameObject.TryFindComponent<RayComponent>(out rayComponent))
            {
                rayComponent.CallHit(args.Player, ObjectHitSource.Firearm, args.TargetDamage);
                return;
            }
        }
        
        foreach (var hit in args.Hitscan.Obstacles)
        {
            if (hit.Hit.collider != null)
            {
                if (hit.Hit.collider.TryGetComponent<RayComponent>(out var rayComponent)
                    || hit.Hit.collider.gameObject.TryFindComponent<RayComponent>(out rayComponent))
                {
                    rayComponent.CallHit(args.Player, ObjectHitSource.Firearm, args.TargetDamage);
                    return;
                }
            }
        }
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

            if (!player.TryRayCast(frameBuffer, null, null, null, out var count, out _))
                continue;
            
            if (count < 1)
                continue;

            for (var x = 0; x < count; x++)
            {
                var hit = frameBuffer[x];

                if (hit.collider != null)
                {
                    HitSuccess?.InvokeSafe(player, hit);

                    if (hit.collider.gameObject.TryFindComponent<RayComponent>(out var rayComponent))
                    {
                        rayComponent.OnHit(player);
                    }
                }
            }
        }

        FrameFinished?.InvokeSafe();
    }

    private static void OnJoined(ExPlayer player)
    {
        Timing.CallDelayed(0.2f, () =>
        {
            allObjects.ForEach(obj =>
            {
                obj.UpdateBehaviourVisibility(player);
            });
        });
    }

    private static void OnRestarting()
    {
        var components = UnityEngine.Object.FindObjectsByType<RayComponent>(FindObjectsSortMode.None);

        foreach (var component in components)
        {
            if (component != null)
            {
                UnityEngine.Object.Destroy(component);
            }
        }
        
        allObjects.Clear();
        
        layerMask = LayerMask.GetMask(Layers);
    }

    private static void Initialize()
    {
        ExPlayerEvents.Joined += OnJoined;
        ExPlayerEvents.ShootingFirearm += OnShooting;
        
        ExRoundEvents.Restarting += OnRestarting;

        if (UseAllLayers)
            layerMask = Physics.AllLayers;
        else
            layerMask = LayerMask.GetMask(Layers);
        
        frameBuffer = new RaycastHit[MaxHits];
        bufferPool = new FixedArrayPool<RaycastHit>(MaxHits, 10);
        
        if (FrameSkip != -1)
            component.OnLateUpdate += OnUpdate;
    }
}