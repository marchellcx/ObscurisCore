using LabExtended.API;
using LabExtended.API.Custom.Teams;

using LabExtended.Core;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Loadouts;

using PlayerRoles;

namespace ObscurisCore.Features.Custom.Teams.Configurable;

/// <summary>
/// Represents a wave of a configurable team within the game.
/// </summary>
public class ConfigurableTeamWave : CustomTeamInstance<ConfigurableTeam>
{
    /// <summary>
    /// Spawns the specified player with the given role in this team wave.
    /// </summary>
    /// <param name="player">The player to spawn.</param>
    /// <param name="role">The role to assign to the player.</param>
    /// <remarks>
    /// Implement this method to define how players are spawned in this team wave.
    /// </remarks>
    public override void SpawnPlayer(ExPlayer player, RoleTypeId role)
    {
        if (!Handler.Selections.TryGetValue(player.UserId, out var loadoutDefinition))
        {
            ApiLog.Warn($"Loadout for player &3{player.UserId}&r could not be found.");
            return;
        }

        if (player.Role.Type != role)
        {
            player.Role.Set(role, RoleChangeReason.Respawn, RoleSpawnFlags.UseSpawnpoint);
            
            ApiLog.Debug("ConfigurableTeamWave", $"Spawned player &3{player.UserId}&r with role &1{role}&r.");
        }
        else
        {
            player.RandomSpawnPositionTeleport([role.GetTeam()]);
            
            ApiLog.Debug("ConfigurableTeamWave", $"Teleported player &3{player.UserId}&r with role &1{role}&r.");
        }

        if (!LoadoutManager.TryApply(player, loadoutDefinition.Name))
        {
            ApiLog.Warn($"Loadout &1{loadoutDefinition.Name}&r for player &3{player.UserId}&r could not be applied.");
        }
        else
        {
            ApiLog.Debug("ConfigurableTeamWave", $"Loadout &1{loadoutDefinition.Name}&r for player &3{player.UserId}&r was applied.");
        }
    }
}