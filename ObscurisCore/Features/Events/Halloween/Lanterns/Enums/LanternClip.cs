namespace ObscurisCore.Features.Events.Halloween.Lanterns.Enums;

/// <summary>
/// Represents the audio clips associated with the lantern in the Halloween event.
/// </summary>
public enum LanternClip : byte
{
    /// <summary>
    /// The clip to play while the lantern is active.
    /// </summary>
    Ambience,
    
    /// <summary>
    /// The clip to play when the lantern is shot.
    /// </summary>
    Shot
}