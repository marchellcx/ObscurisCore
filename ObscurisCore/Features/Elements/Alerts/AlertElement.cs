using System.Diagnostics;

using LabExtended.API;
using LabExtended.API.Enums;

using LabExtended.API.Hints;
using LabExtended.API.Hints.Elements.Personal;

using LabExtended.Events;
using LabExtended.Extensions;

namespace ObscurisCore.Features.Elements.Alerts;

/// <summary>
/// A hint element used to display alerts to players.
/// </summary>
public class AlertElement : PersonalHintElement
{
    /// <summary>
    /// The alignment of the element.
    /// </summary>
    public const HintAlign SettingsAlign = HintAlign.Center;

    /// <summary>
    /// The vertical offset.
    /// </summary>
    public const float SettingsOffset = -5f;
    
    /// <summary>
    /// The pixel line spacing.
    /// </summary>
    public const int SettingsPixelSpacing = DefaultPixelLineSpacing;
    
    /// <summary>
    /// Gets the element's alert queue.
    /// </summary>
    public List<AlertInfo> Alerts { get; } = new();
    
    /// <summary>
    /// Gets or sets the currently displayed alert.
    /// </summary>
    public AlertInfo? CurrentAlert { get; set; }

    /// <summary>
    /// Gets the stopwatch used to check alert duration.
    /// </summary>
    public Stopwatch AlertTimer { get; } = new();

    /// <inheritdoc cref="PersonalHintElement.Alignment"/>
    public override HintAlign Alignment => SettingsAlign;

    /// <inheritdoc cref="PersonalHintElement.VerticalOffset"/>
    public override float VerticalOffset => SettingsOffset;

    /// <inheritdoc cref="PersonalHintElement.PixelSpacing"/>
    public override int PixelSpacing => SettingsPixelSpacing;

    /// <inheritdoc cref="PersonalHintElement.OnDisabled"/>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        AlertTimer.Stop();
        Alerts.Clear();
    }

    /// <inheritdoc cref="PersonalHintElement.OnUpdate"/>
    public override void OnUpdate()
    {
        base.OnUpdate();

        if (CurrentAlert != null)
        {
            if (AlertTimer.Elapsed.TotalSeconds >= CurrentAlert.Duration)
            {
                CurrentAlert = null;
                
                AlertTimer.Stop();
                AlertTimer.Reset();
            }
        }

        if (CurrentAlert is null)
        {
            if (Alerts.Count > 0)
            {
                CurrentAlert = Alerts.RemoveAndTake(0);
                
                AlertTimer.Start();
            }
        }
    }

    /// <inheritdoc cref="PersonalHintElement.OnDraw()"/>
    public override bool OnDraw()
    {
        if (CurrentAlert is null)
            return false;
        
        CurrentAlert.FormattedContent ??= CurrentAlert.FormatAlert();

        Builder?.AppendLine(CurrentAlert.FormattedContent);
        return true;
    }

    private static void Internal_Joined(ExPlayer player)
    {
        player.AddHintElement(new AlertElement());
    }

    internal static void Initialize()
    {
        ExPlayerEvents.Verified += Internal_Joined;
    }
}