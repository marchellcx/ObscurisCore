using LabExtended.API;
using LabExtended.API.Hints;

using LabExtended.Core;
using LabExtended.Extensions;

using NorthwoodLib.Pools;

namespace ObscurisCore.Features.Elements.Alerts;

/// <summary>
/// Extensions targeting alerts.
/// </summary>
public static class AlertExtensions
{
    /// <summary>
    /// The color of the info emoji.
    /// </summary>
    public const string InfoColor = "047a08";
    
    /// <summary>
    /// The color of the warning emoji.
    /// </summary>
    public const string WarnColor = "fc8403";

    /// <summary>
    /// The start of a message line.
    /// </summary>
    public const string TextStart = "»";

    /// <summary>
    /// The end of a message line.
    /// </summary>
    public const string TextEnd = "«";
    
    /// <summary>
    /// The prefix of a title.
    /// </summary>
    public const string TitlePrefix = "<size=30><b>﹝ ";

    /// <summary>
    /// The postfix of a title.
    /// </summary>
    public const string TitlePostfix = " ﹞</b></size>";
    
    /// <summary>
    /// The default title.
    /// </summary>
    public const string DefaultTitle = "server zpráva";

    /// <summary>
    /// Sends an alert to a player.
    /// </summary>
    /// <param name="player">The player who will receive the alert.</param>
    /// <param name="type">The type of the alert (e.g., informational or warning).</param>
    /// <param name="duration">The duration of the alert in seconds. Must be at least 1.</param>
    /// <param name="title">The title of the alert. If null, a default title is used.</param>
    /// <param name="content">The content of the alert. Must not be empty or whitespace.</param>
    /// <param name="overrideCurrent">Indicates whether to clear current alerts and display this one immediately.</param>
    public static void SendAlert(this ExPlayer player, AlertType type, float duration, string? title, string content,
        bool overrideCurrent = false)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException(nameof(content));

        if (duration < 1f)
            throw new ArgumentOutOfRangeException(nameof(duration));

        if (!player.TryGetHintElement<AlertElement>(out var alertElement))
        {
            if (!player.AddHintElement(alertElement = new()))
            {
                ApiLog.Warn("Alerts", $"Player {player.ToLogString()} is missing the alert element!");
                return;
            }
        }

        if (alertElement.CurrentAlert == null || overrideCurrent)
        {
            alertElement.AlertTimer.Restart();
            
            alertElement.CurrentAlert = new()
            {
                Title = title ?? DefaultTitle,
                Type = type,
                Content = content,
                Duration = duration
            };
        }
        else
        {
            alertElement.Alerts.Add(new()
            {
                Title = title ?? DefaultTitle,
                Type = type,
                Content = content,
                Duration = duration
            });
        }
    }

    /// <summary>
    /// Formats an alert's content.
    /// </summary>
    /// <param name="alert">The alert to format.</param>
    /// <returns>the formatted content of the alert</returns>
    public static string FormatAlert(this AlertInfo alert)
    {
        return StringBuilderPool.Shared.BuildString(x =>
        {
            var content = alert.Content.Replace("\n", $" {TextEnd}\n{TextStart} ");
            var title = ProcessTitle(alert.Title);
            
            x.Append("<color=#");
            x.Append(alert.Type is AlertType.Info ? InfoColor : WarnColor);
            x.Append(">");
            
            x.Append(TitlePrefix);
            x.Append(title);
            x.Append(TitlePostfix);
            
            x.Append("</color>\n<size=25>");
            x.Append(TextStart);
            x.Append(" ");
            x.Append(content);
            x.Append(" ");
            x.Append(TextEnd);
            x.Append("</size>");
        });
    }

    // "word one" to "W O R D • O N E"
    private static string ProcessTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title;

        return string.Join(" • ", title.Split(' ').Select(ProcessTitleWord));
    }

    private static string ProcessTitleWord(string str)
    {
        var result = string.Empty;

        for (var x = 0; x < str.Length; x++)
        {
            var c = str[x];
            
            result += char.ToUpperInvariant(c);

            if (!char.IsWhiteSpace(c))
                result += " ";
        }

        return result.Trim();
    }
}