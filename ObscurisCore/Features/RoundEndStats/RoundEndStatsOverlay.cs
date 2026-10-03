using System.Text;

using LabExtended.API;
using LabExtended.API.Enums;
using LabExtended.API.Hints;

using LabExtended.Events;
using LabExtended.Extensions;

using Respawning.Objectives;

using ObscurisCore.Extensions;

using Utils.NonAllocLINQ;

namespace ObscurisCore.Features.RoundEndStats;

/// <summary>
/// Represents an overlay that displays statistical data at the end of a round.
/// </summary>
public class RoundEndStatsOverlay : HintElement
{
    /// <summary>
    /// Indicates whether the RoundEndStatsOverlay component should override other hint elements
    /// displayed simultaneously on the screen.
    /// </summary>
    public override bool OverridesOthers => true;

    /// <summary>
    /// Indicates whether the RoundEndStatsOverlay content should be wrapped to fit within the display boundaries.
    /// </summary>
    public override bool ShouldWrap => false;

    /// <summary>
    /// Determines the alignment of the RoundEndStatsOverlay component for a specific player.
    /// This method specifies how the overlay should be positioned within the screen in terms of alignment (e.g., centered).
    /// </summary>
    /// <param name="player">The player for whom the alignment is being determined.</param>
    /// <returns>Returns a <see cref="HintAlign"/> value representing the calculated alignment for the overlay.</returns>
    public override HintAlign GetAlignment(ExPlayer player)
        => HintAlign.Left;

    /// <summary>
    /// Calculates the vertical offset for the RoundEndStatsOverlay component for a specific player.
    /// This method determines how far vertically the overlay should appear relative to its default position,
    /// ensuring correct alignment on the screen.
    /// </summary>
    /// <param name="player">The player for whom the vertical offset is being calculated.</param>
    /// <returns>Returns a float representing the calculated vertical offset for the overlay.</returns>
    public override float GetVerticalOffset(ExPlayer player)
        => -2.2f;

    /// <summary>
    /// Builder for the global stats.
    /// </summary>
    public static StringBuilder GlobalStats { get; } = new();
    
    /// <summary>
    /// Builder for the personal stats.
    /// </summary>
    public static Dictionary<string, StringBuilder> BuiltStats { get; } = new();

    /// <summary>
    /// Renders statistical data for a specific player on the RoundEndStatsOverlay component.
    /// This method retrieves and appends global and player-specific statistics to the UI builder
    /// if available, ensuring the stats are displayed correctly during the round-end overlay.
    /// </summary>
    /// <param name="player">The player whose statistical data is being rendered.</param>
    /// <returns>Returns true if the Builder contains data to render after appending the statistics; otherwise, false.</returns>
    public override bool OnDraw(ExPlayer player)
    {
        if (Builder != null && GlobalStats.Length > 0)
        {
            if (!BuiltStats.TryGetValue(player.UserId, out var stats))
                return false;

            Builder.AppendLine("<size=16><color=red>[</color> <b>GLOBÁLNÍ STATISTIKY</b> <color=red>]</color></size>");
            Builder.AppendLine($"<size=14>{GlobalStats}</size>");

            if (stats.Length > 0)
            {
                Builder.AppendLine();
                Builder.AppendLine("<size=16><color=yellow>[</color> <b>OSOBNÍ STATISTIKY</b> <color=yellow>]</color></size>");
                Builder.AppendLine($"<size=14>{stats}</size>");
            }

            return Builder.Length > 0;
        }

        return false;
    }

    /// <summary>
    /// Initializes the RoundEndStatsOverlay component when it is enabled.
    /// This method sets up required resources, initializes global and per-user statistics,
    /// and registers event handlers to handle player join and leave events during the round.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();
        
        ExPlayerEvents.Left += OnLeft;
        ExPlayerEvents.Verified += OnJoined;

