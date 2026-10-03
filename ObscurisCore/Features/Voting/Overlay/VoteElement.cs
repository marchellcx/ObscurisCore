using LabExtended.API.Enums;
using LabExtended.API.Hints.Elements.Personal;

using ObscurisCore.Features.Elements.ProgressBar;

using UnityEngine;

namespace ObscurisCore.Features.Voting.Overlay;

/// <summary>
/// Represents a hint element that displays the current active vote and its progress to players.
/// </summary>
/// <remarks>The VoteElement provides a visual summary of the ongoing vote, including the initiator,
/// remaining time, vote title, and options with their respective progress bars. This element is only visible when a
/// vote is active. It is typically used in user interfaces to inform players about the status and results of
/// in-game voting events.</remarks>
public class VoteElement : PersonalHintElement
{
    /// <inheritdoc/>
    public override float VerticalOffset => 5f;

    /// <inheritdoc/>
    public override HintAlign Alignment => HintAlign.FullLeft;

    /// <inheritdoc/>
    public override bool ShouldWrap => false;

    /// <inheritdoc/>
    public override bool ShouldParse => false;

    /// <inheritdoc/>
    public override bool OverridesOthers => true;

    /// <inheritdoc/>
    public override bool OnDraw()
    {
        if (VoteManager.CurrentVote == null)
            return false;

        if (Builder == null)
            return false;

        Builder.Clear();

        if (VoteManager.CurrentVote.Starter?.ReferenceHub != null)
        {
            Builder.Append($"<b><size=18><color=red>📊</color> | <color=red>Hlasování</color> (<color=yellow>{VoteManager.CurrentVote.Starter.Nickname}</color>)</size> | ");
            Builder.AppendLine();
        }

        var voteProgress = Mathf.CeilToInt( ((float)VoteManager.CurrentVote.Duration - Mathf.CeilToInt((float)VoteManager.Remaining.TotalSeconds)) / (float)VoteManager.CurrentVote.Duration * 100);
        var voteColor = ProgressBarElement.GetBarColor(voteProgress);

        Builder.Append("<size=15><b><color=");
        Builder.Append(voteColor);
        Builder.Append(">");
        Builder.Append(Mathf.CeilToInt((float)VoteManager.Remaining.TotalSeconds));
        Builder.Append("s | ");
        Builder.Append(VoteManager.CurrentVote.Title);
        Builder.Append("</color></b></size> | ");
        Builder.AppendLine();

        for (var x = 0; x < VoteManager.CurrentVote.Options.Count; x++)
        {
            var option = VoteManager.CurrentVote.Options[x];
            var percentage = VoteManager.CurrentVote.GetPercentage(option);
            var color = ProgressBarElement.GetBarColor(percentage);
            var bar = ProgressBarElement.RenderBar(percentage, color, ProgressBarSettings.Default);

            Builder.Append("<size=14><b><color=");
            Builder.Append(color);
            Builder.Append(">");
            Builder.Append(x + 1);
            Builder.Append(" | ");
            Builder.Append(option);
            Builder.Append(" | ");
            Builder.Append(bar);
            Builder.Append(" | ");
            Builder.Append(percentage);
            Builder.Append("%");
            Builder.Append("</color></b></size>");
            Builder.AppendLine();
        }

        Builder.AppendLine();
        Builder.Append(
            $"<size=18><b>Pro hlasování stiskni jedno z tlačítek pojmenovaných podle čísla možnosti v záložce <color=red>Server-Specific</color> v <color=yellow>nastavení hry</color>!</b></size>");

        return true;
    }
}