using LabExtended.API;
using LabExtended.Core;
using NorthwoodLib.Pools;
using UnityEngine;

namespace ObscurisCore.Features.Rays.Objects
{
    /// <summary>
    /// Represents a Unity component that manages ray-based interactions and tracks players currently interacting with
    /// ray objects.
    /// </summary>
    public class RayComponent : MonoBehaviour
    {
        /// <summary>
        /// Gets the collection of players associated with the current frame.
        /// </summary>
        private List<ExPlayer> framePlayers;

        /// <summary>
        /// Gets the collection of ray-traceable objects contained in the scene.
        /// </summary>
        public List<RayObject> Objects { get; private set; }

        /// <summary>
        /// Gets the list of players currently being observed or tracked.
        /// </summary>
        public List<ExPlayer> Players { get; private set; }

        /// <summary>
        /// Adds the specified <see cref="RayObject"/> to the collection if it is not already present and not associated
        /// with another component.
        /// </summary>
        /// <remarks>If the object is already associated with a component or is present in the collection,
        /// this method does nothing. Upon successful addition, the object's <c>Component</c> property is set and its
        /// <c>Start</c> method is called.</remarks>
        /// <param name="obj">The <see cref="RayObject"/> to add. Must not be null and must not already be associated with a component.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        public void AddObject(RayObject obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            if (obj.Component != null)
                return;

            if (Objects.Contains(obj))
                return;

            obj.Component = this;
            obj.Start();

            Objects.Add(obj);
        }

        /// <summary>
        /// Removes the specified object from the collection and destroys its associated component if present.
        /// </summary>
        /// <remarks>If the object has an associated component, the component is destroyed before removal.
        /// If the object is not present in the collection, no action is taken.</remarks>
        /// <param name="obj">The object to remove from the collection. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        public void RemoveObject(RayObject obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            if (!Objects.Contains(obj))
                return;

            if (obj.Component != null)
            {
                obj.Destroy();
                obj.Component = null!;
            }

            Objects.Remove(obj);
        }

        void Start()
        {
            Objects = ListPool<RayObject>.Shared.Rent();
            Players = ListPool<ExPlayer>.Shared.Rent();

            framePlayers = ListPool<ExPlayer>.Shared.Rent();

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

            ListPool<RayObject>.Shared.Return(Objects);

            ListPool<ExPlayer>.Shared.Return(Players);
            ListPool<ExPlayer>.Shared.Return(framePlayers);
        }

        private void OnStartedLooking(ExPlayer player)
        {
            for (var i = 0; i < Objects.Count; i++)
            {
                try
                {
                    Objects[i].OnStartedLooking(player);
                }
                catch (Exception ex)
                {
                    ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
                }
            }
        }

        private void OnStoppedLooking(ExPlayer player)
        {
            for (var i = 0; i < Objects.Count; i++)
            {
                try
                {
                    Objects[i].OnStoppedLooking(player);
                }
                catch (Exception ex)
                {
                    ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
                }
            }
        }
        
        private void OnLooking(ExPlayer player)
        {
            for (var i = 0; i < Objects.Count; i++)
            {
                try
                {
                    Objects[i].OnLooking(player);
                }
                catch (Exception ex)
                {
                    ApiLog.Error("RayComponent", $"Error in OnLooking of RayObject:\n{ex}");
                }
            }
        }

        internal void OnHit(ExPlayer player)
        {
            if (!framePlayers.Contains(player))
            {
                framePlayers.Add(player);
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
}