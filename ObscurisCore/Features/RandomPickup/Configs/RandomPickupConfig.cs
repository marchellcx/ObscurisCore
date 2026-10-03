using System.ComponentModel;

namespace ObscurisCore.Features.RandomPickup.Configs
{
    /// <summary>
    /// Config file structure for random pickups.
    /// </summary>
    public class RandomPickupConfig
    {
        /// <summary>
        /// Gets or sets the base weight used to determine the likelihood of a player being selected.
        /// </summary>
        [Description("Sets the base weight for a player to be selected.")]
        public float PlayerWeight { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the number of seconds to wait between each player spawn check.
        /// </summary>
        [Description("The amount of seconds of delay between each player spawn check.")]
        public float PlayerDelay { get; set; } = 200f;

        /// <summary>
        /// Gets or sets the number of seconds to wait after the round starts before player pickups begin spawning.
        /// </summary>
        [Description("The amount of seconds that must pass since the round start for player pickups to start spawning.")]
        public float PlayerStartDelay { get; set; } = 300f;

        /// <summary>
        /// Gets or sets the collection of spawn locations and their associated weights.
        /// </summary>
        [Description("Sets the spawn locations along with their weights.")]
        public Dictionary<string, float> Spawns { get; set; } = new();

        /// <summary>
        /// Gets or sets the default properties applied to pickups that do not specify custom properties.
        /// </summary>
        [Description("Sets the properties used for pickups without custom properties.")]
        public RandomPickupProperties GlobalProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the collection of custom properties associated with specific pickup types, keyed by identifier.
        /// </summary>
        [Description("Sets the custom properties for specific pickup types (location names for defined spawns, user IDs for specific players or PlayerSpawn for all players).")]
        public Dictionary<string, RandomPickupProperties> Properties { get; set; } = new()
        {
            { "example", new() },
            { "example2", new() }
        };
    }
}