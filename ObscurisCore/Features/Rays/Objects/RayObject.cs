using LabExtended.API;
using LabExtended.Extensions;

using LabExtended.Events.Player;

using Mirror;
using ObscurisCore.Features.Rays.Enums;
using UnityEngine;

namespace ObscurisCore.Features.Rays.Objects;

/// <summary>
/// Wrapper for raycast hit objects.
/// </summary>
public class RayObject
{
    private bool isGloballyHidden;
    
    /// <summary>
    /// Gets the parent RayComponent.
    /// </summary>
    public RayComponent Component { get; internal set; }

    /// <summary>
    /// Gets the Transform component associated with the RayObject's underlying GameObject.
    /// </summary>
    public Transform Transform => Component.transform;

    /// <summary>
    /// Gets the associated Unity GameObject for this ray object.
    /// </summary>
    public GameObject GameObject => Component.gameObject;

    /// <summary>
    /// Gets or sets the parent GameObject of the current RayObject.
    /// </summary>
    public GameObject? Parent
    {
        get => Transform.parent?.gameObject;
        set => Transform.parent = value?.transform;
    }

    /// <summary>
    /// Gets or sets the parent transform of the current object.
    /// </summary>
    public Transform TransformParent
    {
        get => Transform.parent;
        set => Transform.parent = value;
    }

    /// <summary>
    /// Gets or sets the local scale of the associated transform.
    /// </summary>
    public Vector3 Scale
    {
        get => Component.transform.localScale;
        set => Component.transform.localScale = value;
    }

    /// <summary>
    /// Gets or sets the world-space position of the associated RayComponent's transform.
    /// </summary>
    public Vector3 Position
    {
        get => Component.transform.position;
        set => Component.transform.position = value;
    }

    /// <summary>
    /// Gets or sets the local position of the transform relative to its parent.
    /// </summary>
    public Vector3 LocalPosition
    {
        get => Component.transform.localPosition;
        set => Component.transform.localPosition = value;
    }

    /// <summary>
    /// Gets or sets the world rotation of the object.
    /// </summary>
    public Quaternion Rotation
    {
        get => Component.transform.rotation;
        set => Component.transform.rotation = value;
    }

    /// <summary>
    /// Gets or sets the local rotation of the RayObject's transform.
    /// </summary>
    public Quaternion LocalRotation
    {
        get => Component.transform.localRotation;
        set => Component.transform.localRotation = value;
    }

    /// <summary>
    /// Gets the list of attached network behaviours associated with the ray object.
    /// </summary>
    public List<NetworkBehaviour> AttachedBehaviours { get; } = new();

    /// <summary>
    /// Stores a mapping of user identifiers to lists of hidden NetworkBehaviours.
    /// This property is used to track which NetworkBehaviours should be hidden
    /// from specific players.
    /// </summary>
    public Dictionary<string, List<NetworkBehaviour>> HiddenBehaviours { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the attached network behaviours should be hidden.
    /// </summary>
    public virtual bool ShouldHideAttachedBehaviours { get; }
    
    /// <summary>
    /// Gets a value indicating whether the attached network behaviours should be destroyed.
    /// </summary>
    public virtual bool ShouldDestroyAttachedBehaviours { get; }

    /// <summary>
    /// Indicates whether the associated component has been destroyed or is no longer available.
    /// </summary>
    public bool IsDestroyed => Component == null;

    /// <summary>
    /// Gets a value indicating whether the object is globally hidden,
    /// preventing it from being visible or interactable within the system.
    /// </summary>
    public bool IsGloballyHidden => isGloballyHidden;

    /// <summary>
    /// Gets the list of players who are currently looking at this object.
    /// </summary>
    public IReadOnlyList<LookingPlayer> Players => Component.Players;

    /// <summary>
    /// Gets called when the object gets added.
    /// </summary>
    public virtual void Start()
    {
        RayManager.allObjects.Add(this);
        
        RayManager.component.OnUpdate += Update;
        RayManager.component.OnLateUpdate += LateUpdate;
        RayManager.component.OnFixedUpdate += FixedUpdate;
    }

    /// <summary>
    /// Gets called when the parent object gets destroyed.
    /// </summary>
    public virtual void Destroy()
    {
        RayManager.allObjects.Remove(this);
        
        RayManager.component.OnUpdate -= Update;
        RayManager.component.OnLateUpdate -= LateUpdate;
        RayManager.component.OnFixedUpdate -= FixedUpdate;
        
        if (ShouldDestroyAttachedBehaviours)
            AttachedBehaviours.ForEach(networkBehaviour => NetworkServer.Destroy(networkBehaviour.gameObject));
        
        AttachedBehaviours.Clear();
        
        foreach (var kvp in HiddenBehaviours)
            kvp.Value.Clear();
        
        HiddenBehaviours.Clear();
    }

    /// <summary>
    /// Determines the visibility of attached behaviours for the specified player.
    /// </summary>
    /// <param name="player">The player for whom to evaluate the visibility of attached behaviours.</param>
    /// <returns>
    /// A boolean value indicating whether the attached behaviours should be hidden (true to hide; false otherwise).
    /// </returns>
    public virtual bool BehaviourVisibilityPredicate(ExPlayer player)
        => true;

    /// <summary>
    /// Determines whether a specific network behaviour attached to the object is visible to a given player.
    /// </summary>
    /// <param name="player">The player for whom the visibility of the behaviour is being evaluated.</param>
    /// <param name="networkBehaviour">The network behaviour being checked for visibility.</param>
    /// <returns>True if the behaviour is visible to the specified player; otherwise, false.</returns>
    public bool IsBehaviourVisible(ExPlayer player, NetworkBehaviour networkBehaviour)
    {
        if (player?.ReferenceHub == null)
            return false;

        if (networkBehaviour == null)
            return false;

        if (!AttachedBehaviours.Contains(networkBehaviour))
            return true;
        
        if (isGloballyHidden)
            return false;

        if (HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList) && hiddenList.Contains(networkBehaviour))
            return false;

        return true;
    }

