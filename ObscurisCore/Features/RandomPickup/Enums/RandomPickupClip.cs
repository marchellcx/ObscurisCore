namespace ObscurisCore.Features.RandomPickup.Enums
{
    /// <summary>
    /// Definition of a random pickup audio clip.
    /// </summary>
    public enum RandomPickupClip
    {
        /// <summary>
        /// Played when the pickup spawns in the world.
        /// </summary>
        Spawned,

        /// <summary>
        /// Played on repeat while the pickup is waiting to be collected.
        /// </summary>
        Waiting,

        /// <summary>
        /// Played on first open.
        /// </summary>
        Opened
    }
}