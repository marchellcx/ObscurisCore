using System.ComponentModel;

namespace ObscurisCore.Features.Audio.Clips
{
    /// <summary>
    /// Represents the configuration settings for an audio clip, including playback behavior, volume, and selection
    /// properties.
    /// </summary>
    public class ClipDefinition
    {
        /// <summary>
        /// Gets or sets the weight of this clip when being selected randomly from a list of clips.
        /// </summary>
        [Description("Sets the weight of this clip when being selected randomly from a list of clips.")]
        public float Weight { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the amplification (volume multiplier) of the clip when played.
        /// </summary>
        [Description("Sets the amplification (volume multiplier) of this clip when played.")]
        public float Amplification { get; set; } = 1f;

        /// <summary>
        /// Gets or sets a value indicating whether the clip will loop continuously when played.
        /// </summary>
        [Description("If true, the clip will loop continuously when played.")]
        public bool Loop { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the audio file for this clip.
        /// </summary>
        [Description("Sets the name of the audio file for this clip.")]
        public string Name { get; set; } = string.Empty;
    }
}