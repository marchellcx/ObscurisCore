namespace ObscurisCore.Features.RoundEndStats.Stats;

/// <summary>
/// Represents personal statistics for a player.
/// </summary>
public class PersonalStats
{
    /// <summary>
    /// Gets the item uses for the player.
    /// </summary>
    public Dictionary<ItemType, int> ItemUses { get; } = new();

    /// <summary>
    /// Gets the item drops for the player.
    /// </summary>
    public Dictionary<ItemType, int> ItemDrops { get; } = new();
    
    /// <summary>
    /// Resets the personal statistics for the player.
    /// </summary>
    public void Reset()
    {
        ItemUses.Clear();
        ItemDrops.Clear();
    }
}