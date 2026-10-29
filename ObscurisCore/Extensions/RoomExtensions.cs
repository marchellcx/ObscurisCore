using Interactables.Interobjects.DoorUtils;
using LabExtended.API;

using MapGeneration;
using MapGeneration.Distributors;
using PlayerRoles.PlayableScps.Scp106;

using UnityEngine;

namespace ObscurisCore.Extensions;

/// <summary>
/// Extensions for room utils.
/// </summary>
public static class RoomExtensions
{
    /// <summary>
    /// Represents the maximum Y-coordinate value for the surface zone.
    /// </summary>
    public const float MaxSurfaceY = 302f;

    /// <summary>
    /// Triggers the distribution process for all doors associated with the specified room by invoking
    /// the server-side spawn logic for each door.
    /// </summary>
    /// <param name="room">The room for which the door distribution process should be triggered. Cannot be null.</param>
    public static void TriggerDistribution(this RoomIdentifier room)
    {
        foreach (var door in DoorVariant.AllDoors)
        {
            if (!door.Rooms.Contains(room))
                continue;
            
            SpawnablesDistributorBase.ServerSpawnForAllDoor(door);
        }
    }

    /// <summary>
    /// Returns a safe position within the specified room for the given player, ensuring the location is suitable
    /// for spawning or teleportation.
    /// </summary>
    /// <remarks>If no safe positions can be found for the specified room, an exception is thrown. On
    /// the surface zone, the Y coordinate of the returned position is clamped to a maximum value to ensure
    /// safety.</remarks>
    /// <param name="room">The room in which to find a safe position. Cannot be null.</param>
    /// <param name="player">The player for whom the safe position is being determined. The player's role must be alive and have a valid
    /// movement module and character controller.</param>
    /// <returns>A Vector3 representing a safe position in the room for the player. Returns Vector3.zero if the player is not
    /// alive or lacks a valid movement module or character controller.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="room"/> is null.</exception>
    public static Vector3 GetSafePosition(this RoomIdentifier room, ExPlayer player)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));

        if (player?.ReferenceHub == null
            || !player.Role.IsAlive
            || player.Role.MovementModule == null
            || player.Role.MovementModule.CharController == null)
            return Vector3.zero;

        var poses = SafeLocationFinder.GetLocations(
            r => r != null && r.Link.Rooms?.Length > 0 && r.Link.Rooms.Contains(room),
            d => d != null && d.Rooms?.Length > 0 && d.Rooms.Contains(room));

        if (poses?.Count < 1)
            return Vector3.zero;

        var pose = Scp106PocketExitFinder.GetRandomPose(poses!.ToArray());

        var range = room.Zone is FacilityZone.Surface
            ? 11f
            : 45f;

        var position = SafeLocationFinder.GetSafePositionForPose(pose, range, player.Role.MovementModule!.CharController);

        if (room.Zone is FacilityZone.Surface)
            position.y = Mathf.Min(MaxSurfaceY, position.y);

        return position;
    }
}