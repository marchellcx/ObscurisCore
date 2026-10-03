using System.ComponentModel;

namespace ObscurisCore.Utilities.Configs
{
    /// <summary>
    /// Settings for displaying a string with customizable appearance.
    /// </summary>
    public class ConfigurableString
    {
        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        [Description("Sets the text to be displayed.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        [Description("Sets the color of the text (e.g., hex code or color name).")]
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the font size of the text.
        /// </summary>
        [Description("Sets the font size of the text.")]
        public int Size { get; set; } = 15;

        /// <summary>
        /// Gets or sets the boldness of the text.
        /// </summary>
        [Description("Determines whether the text is bold.")]
        public bool Bold { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the text is italicized.
        /// </summary>
        [Description("Determines whether the text is italicized.")]
        public bool Italic { get; set; } = false;

        /// <summary>
        /// Formats and returns the current value as a styled string based on the applied formatting options.
        /// </summary>
        /// <remarks>The returned string may include optional formatting tags for color, size, bold, and
        /// italic styles, depending on the properties set. If no formatting options are applied, the method returns the
        /// base value.</remarks>
        /// <returns>A formatted string representing the current value with applied styles, or <see langword="null"/> if the base
        /// value is <see langword="null"/>.</returns>
        public string? GetValue()
        {
            var baseValue = Value;

            if (baseValue?.Length < 1)
                return baseValue;

            if (Color?.Length > 0)
                baseValue = $"<color={Color}>{baseValue}</color>";

            if (Size > 0)
                baseValue = $"<size={Size}>{baseValue}</size>";

            if (Bold)
                baseValue = $"<b>{baseValue}</b>";

            if (Italic)
                baseValue = $"<i>{baseValue}</i>";

            return baseValue;
        }
    }
}