using LabExtended.API;
using PlayerRoles;

namespace ObscurisCore.Features.RoundEndStats.Stats;

public class StatValue<T>
{
    /// <summary>
    /// The value of the statistic.
    /// </summary>
    public T Value { get; set; }
    
    /// <summary>
    /// The nickname of the player.
    /// </summary>
    public string Nick { get; }
    
    /// <summary>
    /// The ID of the player.
    /// </summary>
    public string UserId { get; }
    
    /// <summary>
    /// The role of the player.
    /// </summary>
    public RoleTypeId Role { get; }

    /// <summary>
    /// Creates a new instance of the StatValue class.
    /// </summary>
    /// <param name="player">The player associated with the statistic.</param>
    /// <param name="value">The value of the statistic.</param>
    /// <exception cref="ArgumentNullException">Thrown if the player is null.</exception>
    public StatValue(ExPlayer player, T value)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        Nick = player.Nickname;
        UserId = player.UserId;
        Role = player.Role.Type;

        Value = value;
    }

    /// <summary>
    /// Attempts to retrieve the player associated with this statistic using their User ID.
    /// </summary>
    /// <param name="player">When this method returns, contains the player associated with the User ID, or null if no player was found.</param>
    /// <returns>True if the player was successfully retrieved; otherwise, false.</returns>
    public bool TryGetPlayer(out ExPlayer player)
        => ExPlayer.TryGet(UserId, out player);

    /// <summary>
    /// Retrieves an existing statistic for the specified player or creates a new one with a default value if it does not exist.
    /// </summary>
    /// <param name="player">The player associated with the statistic.</param>
    /// <param name="stats">The collection of statistics to search within or update with a new entry.</param>
    /// <param name="defaultValue">The default value to assign if a statistic for the player is not found.</param>
    /// <returns>A <see cref="StatValue{T}"/> instance associated with the specified player.</returns>
    public static StatValue<T> GetOrAdd(ExPlayer player, List<StatValue<T>> stats, T defaultValue)
    {
        var statValue = stats.Find(x => x.UserId == player.UserId);

        if (statValue == null)
        {
            statValue = new(player, defaultValue);
            
            stats.Add(statValue);
            return statValue;
        }

        return statValue;
    }
}