    /// <summary>
    /// Sends a message to hide all attached network behaviors for the specified player,
    /// ensuring they are no longer visible to that player.
    /// </summary>
    /// <param name="player">The player for whom the attached behaviors should be hidden.
    /// This parameter cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="player"/> is null or if the player's reference hub is null.
    /// </exception>
    public void HideAttachedBehaviours(ExPlayer player)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        if (!HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
            HiddenBehaviours.Add(player.UserId, hiddenList = new());
        
        foreach (var behaviour in AttachedBehaviours)
        {
            hiddenList.AddUnique(behaviour);
            
            player.Connection.Send(new ObjectHideMessage() { netId = behaviour.netId });
        }
    }

    /// <summary>
    /// Hides the attached behaviours of the object for players matching the given predicate.
    /// </summary>
    /// <param name="predicate">The condition used to determine which players should have the behaviours hidden.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided predicate is null.</exception>
    public void HideAttachedBehaviours(Predicate<ExPlayer> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        foreach (var behaviour in AttachedBehaviours)
        {
            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub == null)
                    continue;

                if (!predicate(player))
                    continue;
                
                if (!HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                    HiddenBehaviours.Add(player.UserId, hiddenList = new());

                hiddenList.AddUnique(behaviour);
                
                player.Connection.Send(new ObjectHideMessage() { netId = behaviour.netId });
            }
        }
    }

    /// <summary>
    /// Sets the global visibility state for the attached behaviours to hidden
    /// and sends a message to all connected players, instructing them to hide
    /// the associated network behaviours of this object.
    /// </summary>
    public void HideAttachedBehaviours()
    {
        isGloballyHidden = true;
        
        foreach (var behaviour in AttachedBehaviours)
        {
            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub == null)
                    continue;
                
                if (!HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                    HiddenBehaviours.Add(player.UserId, hiddenList = new());

                hiddenList.AddUnique(behaviour);
                
                player.Connection.Send(new ObjectHideMessage() { netId = behaviour.netId });
            }
        }
    }

    /// <summary>
    /// Hides a specific network behaviour for a specified player, preventing it from being visible.
    /// </summary>
    /// <param name="target">The player for whom the network behaviour should be hidden.</param>
    /// <param name="networkBehaviour">The network behaviour to hide from the target player.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either the target player or the network behaviour is null.
    /// </exception>
    public void HideAttachedBehaviour(ExPlayer target, NetworkBehaviour networkBehaviour)
    {
        if (target?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(target));

        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        if (!HiddenBehaviours.TryGetValue(target.UserId, out var hiddenList))
            HiddenBehaviours.Add(target.UserId, hiddenList = new());

        hiddenList.Add(networkBehaviour);

        target.Connection.Send(new ObjectHideMessage() { netId = networkBehaviour.netId });
    }

    /// <summary>
    /// Hides a specific network behaviour for all players matching the provided predicate.
    /// </summary>
    /// <param name="predicate">The condition used to determine which players the behaviour should be hidden from.</param>
    /// <param name="networkBehaviour">The network behaviour to be hidden.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="predicate"/> or <paramref name="networkBehaviour"/> is null.</exception>
    public void HideAttachedBehaviour(Predicate<ExPlayer> predicate, NetworkBehaviour networkBehaviour)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));

        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        foreach (var player in ExPlayer.Players)
        {
            if (player?.ReferenceHub == null)
                continue;

            if (!predicate(player))
                continue;
            
            if (!HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                HiddenBehaviours.Add(player.UserId, hiddenList = new());
            
            hiddenList.Add(networkBehaviour);

            player.Connection.Send(new ObjectHideMessage() { netId = networkBehaviour.netId });
        }
    }

    /// <summary>
    /// Hides the specified <paramref name="networkBehaviour"/> from all players.
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to hide.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="networkBehaviour"/> is null.</exception>
    public void HideAttachedBehaviour(NetworkBehaviour networkBehaviour)
    {
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        foreach (var player in ExPlayer.Players)
        {
            if (player?.ReferenceHub == null)
                continue;
            
            if (!HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                HiddenBehaviours.Add(player.UserId, hiddenList = new());
            
            hiddenList.Add(networkBehaviour);

            player.Connection.Send(new ObjectHideMessage() { netId = networkBehaviour.netId });
        }
    }

    /// <summary>
    /// Sends the attached behaviours of the ray object to the specified player.
    /// </summary>
    /// <param name="player">The player to whom the attached behaviours will be sent.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided player or its reference hub is null.</exception>
    public void ShowAttachedBehaviours(ExPlayer player)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList);
        
        foreach (var behaviour in AttachedBehaviours)
        {
            hiddenList?.Remove(behaviour);
            
            player.Connection.Send(new SpawnMessage()
            {
                netId = behaviour.netId,
                
                assetId = behaviour.netIdentity.assetId,
                sceneId = behaviour.netIdentity.sceneId,
                
                isLocalPlayer = behaviour.isLocalPlayer,
                
                isOwner = false,
                
                scale = behaviour.transform.localScale,
                
                position = behaviour.transform.position,
                rotation = behaviour.transform.rotation
            });
        }
    }

    /// <summary>
    /// Sends network spawn messages to make the attached behaviours visible
    /// to players who satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">
    /// A function that determines whether a player should be sent the spawn message
    /// for the attached behaviours. Returns true if the behaviours should be shown
    /// to the player, false otherwise.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="predicate"/> is null.
    /// </exception>
    public void ShowAttachedBehaviours(Predicate<ExPlayer> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        foreach (var behaviour in AttachedBehaviours)
        {
            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub == null)
                    continue;

                if (!predicate(player))
                    continue;
                
                if (HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                    hiddenList.Remove(behaviour);
                
                player.Connection.Send(new SpawnMessage()
                {
                    netId = behaviour.netId,

                    assetId = behaviour.netIdentity.assetId,
                    sceneId = behaviour.netIdentity.sceneId,

                    isLocalPlayer = behaviour.isLocalPlayer,

                    isOwner = false,

                    scale = behaviour.transform.localScale,

                    position = behaviour.transform.position,
                    rotation = behaviour.transform.rotation
                });
            }
        }
    }

    /// <summary>
    /// Sends messages to all connected players to spawn all attached networked behaviours
    /// associated with this object, making them visible to the players.
    /// </summary>
    public void ShowAttachedBehaviours()
    {
        isGloballyHidden = false;
        
        foreach (var kvp in HiddenBehaviours)
            kvp.Value.Clear();
        
        foreach (var behaviour in AttachedBehaviours)
        {
            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub == null)
                    continue;
                
                player.Connection.Send(new SpawnMessage()
                {
                    netId = behaviour.netId,

                    assetId = behaviour.netIdentity.assetId,
                    sceneId = behaviour.netIdentity.sceneId,

                    isLocalPlayer = behaviour.isLocalPlayer,

                    isOwner = false,

                    scale = behaviour.transform.localScale,

                    position = behaviour.transform.position,
                    rotation = behaviour.transform.rotation
                });
            }
        }
    }

    /// <summary>
    /// Displays a previously hidden <see cref="NetworkBehaviour"/> for a specific player.
    /// </summary>
    /// <param name="target">The player for whom the behaviour should be made visible.</param>
    /// <param name="networkBehaviour">The <see cref="NetworkBehaviour"/> to be shown.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="target"/> is null or does not have a valid reference hub,
    /// or when <paramref name="networkBehaviour"/> is null.
    /// </exception>
    public void ShowAttachedBehaviour(ExPlayer target, NetworkBehaviour networkBehaviour)
    {
        if (target?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(target));

        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        if (HiddenBehaviours.TryGetValue(target.UserId, out var hiddenList))
            hiddenList.Remove(networkBehaviour);

        target.Connection.Send(new SpawnMessage()
        {
            netId = networkBehaviour.netId,

            assetId = networkBehaviour.netIdentity.assetId,
            sceneId = networkBehaviour.netIdentity.sceneId,

            isLocalPlayer = networkBehaviour.isLocalPlayer,

            isOwner = false,

            scale = networkBehaviour.transform.localScale,

            position = networkBehaviour.transform.position,
            rotation = networkBehaviour.transform.rotation
        });
    }

    /// <summary>
    /// Displays a specific attached network behaviour to all players matching the given predicate.
    /// </summary>
    /// <param name="predicate">
    /// A predicate function used to determine which players should have the attached network behaviour made visible.
    /// </param>
    /// <param name="networkBehaviour">
    /// The network behaviour to be displayed to the players specified by the predicate.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="predicate"/> or <paramref name="networkBehaviour"/> is null.
    /// </exception>
    public void ShowAttachedBehaviour(Predicate<ExPlayer> predicate, NetworkBehaviour networkBehaviour)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        foreach (var player in ExPlayer.Players)
        {
            if (player?.ReferenceHub == null)
                continue;

            if (!predicate(player))
                continue;
            
            if (HiddenBehaviours.TryGetValue(player.UserId, out var hiddenList))
                hiddenList.Remove(networkBehaviour);

            player.Connection.Send(new SpawnMessage()
            {
                netId = networkBehaviour.netId,

                assetId = networkBehaviour.netIdentity.assetId,
                sceneId = networkBehaviour.netIdentity.sceneId,

                isLocalPlayer = networkBehaviour.isLocalPlayer,

                isOwner = false,

                scale = networkBehaviour.transform.localScale,

                position = networkBehaviour.transform.position,
                rotation = networkBehaviour.transform.rotation
            });
        }
    }

    /// <summary>
    /// Reveals a previously hidden network behaviour by removing it from all hidden behaviour collections
    /// and sending a spawn message to all connected players.
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to be revealed. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided network behaviour is null.</exception>
    public void ShowAttachedBehaviour(NetworkBehaviour networkBehaviour)
    {
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (!AttachedBehaviours.Contains(networkBehaviour))
            throw new ArgumentException("The provided network behaviour is not attached to this ray object.", nameof(networkBehaviour));

        foreach (var kvp in HiddenBehaviours)
            kvp.Value.Remove(networkBehaviour);

        foreach (var player in ExPlayer.Players)
        {
            if (player?.ReferenceHub == null)
                continue;

            player.Connection.Send(new SpawnMessage()
            {
                netId = networkBehaviour.netId,

                assetId = networkBehaviour.netIdentity.assetId,
                sceneId = networkBehaviour.netIdentity.sceneId,

                isLocalPlayer = networkBehaviour.isLocalPlayer,

                isOwner = false,

                scale = networkBehaviour.transform.localScale,

                position = networkBehaviour.transform.position,
                rotation = networkBehaviour.transform.rotation
            });
        }
    }

    /// <summary>
    /// Attaches a network behaviour to the ray object if it is not already attached.
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to attach to the ray object.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="networkBehaviour"/> is null.</exception>
    public void AttachBehaviour(NetworkBehaviour networkBehaviour)
    {
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));

        if (AttachedBehaviours.AddUnique(networkBehaviour))
        {
            if (ShouldHideAttachedBehaviours)
            {
                ExPlayer.Players.ForEach(p => p.Connection?.Send(new ObjectHideMessage() { netId = networkBehaviour.netId }));  
            }
            else
            {
                ExPlayer.Players.ForEach(p =>
                {
                    if (!BehaviourVisibilityPredicate(p))
                    {
                        p.Connection?.Send(new ObjectHideMessage() { netId = networkBehaviour.netId });
                    }
                });
            }
        }
    }

    /// <summary>
    /// Removes a specified network behaviour from the attached behaviours of the RayObject.
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to be removed. Cannot be null.</param>
    /// <param name="resetVisibility">Indicates whether to reset the visibility of the behaviour to all players before removal.</param>
    /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="networkBehaviour"/> is null.</exception>
    public void RemoveBehaviour(NetworkBehaviour networkBehaviour, bool resetVisibility = false)
    {
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));

        if (resetVisibility)
        {
            foreach (var player in ExPlayer.Players)
            {
                if (!IsBehaviourVisible(player, networkBehaviour))
                {
                    ShowAttachedBehaviour(player, networkBehaviour);
                }
            }
        }

        foreach (var kvp in HiddenBehaviours)
        {
            kvp.Value.Remove(networkBehaviour);
        }

        AttachedBehaviours.Remove(networkBehaviour);
    }

    /// <summary>
    /// Destroys the specified network behaviour and removes it from the attached and hidden behaviours lists.
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to destroy. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided network behaviour is null.</exception>
    public void DestroyBehaviour(NetworkBehaviour networkBehaviour)
    {
        if (networkBehaviour == null)
            throw new ArgumentNullException(nameof(networkBehaviour));
        
        if (AttachedBehaviours.Remove(networkBehaviour))
        {
            foreach (var kvp in HiddenBehaviours)
            {
                kvp.Value.Remove(networkBehaviour);
            }
            
            NetworkServer.Destroy(networkBehaviour.gameObject);
        }
    }

    /// <summary>
    /// Updates the visibility state of attached behaviours for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the visibility of behaviours will be updated.</param>
    /// <exception cref="ArgumentException">Thrown when the provided player is not valid.</exception>
    public void UpdateBehaviourVisibility(ExPlayer player)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentException("The provided player is not valid.", nameof(player));

        foreach (var behaviour in AttachedBehaviours)
        {
            if (!IsBehaviourVisible(player, behaviour))
            {
                player.Connection.Send(new ObjectHideMessage() { netId = behaviour.netId });
            }
            else
            {
                player.Connection.Send(new SpawnMessage()
                {
                    netId = behaviour.netId,

                    assetId = behaviour.netIdentity.assetId,
                    sceneId = behaviour.netIdentity.sceneId,

                    isLocalPlayer = behaviour.isLocalPlayer,

                    isOwner = false,

                    scale = behaviour.transform.localScale,

                    position = behaviour.transform.position,
                    rotation = behaviour.transform.rotation
                });
            }
        }
    }

    /// <summary>
    /// Gets called once the player starts looking at this object.
    /// </summary>
    /// <param name="player"></param>
    public virtual void OnStartedLooking(LookingPlayer player)
    {
    }

    /// <summary>
    /// Gets called once the player stops looking at this object.
    /// </summary>
    /// <param name="player">The player who stopped looking.</param>
    public virtual void OnStoppedLooking(LookingPlayer player)
    {
    }

    /// <summary>
    /// Gets called every frame the player keeps looking for.
    /// </summary>
    /// <param name="player">The player who keeps looking.</param>
    public virtual void OnLooking(LookingPlayer player)
    {
        
    }

    /// <summary>
    /// Called when the object is hit by a specified source. Used to process damage and handle hit logic.
    /// </summary>
    /// <param name="player">The player responsible for the hit.</param>
    /// <param name="source">The source of the hit (e.g., firearm, SCP).</param>
    /// <param name="damage">The amount of damage inflicted by the hit.</param>
    public virtual void OnHit(ExPlayer player, ObjectHitSource source, float damage)
    {
        
    }

    /// <summary>
    /// Gets called once per frame when the parent object updates.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Gets called at a fixed interval when the parent object updates.
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// Gets called once per frame at the end of all Update calls when the parent object updates.
    /// </summary>
    public virtual void LateUpdate()
    {
    }

    /// <summary>
    /// Adds a component of the specified type to the associated GameObject.
    /// </summary>
    /// <typeparam name="T">The type of the component to add. Must inherit from UnityEngine.Component.</typeparam>
    /// <returns>The newly added component of type T.</returns>
    public T AddComponent<T>() where T : Component
        => Component.gameObject.AddComponent<T>();

    /// <summary>
    /// Removes the specified component of type T from the associated GameObject.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <returns>True if the specified component was found and successfully removed; otherwise, false.</returns>
    public bool RemoveComponent<T>()
    {
        if (TryGetComponent<T>(out var component) && component is UnityEngine.Object obj)
        {
            UnityEngine.Object.Destroy(obj);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve a component of type T from the GameObject associated with this object.
    /// </summary>
    /// <param name="component">The output parameter that will contain the component if found.</param>
    /// <typeparam name="T">The type of the component to look for.</typeparam>
    /// <returns>True if the component is found; otherwise, false.</returns>
    public bool TryGetComponent<T>(out T component)
        => Component.gameObject.TryFindComponent(out component);
}