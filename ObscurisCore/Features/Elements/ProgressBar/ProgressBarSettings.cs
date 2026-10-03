using System.ComponentModel;
using ObscurisCore.Utilities.Configs;

namespace ObscurisCore.Features.Elements.ProgressBar
{
    /// <summary>
    /// Represents the configuration settings for a progress bar, including its appearance and optional labels.
    /// </summary>
    public class ProgressBarSettings
    {
        /// <summary>
        /// Gets the default configuration settings for the progress bar.
        /// </summary>
        /// <remarks>Use this property to obtain a standard set of settings when creating or resetting a
        /// progress bar. The returned instance provides commonly used defaults suitable for most scenarios.</remarks>
        public static ProgressBarSettings Default { get; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the percentage value is displayed next to the progress bar.
        /// </summary>
        [Description("Show percentage value next to the progress bar.")]
        public bool ShowPercent { get; set; } = false;

        /// <summary>
        /// Gets or sets the format string used for displaying percentages.
        /// </summary>
        [Description("Format string for the percentage display.")]
        public ConfigurableString PercentFormat { get; set; } = new() { Value = "{0}%" };

        /// <summary>
        /// Gets or sets the string used for the filled portion of the progress bar.
        /// </summary>
        [Description("String used for the filled portion of the progress bar.")]
        public ConfigurableString FilledPart { get; set; } = new() { Value = "█" };

        /// <summary>
        /// Gets or sets the string used to represent the empty portion of the progress bar.
        /// </summary>
        [Description("String used for the empty portion of the progress bar.")]
        public ConfigurableString LowerPart { get; set; } = new() { Value = "▄" };

        /// <summary>
        /// Gets or sets the label displayed to the left of the progress bar.
        /// </summary>
        [Description("Label displayed to the left of the progress bar.")]
        public ConfigurableString LeftLabel { get; set; } = new();

        /// <summary>
        /// Gets or sets the label displayed to the right of the progress bar.
        /// </summary>
        [Description("Label displayed to the right of the progress bar.")]
        public ConfigurableString RightLabel { get; set; } = new();
    }
}
