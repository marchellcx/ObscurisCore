namespace ObscurisCore.Features.Rays.Enums;

/// <summary>
/// Represents the source that caused an object to be hit.
/// </summary>
public enum ObjectHitSource
{
    /// <summary>
    /// Represents the origin of a hit caused by a firearm.
    /// </summary>
    Firearm,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-049.
    /// </summary>
    Scp049,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-049-2.
    /// </summary>
    Scp0492,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-096.
    /// </summary>
    Scp096,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-106.
    /// </summary>
    Scp106,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-173.
    /// </summary>
    Scp173,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-939.
    /// </summary>
    Scp939,

    /// <summary>
    /// Represents the origin of a hit caused by SCP-3114.
    /// </summary>
    Scp3114,
}