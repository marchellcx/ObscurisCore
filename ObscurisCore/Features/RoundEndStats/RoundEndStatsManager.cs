using InventorySystem.Items.Usables;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.API.Hints;
using LabExtended.Events;
using ObscurisCore.Features.RoundEndStats.Stats;
using PlayerStatsSystem;

using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RoundEndStats;

public static class RoundEndStatsManager
{
    private static RoundEndStatsOverlay overlay;
    private static HashSet<string> playersDeathThisRound = new();
    private static Dictionary<string, PersonalStats> personalStats = new();
    
    /// <summary>
    /// Gets the global stats.
    /// </summary>
    public static GlobalStats GlobalStats { get; } = new();
    
    /// <summary>
    /// Gets the personal stats of the players.
    /// </summary>
    public static IReadOnlyDictionary<string, PersonalStats> PersonalStats => personalStats;
    
    private static void OnEscaped(PlayerEscapedEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (GlobalStats.FirstEscape != null && ExRound.Duration > GlobalStats.FirstEscape.Value)
            return;

        GlobalStats.FirstEscape = new(player, ExRound.Duration);
    }

    private static void OnDying(PlayerDyingEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;
        
        if (GlobalStats.LongestSurvival != null && ExRound.Duration > GlobalStats.LongestSurvival.Value)
            return;
        
        if (!playersDeathThisRound.Add(player.UserId))
            return;

        GlobalStats.LongestSurvival = new(player, ExRound.Duration);
    }
    
    private static void OnDied(PlayerDeathEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        StatValue<int>.GetOrAdd(player, GlobalStats.MostDeaths, 0).Value++;
        
        if (args.Attacker.CastPlayer(out var attacker) && attacker != player)
            StatValue<int>.GetOrAdd(attacker, GlobalStats.MostKills, 0).Value++;
    }
    
    private static void OnHurt(PlayerHurtEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (args.DamageHandler is not StandardDamageHandler handler)
            return;

        if (args.Attacker.CastPlayer(out var attacker) && attacker != player)
        {
            if (player.Role.IsScp)
                StatValue<float>.GetOrAdd(attacker, GlobalStats.DamageDealtToScps, 0f).Value +=
                    handler.TotalDamageDealt;
            else
                StatValue<float>.GetOrAdd(attacker, GlobalStats.DamageDealtToHumans, 0f).Value +=
                    handler.TotalDamageDealt;

            if (attacker.Role.IsScp)
                StatValue<float>.GetOrAdd(attacker, GlobalStats.DamageDealtByScps, 0f).Value +=
                    handler.TotalDamageDealt;
        }
    }
    
    private static void OnDroppedItem(PlayerDroppedItemEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;
        
        if (personalStats.TryGetValue(player.UserId, out var stats))
            stats.ItemDrops.SetValueSafe(args.Pickup.Type, 0, v => v + 1);

        GlobalStats.TotalDroppedItems++;
    }

    private static void OnUsedItem(PlayerUsedItemEventArgs args)
    {
        if (args.Player is not ExPlayer player)
            return;

        if (args.UsableItem?.Base == null)
            return;

        if (personalStats.TryGetValue(player.UserId, out var stats))
            stats.ItemUses.SetValueSafe(args.UsableItem.Type, 0, v => v + 1);

        if (args.UsableItem.Base is Medkit medkit)
            StatValue<int>.GetOrAdd(player, GlobalStats.MedkitUses, 0).Value++;
    }
    
    private static void OnRoundEnd()
    {
        overlay.AddHintElement();
    }

    private static void OnRestarting()
    {
        GlobalStats.Reset();
        
        playersDeathThisRound.Clear();

        overlay.RemoveHintElement();
    }

    private static void OnLeft(ExPlayer player)
    {
        if (personalStats.TryGetValue(player.UserId, out var stats))
            stats.Reset();
        
        personalStats.Remove(player.UserId);
    }
    
    private static void OnJoined(ExPlayer player)
    {
        if (!personalStats.TryGetValue(player.UserId, out var stats))
            personalStats[player.UserId] = new PersonalStats();
        else
            stats.Reset();
    }
    
    private static void Initialize()
    {
        overlay = new();
        
        ExRoundEvents.Ending += OnRoundEnd;
        ExRoundEvents.Restarting += OnRestarting;
        
        ExPlayerEvents.Left += OnLeft;
        ExPlayerEvents.Verified += OnJoined;
        
        PlayerEvents.UsedItem += OnUsedItem;
        PlayerEvents.DroppedItem += OnDroppedItem;
        
        PlayerEvents.Hurt += OnHurt;
        PlayerEvents.Death += OnDied;
        PlayerEvents.Dying += OnDying;
        PlayerEvents.Escaped += OnEscaped;
    }
}