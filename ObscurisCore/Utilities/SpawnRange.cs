using System.ComponentModel;

namespace ObscurisCore.Utilities;

/// <summary>
/// Defines a range of player count for role properties.
/// </summary>
public class SpawnRange
{
    /// <summary>
    /// Gets or sets the minimum required amount of players.
    /// </summary>
    [Description("Sets the minimum required amount of players.")]
    public int MinPlayers { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum allowed amount of players.
    /// </summary>
    [Description("Sets the maximum allowed amount of players.")]
    public int MaxPlayers { get; set; } = -1;

    /// <summary>
    /// Gets or sets the minimum amount of players to spawn.
    /// </summary>
    [Description("Sets the minimum amount of players to spawn.")]
    public int MinSpawnCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum amount of players to spawn.
    /// </summary>
    [Description("Sets the maximum amount of players to spawn.")]
    public int MaxSpawnCount { get; set; } = -1;

    /// <summary>
    /// Gets or sets the overall spawn chance.
    /// </summary>
    [Description("Sets the overall spawn chance.")]
    public float OverallChance { get; set; } = 0f;
}