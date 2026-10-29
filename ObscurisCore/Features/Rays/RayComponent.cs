using LabExtended.API;
using LabExtended.Core;

using LabExtended.Events.Player;

using Mirror;
using ObscurisCore.Features.Rays.Enums;
using ObscurisCore.Features.Rays.Objects;

using PlayerStatsSystem;

using UnityEngine;

namespace ObscurisCore.Features.Rays;

/// <summary>
/// Represents a Unity component that manages ray-based interactions and tracks players currently interacting with
/// ray objects.
/// </summary>
public class RayComponent : MonoBehaviour
{
    private List<LookingPlayer> framePlayers = new();

    /// <summary>
    /// Gets the collection of ray-traceable objects contained in the scene.
    /// </summary>
    public List<RayObject> Objects { get; } = new();
    
    /// <summary>
    /// Gets the list of players currently being observed or tracked.
    /// </summary>
    public List<LookingPlayer> Players { get; } = new();

    /// <summary>
    /// Adds the specified <see cref="RayObject"/> to the collection if it is not already present and not associated
    /// with another component.
    /// </summary>
    /// <remarks>If the object is already associated with a component or is present in the collection,
    /// this method does nothing. Upon successful addition, the object's <c>Component</c> property is set and its
    /// <c>Start</c> method is called.</remarks>
    /// <param name="obj">The <see cref="RayObject"/> to add. Must not be null and must not already be associated with a component.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    public bool AddObject(RayObject obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        
        if (obj.Component != null)
        {
            ApiLog.Warn("RayComponent", $"Object {obj.GetType()} is already associated with a component");
            return false;
        }

        if (Objects.Contains(obj))
        {
            ApiLog.Warn("RayComponent", $"Object {obj.GetType()} is already present in the collection");
            return false;
        }

        try
        {
            obj.Component = this;
            obj.Start();
        }
        catch (Exception ex)
        {
            obj.Component = null!;
            
            ApiLog.Error("RayComponent", $"Failed to start component {obj.GetType()}:\n{ex}");
            return false;
        }

        Objects.Add(obj);
        return true;
    }

    /// <summary>
    /// Removes the specified object from the collection and destroys its associated component if present.
    /// </summary>
    /// <remarks>If the object has an associated component, the component is destroyed before removal.
    /// If the object is not present in the collection, no action is taken.</remarks>
    /// <param name="obj">The object to remove from the collection. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    public bool RemoveObject(RayObject obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        
        if (!Objects.Contains(obj))
        {
            ApiLog.Warn("RayComponent", $"Object {obj.GetType()} is not present in the collection");
            return false;
        }

        try
        {
            if (obj.Component != null)
            {
                obj.Destroy();
                obj.Component = null!;
            }
        }
        catch (Exception ex)
        {
            ApiLog.Error("RayComponent", $"Failed to destroy component {obj.GetType()}:\n{ex}");
        }

        Objects.Remove(obj);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve an object of type <typeparamref name="T"/> from the collection of ray objects managed
    /// by this component.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="RayObject"/> to retrieve.</typeparam>
    /// <param name="obj">
    /// When this method returns, contains the object of type <typeparamref name="T"/> if found; otherwise, the default value for the type.
    /// </param>
    /// <returns>
    /// <c>true</c> if an object of type <typeparamref name="T"/> is found in the collection; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetObject<T>(out T obj) where T : RayObject
    {
        for (var x = 0; x < Objects.Count; x++)
        {
            if (Objects[x] is T result)
            {
                obj = result;
                return true;
            }
        }

        obj = null!;
        return false;
    }

    void Start()
    {
        RayManager.FrameFinished += OnFrame;
    }

    void OnDestroy()
    {
        RayManager.FrameFinished -= OnFrame;

        for (var i = 0; i < Objects.Count; i++)
        {
            try
            {
                Objects[i].Destroy();
            }
            catch (Exception ex)
            {
                ApiLog.Error("RayComponent", $"Error destroying RayObject:\n{ex}");
            }
        }
        
        Objects.Clear();
        Players.Clear();
        
        framePlayers.Clear();
    }

    private void OnStartedLooking(LookingPlayer lookingPlayer)
    {
        for (var i = 0; i < Objects.Count; i++)
        {
            try
            {
                Objects[i].OnStartedLooking(lookingPlayer);
            }
            catch (Exception ex)
            {
                ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
            }
        }
    }

    private void OnStoppedLooking(LookingPlayer lookingPlayer)
    {
        for (var i = 0; i < Objects.Count; i++)
        {
            try
            {
                Objects[i].OnStoppedLooking(lookingPlayer);
            }
            catch (Exception ex)
            {
                ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
            }
        }
    }

    private void OnLooking(LookingPlayer lookingPlayer)
    {
        for (var i = 0; i < Objects.Count; i++)
        {
            try
            {
                Objects[i].OnLooking(lookingPlayer);
            }
            catch (Exception ex)
            {
                ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
            }
        }
    }

    internal void CallHit(ExPlayer player, ObjectHitSource source, float damage)
    {
        for (var x = 0; x < Objects.Count; x++)
        {
            try
            {
                Objects[x].OnHit(player, source, damage);
            }
            catch (Exception ex)
            {
                ApiLog.Error("RayComponent", $"Error in OnShooting of RayObject:\n{ex}");
            }
        }
    }

    internal void OnHit(ExPlayer player)
    {
        if (!framePlayers.Any(x => x.Player == player))
        {
            framePlayers.Add(new LookingPlayer(player, Time.realtimeSinceStartup));
        }
    }

    internal void OnFrame()
    {
        for (var i = 0; i < framePlayers.Count; i++)
        {
            var framePlayer = framePlayers[i];

            if (Players.Contains(framePlayer))
            {
                OnLooking(framePlayer);
                continue;
            }

            OnStartedLooking(framePlayer);
        }

        for (var i = 0; i < Players.Count; i++)
        {
            var lookingPlayer = Players[i];

            if (framePlayers.Contains(lookingPlayer))
                continue;

            OnStoppedLooking(lookingPlayer);
        }

        Players.Clear();
        Players.AddRange(framePlayers);

        framePlayers.Clear();
    }
}