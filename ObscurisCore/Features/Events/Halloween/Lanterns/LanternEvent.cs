using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API.Custom.Items;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;

using MEC;

using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;

using PRTS.Client.Profiles;

namespace ObscurisCore.Features.Events.Halloween.Lanterns;

/// <summary>
/// Provides functionality for managing and retrieving data related to the Lantern Event,
/// including operations for fetching the top players based on lantern points collected.
/// </summary>
public static class LanternEvent
{
    /// <summary>
    /// Gets the configuration key used to store and retrieve lantern points for the Halloween event.
    /// </summary>
    [Config("halloween", "points-key", "The key used to store and retrieve lantern points for the event.")]
    public static string PointsKey = "Halloween2026_LanternPoints";

    /// <summary>
    /// Gets or sets the key used to store and retrieve The Ripper perk status for the Halloween event.
    /// </summary>
    [Config("halloween", "ripper-key", "The key used to store and retrieve The Ripper perk status for the event.")]
    public static string RipperKey = "Halloween2026_TheRipperPerk";

    /// <summary>
    /// Gets the key used to store and retrieve The Ripper block status for the Halloween event.
    /// </summary>
    [Config("halloween", "ripper-block-key", "The key used to store and retrieve The Ripper block status for the event.")]
    public static string RipperBlockKey = "Halloween2026_TheRipperUsed";

    /// <summary>
    /// The chance of a player receiving The Ripper gun.
    /// </summary>
    [Config("halloween", "ripper-chance", "The chance of a player receiving The Ripper gun.")]
    public static float RipperChance = 10f;

    /// <summary>
    /// The ID of The Ripper gun.
    /// </summary>
    [Config("halloween", "ripper-id", "The ID of The Ripper gun.")]
    public static string RipperId = "the_ripper";

    /// <summary>
    /// A set of user IDs that are blocked.
    /// </summary>
    public static HashSet<string> BlockedUsers = new();

    /// <summary>
    /// Gets or sets the available users.
    /// </summary>
    public static HashSet<string> AvailableUsers = new();

    private static void OnPlayerChangedRole(PlayerChangedRoleEventArgs args)
    {
        Timing.CallDelayed(0.2f, () =>
        {
            if (!args.Player.CastPlayer(out var player))
                return;

            if (args.ChangeReason is not PlayerRoles.RoleChangeReason.Respawn)
                return;

            if (args.NewRole.Team is not PlayerRoles.Team.FoundationForces && args.NewRole.Team is not PlayerRoles.Team.ChaosInsurgency)
                return;

            if (args.NewRole.RoleTypeId is PlayerRoles.RoleTypeId.FacilityGuard)
                return;

            if (!AvailableUsers.Contains(args.Player.UserId))
            {
                ApiLog.Debug("LanternEvent", $"User {args.Player.UserId} is not eligible to receive The Ripper perk.");
                return;
            }

            if (RipperChance <= 0f && (RipperChance < 100f && !WeightUtils.GetBool(RipperChance)))
            {
                ApiLog.Debug("LanternEvent", $"User {args.Player.UserId} did not receive The Ripper perk due to chance.");
                return;
            }

            if (!BlockedUsers.Add(args.Player.UserId))
            {
                ApiLog.Debug("LanternEvent", $"User {args.Player.UserId} has already received The Ripper perk, blocking them from receiving it again.");
                return;
            }

            if (!CustomItem.TryGet(RipperId, out var ripperCustomItem))
            {
                ApiLog.Warn("LanternEvent", $"Could not find custom item &3{RipperId}&r!");
                return;
            }

            ripperCustomItem.AddItem(player);

            ProfileModule.Singleton?.CallCmdSetCustomData(player.UserId, RipperKey, "false");
            ProfileModule.Singleton?.CallCmdSetCustomData(player.UserId, RipperBlockKey, "true");

            ApiLog.Debug("LanternEvent", $"User {args.Player.UserId} has received The Ripper perk.");
        });
    }

    private static IEnumerator<float> DownloadData()
    {
        ApiLog.Info("LanternEvent", "Downloading lantern event data - waiting for PRTS");

        while (ProfileModule.Singleton == null)
            yield return Timing.WaitForOneFrame;

        ApiLog.Info("LanternEvent", "Downloading lantern event data - sending request");

        ProfileModule.Singleton.CallCmdGetAllData(result =>
        {
            try
            {
                BlockedUsers.Clear();
                AvailableUsers.Clear();

                if (result != null)
                {
                    ApiLog.Debug("LantarnEvent", $"Successfully downloaded lantern event data ({result.Count})!");

                    foreach (var kvp in result)
                    {
                        if (kvp.CustomData == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(kvp.UserId))
                            continue;

                        if (kvp.CustomData.ContainsKey(RipperBlockKey))
                        {
                            ApiLog.Debug("LanternEvent", $"User {kvp.Nickname} {kvp.UserId} has already used The Ripper perk, blocking them from receiving it again.");

                            BlockedUsers.Add(kvp.UserId);
                            continue;
                        }

                        if (!kvp.CustomData.ContainsKey(RipperKey))
                        {
                            ApiLog.Debug("LanternEvent", $"User {kvp.Nickname} {kvp.UserId} does not have The Ripper perk, blocking them from receiving it.");

                            BlockedUsers.Add(kvp.UserId);
                            continue;
                        }

                        AvailableUsers.Add(kvp.UserId);

                        ApiLog.Debug("LanternEvent", $"User {kvp.Nickname} {kvp.UserId} is eligible to receive The Ripper perk.");
                    }

                    ApiLog.Info("LanternEvent", $"Finished processing lantern event data. Blocked users: &3{BlockedUsers.Count}&r, Available users: &3{AvailableUsers.Count}&r");
                }
                else
                {
                    ApiLog.Warn("LanternEvent", "Failed to download lantern event data!");
                }
            }
            catch (Exception ex)
            {
                ApiLog.Error("LanternEvent", $"An error occurred while processing lantern event data: {ex}");
            }
        });
    }

    private static void OnWaiting()
    {
        Timing.RunCoroutine(DownloadData());
    }

    private static void Initialize()
    {
        PlayerEvents.ChangedRole += OnPlayerChangedRole;

        ExRoundEvents.WaitingForPlayers += OnWaiting;
    }
}