        BuildStats();
    }

    /// <summary>
    /// Handles the deinitialization logic for the RoundEndStatsOverlay component.
    /// This method is called when the component is disabled. It cleans up any resources,
    /// clears global and per-user stats, and unregisters event handlers associated with the component.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();
        
        ExPlayerEvents.Left -= OnLeft;
        ExPlayerEvents.Verified -= OnJoined;
        
        BuiltStats.ForEachValue(v => v.Clear());
        BuiltStats.Clear();

        GlobalStats.Clear();
    }

    private static void BuildStats()
    {
        var globalStats = RoundEndStatsManager.GlobalStats;
        
        GlobalStats.Clear();
        
        var maxMedkits = globalStats.MedkitUses.OrderBy(p => p.Value).FirstOrDefault();
        var maxDamageByScps = globalStats.DamageDealtByScps.OrderBy(p => p.Value).FirstOrDefault();
        var maxDamageToScps = globalStats.DamageDealtToScps.OrderBy(p => p.Value).FirstOrDefault();
        var maxDamageToHumans = globalStats.DamageDealtToHumans.OrderBy(p => p.Value).FirstOrDefault();
        var mostDeaths = globalStats.MostDeaths.OrderBy(p => p.Value).FirstOrDefault();
        var mostKills = globalStats.MostKills.OrderBy(p => p.Value).FirstOrDefault();

        if (maxMedkits != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální počet použitých medkitů:</b> <color=yellow>{maxMedkits.Value}</color> (<color=red>{maxMedkits.Nick}</color>)");
        
        if (maxDamageByScps != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální damage OD SCP:</b> <color=yellow>{maxDamageByScps.Value}</color> (<color=red>{maxDamageByScps.Nick}</color>)");
        
        if (maxDamageToScps != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální damage SCP:</b> <color=yellow>{maxDamageToScps.Value}</color> (<color=red>{maxDamageToScps.Nick}</color>)");
        
        if (maxDamageToHumans != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální damage hráčům:</b> <color=yellow>{maxDamageToHumans.Value}</color> (<color=red>{maxDamageToHumans.Nick}</color>)");
        
        if (mostDeaths != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální počet úmrtí:</b> <color=yellow>{mostDeaths.Value}</color> (<color=red>{mostDeaths.Nick}</color>)");
        
        if (mostKills != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Maximální počet zabití:</b> <color=yellow>{mostKills.Value}</color> (<color=red>{mostKills.Nick}</color>)");
        
        if (globalStats.FirstEscape != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> První útěk:</b> <color=yellow>{globalStats.FirstEscape.Value}</color> " +
                $"(<color=red>{globalStats.FirstEscape.Nick}</color> za <color={globalStats.FirstEscape.Role.GetRoleColor().ToHex()}>{globalStats.FirstEscape.Role.GetName()}</color>)");
        
        if (globalStats.FirstDeath != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> První úmrtí:</b> <color=yellow>{globalStats.FirstDeath.Value}</color> " +
                $"(<color=red>{globalStats.FirstDeath.Nick}</color> za <color={globalStats.FirstDeath.Role.GetRoleColor().ToHex()}>{globalStats.FirstDeath.Role.GetName()}</color>)");
        
        if (globalStats.LongestSurvival != null)
            GlobalStats.AppendLine(
                $"<b><color=red>>></color> Nejdelší přežití:</b> <color=yellow>{globalStats.LongestSurvival.Value}</color> " +
                $"(<color=red>{globalStats.LongestSurvival.Nick}</color> za <color={globalStats.LongestSurvival.Role.GetRoleColor().ToHex()}>{globalStats.LongestSurvival.Role.GetName()}</color>)");
        
        foreach (var player in ExPlayer.Players)
        {
            if (!player.IsValidPlayer())
                continue;
            
            if (!RoundEndStatsManager.PersonalStats.TryGetValue(player.UserId, out var personalStats))
                continue;
            
            if (!BuiltStats.TryGetValue(player.UserId, out var stats))
                BuiltStats[player.UserId] = stats = new();
            
            stats.Clear();

            stats.AppendLine($"<b><color=green>>>></color> Celkový počet použitých itemů:</b> <color=yellow>{personalStats.ItemUses.Sum(p => p.Value)}</color>");
            stats.AppendLine($"<b><color=green>>>></color> Celkový počet dropnutých itemů:</b> <color=yellow>{personalStats.ItemDrops.Sum(p => p.Value)}</color>");
        }
    }
    
    private static void OnLeft(ExPlayer player)
    {
        if (BuiltStats.TryGetValue(player.UserId, out var stats))
            stats.Clear();
        
        BuiltStats.Remove(player.UserId);
    }

    private static void OnJoined(ExPlayer player)
    {
        BuiltStats[player.UserId] = new();
    }
}