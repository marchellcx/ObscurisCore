using LabExtended.API;

using UnityEngine;

namespace ObscurisCore.Features.Rays.Objects;

/// <summary>
/// Represents a player being observed and metadata about the observation.
/// </summary>
public struct LookingPlayer : IEquatable<LookingPlayer>
{
    /// <summary>
    /// Represents the player who is the subject of observation.
    /// </summary>
    public readonly ExPlayer Player;

    /// <summary>
    /// Represents the realtime since startup when the player started being looked at, measured in seconds.
    /// </summary>
    public readonly float LookedAt;

    /// <summary>
    /// Gets the duration for which the player has been continuously looked at, measured in seconds.
    /// </summary>
    /// <remarks>
    /// The value is calculated as the difference between the current realtime since startup
    /// and the moment the player started being looked at.
    /// </remarks>
    public float LookDuration => Time.realtimeSinceStartup - LookedAt;
    
    /// <summary>
    /// Constructs a new instance of the <c>LookingPlayer</c> struct.
    /// </summary>
    /// <param name="player">The player entity being observed.</param>
    /// <param name="lookedAt">The realtime since startup when the observation began, measured in seconds.</param>
    public LookingPlayer(ExPlayer player, float lookedAt)
    {
        Player = player;
        LookedAt = lookedAt;
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return Player.GetHashCode();
    }

    /// <summary>Indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return obj is LookingPlayer other && Equals(other);
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
    public bool Equals(LookingPlayer other)
    {
        return Player.Equals(other.Player);
    }
}