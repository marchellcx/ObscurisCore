using AdminToys;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.API.Toys;

using UnityEngine;

namespace ObscurisCore.Utilities.Interactions
{
    /// <summary>
    /// Base class for interactable object with mono behaviours.
    /// </summary>
    public abstract class InteractableBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Gets the spawned interactable instance.
        /// </summary>
        public InteractableToy InteractableToy { get; private set; }

        /// <summary>
        /// Gets the scale of the interctable toy.
        /// </summary>
        public virtual Vector3 InteractableScale { get; set; } = Vector3.one;

        /// <summary>
        /// Gets the minimum duration of the interaction.
        /// </summary>
        public virtual float InteractionDuration { get; set; } = 0.5f;

        /// <summary>
        /// Gets the shape of the collider.
        /// </summary>
        public virtual InvisibleInteractableToy.ColliderShape InteractableShape { get; set; } = InvisibleInteractableToy.ColliderShape.Box;

        /// <summary>
        /// Spawns the toy.
        /// </summary>
        /// <param name="parent">The parent of the toy.</param>
        /// <returns>true if the toy was spawned.</returns>
        public bool SpawnInteractable(Transform? parent = null)
        {
            if (InteractableToy?.Base != null)
                return false;

            InteractableToy = new()
            {
                Scale = InteractableScale,
                Shape = InteractableShape,
                InteractionDuration = InteractionDuration
            };

            if (parent != null)
                InteractableToy.Transform.SetParent(parent);

            PlayerEvents.SearchedToy += _OnSearched;
            PlayerEvents.InteractedToy += _OnInteracted;

            return true;
        }

        /// <summary>
        /// Destroys the toy.
        /// </summary>
        /// <returns>true if the toy was destroyed</returns>
        public bool DestroyInteractable()
        {
            if (InteractableToy?.Base != null)
            {
                PlayerEvents.SearchedToy -= _OnSearched;
                PlayerEvents.InteractedToy -= _OnInteracted;

                InteractableToy.Delete();
                InteractableToy = null!;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets called once a player interacts with the toy.
        /// </summary>
        public virtual void OnInteracted(ExPlayer player) { }

        private void _OnInteracted(PlayerInteractedToyEventArgs args)
        {
            if (args.Player is not ExPlayer player)
                return;

            OnInteracted(player);
        }

        private void _OnSearched(PlayerSearchedToyEventArgs args)
        {
            if (args.Player is not ExPlayer player)
                return;

            OnInteracted(player);
        }
    }
}