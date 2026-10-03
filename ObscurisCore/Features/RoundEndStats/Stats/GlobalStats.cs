namespace ObscurisCore.Features.RoundEndStats.Stats;

/// <summary>
/// Represents global statistics for the game.
/// </summary>
public class GlobalStats
{
    /// <summary>
    /// Gets or sets the total amount of dropped items.
    /// </summary>
    public int TotalDroppedItems { get; set; } = 0;
    
    /// <summary>
    /// Gets the number of times a player used a medkit.
    /// </summary>
    public List<StatValue<int>> MedkitUses { get; } = new();
    
    /// <summary>
    /// Gets the damage dealt by SCPs.
    /// </summary>
    public List<StatValue<float>> DamageDealtByScps { get; } = new();
    
    /// <summary>
    /// Gets the damage dealt to SCPs.
    /// </summary>
    public List<StatValue<float>> DamageDealtToScps { get; } = new();
    
    /// <summary>
    /// Gets the damage dealt to humans.
    /// </summary>
    public List<StatValue<float>> DamageDealtToHumans { get; } = new();

    /// <summary>
    /// Gets the most deaths of the players.
    /// </summary>
    public List<StatValue<int>> MostDeaths { get; } = new();
    
    /// <summary>
    /// Gets the most kills of the players.
    /// </summary>
    public List<StatValue<int>> MostKills { get; } = new();
    
    /// <summary>
    /// Gets the first death of the players.
    /// </summary>
    public StatValue<TimeSpan>? FirstDeath { get; set; }
    
    /// <summary>
    /// Gets the first escape of the players.
    /// </summary>
    public StatValue<TimeSpan>? FirstEscape { get; set; }
    
    /// <summary>
    /// Gets the longest survival of the players.
    /// </summary>
    public StatValue<TimeSpan>? LongestSurvival { get; set; }
    
    /// <summary>
    /// Resets the global statistics.
    /// </summary>
    public void Reset()
    {
        MedkitUses.Clear();
        
        DamageDealtByScps.Clear();
        DamageDealtToScps.Clear();
        DamageDealtToHumans.Clear();
        
        MostDeaths.Clear();
        MostKills.Clear();

        FirstEscape = null;
        FirstDeath = null;
        LongestSurvival = null;
        
        TotalDroppedItems = 0;
    }
}