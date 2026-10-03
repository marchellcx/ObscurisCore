using LabExtended.API.Enums;

using System.ComponentModel;

namespace ObscurisCore.Utilities.Configs
{
    /// <summary>
    /// Base class for overlay options.
    /// </summary>
    public class OverlayOptions
    {
        /// <summary>
        /// Whether the overlay is enabled or disabled.
        /// </summary>
        [Description("Enables or disables the overlay.")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the alignment of the overlay on the screen.
        /// </summary>
        [Description("Sets the alignment of the overlay on the screen.\n#Right, Left, FullLeft, Center")]
        public HintAlign Align { get; set; } = HintAlign.Center;

        /// <summary>
        /// Gets or sets the vertical offset of the overlay on the screen.
        /// </summary>
        [Description("Sets the vertical offset of the overlay on the screen.\n#-15 is bottom of the screen.")]
        public float VerticalOffset { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the pixel spacing on the overlay.
        /// </summary>
        [Description("Sets the pixel spacing on the overlay. Set to -1 to use default spacing.")]
        public int PixelSpacing { get; set; } = -1;

        /// <summary>
        /// Configurable string for the overlay.
        /// </summary>
        [Description("Configurable string for the overlay.")]
        public ConfigurableString OverlayString { get; set; } = new() { Value = string.Empty };
    }
}