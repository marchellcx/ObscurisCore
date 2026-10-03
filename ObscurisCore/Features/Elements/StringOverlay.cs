using LabExtended.API;
using LabExtended.API.Enums;
using LabExtended.API.Hints;
using ObscurisCore.Utilities.Configs;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Elements
{
    /// <summary>
    /// Displays static strings.
    /// </summary>
    public class StringOverlay : HintElement
    {
        /// <summary>
        /// Gets the options for this overlay.
        /// </summary>
        public OverlayOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the StringOverlay class with the specified overlay options.
        /// </summary>
        /// <param name="options">The configuration options to use for the overlay. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if options is null.</exception>
        public StringOverlay(OverlayOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            Options = options;
        }

        /// <summary>
        /// Gets the server name string.
        /// </summary>
        public string? Content { get; private set; }

        /// <inheritdoc/>
        public override HintAlign GetAlignment(ExPlayer player)
            => Options.Align;

        /// <inheritdoc/>
        public override int GetPixelSpacing(ExPlayer player)
            => Options.PixelSpacing;

        /// <inheritdoc/>
        public override float GetVerticalOffset(ExPlayer player)
            => Options.VerticalOffset;

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            base.OnEnabled();

            Content = Options.OverlayString
                .GetValue()
                .ReplaceEmojis();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            base.OnDisabled();
            Content = null;
        }

        /// <inheritdoc/>
        public override bool OnDraw(ExPlayer player)
        {
            if (Builder == null)
                return false;

            if (Content != null)
                Builder.Append(Content);

            return Builder.Length > 0;
        }
    }
}
