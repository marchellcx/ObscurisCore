using LabExtended.API;

namespace ObscurisCore.Features.Rays.Objects
{
    /// <summary>
    /// Wrapper for raycast hit objects.
    /// </summary>
    public class RayObject
    {
        /// <summary>
        /// Gets the parent RayComponent.
        /// </summary>
        public RayComponent Component { get; internal set; }

        /// <summary>
        /// Gets the list of players who are currently looking at this object.
        /// </summary>
        public IReadOnlyList<ExPlayer>? Players => Component?.Players;

        /// <summary>
        /// Gets called when the object gets added.
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Gets called when the parent object gets destroyed.
        /// </summary>
        public virtual void Destroy() { }

        /// <summary>
        /// Gets called once the player starts looking at this object.
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnStartedLooking(ExPlayer player) { }

        /// <summary>
        /// Gets called once the player stops looking at this object.
        /// </summary>
        /// <param name="player">The player who stopped looking.</param>
        public virtual void OnStoppedLooking(ExPlayer player) { }

        /// <summary>
        /// Gets called every frame the player keeps looking for.
        /// </summary>
        /// <param name="player">The player who keeps looking.</param>
        public virtual void OnLooking(ExPlayer player) { }

        /// <summary>
        /// Gets called once per frame when the parent object updates.
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Gets called at a fixed interval when the parent object updates.
        /// </summary>
        public virtual void FixedUpdate() { }

        /// <summary>
        /// Gets called once per frame at the end of all Update calls when the parent object updates.
        /// </summary>
        public virtual void LateUpdate() { }
    }
}