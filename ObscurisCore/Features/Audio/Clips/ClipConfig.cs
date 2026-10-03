using System.ComponentModel;

namespace ObscurisCore.Features.Audio.Clips
{
    /// <summary>
    /// Represents the configuration for managing clips, including cooldown durations and available clip definitions for
    /// each clip type.
    /// </summary>
    public class ClipConfig<T>
    {
        /// <summary>
        /// Gets or sets the cooldown durations, in seconds, for each clip type.
        /// </summary>
        [Description("Sets cooldown time (in seconds). for each clip type.")]
        public Dictionary<T, float> Cooldowns { get; set; } = new()
        {

        };

        /// <summary>
        /// Gets or sets a dictionary that maps each clip type to a list of available clip definitions.
        /// </summary>
        [Description("Sets a list of available clips for each clip type.")]
        public Dictionary<T, List<ClipDefinition>> Clips { get; set; } = new()
        {

        };
    }
}