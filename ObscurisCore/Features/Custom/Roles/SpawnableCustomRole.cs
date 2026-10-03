using System.ComponentModel;

using LabExtended.API;
using LabExtended.API.Custom.Roles;
using LabExtended.Core;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Custom.Roles;

/// <summary>
/// A base class for custom roles that can be spawned on round start.
/// </summary>
public abstract class SpawnableCustomRole : CustomRole
{
    /// <summary>
    /// Janitor spawn conditions.
    /// </summary>
    [Description("Sets the Janitor spawn conditions.")]
    public virtual List<SpawnRange> Conditions { get; set; } = new()
    {
        new()
        { 
            MinPlayers = 1,
            MaxPlayers = 6,
            OverallChance = 20,
            MaxSpawnCount = 1
        },

        new()
        {
            MinPlayers = 7,
            MaxPlayers = 11,
            OverallChance = 50,
            MaxSpawnCount = 1
        },

        new()
        {
            MinPlayers = 12,
            MaxPlayers = 18,
            OverallChance = 80,
            MaxSpawnCount = 1
        },

        new()
        {
            MinPlayers = 19,
            OverallChance = 100,
            MaxPlayers = -1,
            MaxSpawnCount = 1
        }
    };
    
    /// <summary>
    /// The message to be displayed when the role is spawned.
    /// </summary>
    [Description("Sets the message to be displayed when the role is spawned.")]
    public string SpawnMessage { get; set; } = "";

    /// <summary>
    /// Executes when the custom role is added to a player, performing additional setup or initialization logic.
    /// </summary>
    /// <param name="player">The player instance to which the custom role is being added.</param>
    /// <param name="data">Optional data passed during the addition of the custom role.</param>
    public override void OnAdded(ExPlayer player, ref object? data)
    {
        base.OnAdded(player, ref data);
        
        player.SendFormattedAlert(SpawnMessage, true, AlertType.Info, 5f, "Custom Role");
    }

    /// <summary>
    /// Determines whether a player is eligible for spawning a custom role based on certain conditions.
    /// </summary>
    /// <param name="player">The player to evaluate for spawning eligibility.</param>
    /// <returns>
    /// A boolean value indicating whether the specified player meets the conditions to be spawned
    /// as a custom role (true if eligible, false otherwise).
    /// </returns>
    public virtual bool CheckPlayerForSpawning(ExPlayer player)
        => player.Role.Type == Type;

    internal void SpawnRoleOnRoundStart(List<ExPlayer> players)
    {
        if (players.Count == 0)
            return;

        Conditions.SetRoles(players, player =>
        {
            ApiLog.Info(Name, $"Spawning {player.ToLogString()} as &3{Name}&r");
            
            Give(player);

            players.Remove(player);
        }, CheckPlayerForSpawning);
    }
}