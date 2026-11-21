using System.ComponentModel;

using LabExtended.API;
using LabExtended.API.Custom.Teams;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;

using ObscurisCore.Features.Loadouts;

using PlayerRoles;

using YamlDotNet.Serialization;

using ObscurisCore.Extensions;

using MEC;

using LabApi.Features.Wrappers;

using LabExtended.Extensions;

using NorthwoodLib.Pools;

using Respawning;
using Respawning.NamingRules;

using Respawning.Waves;
using Respawning.Waves.Generic;

namespace ObscurisCore.Features.Custom.Teams.Configurable;

/// <summary>
/// Represents a configurable team within the game.
/// </summary>
public class ConfigurableTeam : CustomTeamHandler<ConfigurableTeamWave>
{
    internal string configId = string.Empty;

    /// <summary>
    /// Gets the unique identifier for the configurable team.
    /// </summary>
    public override string Id { get => configId; }

    /// <summary>
    /// Gets the name of the configurable team.
    /// </summary>
    public override string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// How many tokens to deduct from the replaced wave upon spawning.
    /// </summary>
    [Description("How many tokens to deduct from the replaced wave upon spawning (also sets the amount of required tokens for spawning).")]
    public int TokenCost { get; set; } = 0;

    /// <summary>
    /// The minimum required amount of spawnable players to spawn a wave.
    /// </summary>
    [Description("The minimum required amount of spawnable players to spawn a wave.")]
    public int MinPlayers { get; set; } = 0;

    /// <summary>
    /// The maximum amount of players that can be spawned in a wave.
    /// </summary>
    [Description("The maximum amount of players that can be spawned in a wave.")]
    public int MaxPlayers { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the chance that this team will replace the specified faction.
    /// </summary>
    [Description("The chance that this team will replace the specified faction.")]
    public float ReplacementChance { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the faction that this team replaces.
    /// </summary>
    [Description("The faction that this team replaces.")]
    public Faction? ReplacedFaction { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to deduct tokens from the replaced team.
    /// </summary>
    [Description("Whether or not to deduct tokens from the replaced team.")]
    public bool DeductTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the replaced team's CASSIE announcement.
    /// </summary>
    [Description("Whether or not to use the replaced team's CASSIE announcement.")]
    public bool ReplacementUseCassie { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the respawn timer for this team.
    /// </summary>
    [Description("Whether or not to use the respawn timer for this team.")]
    public bool UseRespawnTimer { get; set; }

    /// <summary>
    /// Gets or sets the respawn timer for this team.
    /// </summary>
    [Description("The respawn timer for this team.")]
    public CustomTeamTimer<ConfigurableTeamWave> RespawnTimer { get; set; } = new();

    /// <summary>
    /// Gets or sets the default role for this team. Players will be assigned this role if no specific role is selected.
    /// </summary>
    [Description("The default role for this team. Players will be assigned this role if no specific role is selected.")]
    public RoleTypeId DefaultRole { get; set; } = RoleTypeId.None;

    /// <summary>
    /// Gets or sets the list of roles that a player must have to be respawned in this team.
    /// </summary>
    [Description("List of roles that a player must have to be respawned in this team.")]
    public List<RoleTypeId> AllowedRoles { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of roles that a player must not have to be respawned in this team.
    /// </summary>
    [Description("List of roles that a player must not have to be respawned in this team.")]
    public List<RoleTypeId> BlacklistedRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of custom CASSIE announcements for this team.
    /// </summary>
    [Description("List of custom CASSIE announcements, selected randomly upon spawning.")]
    public List<string> CassieAnnouncements { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of loadouts and their corresponding probabilities.
    /// </summary>
    [Description("A dictionary of loadouts and their corresponding probabilities.")]
    public Dictionary<string, float> Loadouts { get; set; } = new() 
    {
        { "LoadoutA", 0.5f },
        { "LoadoutB", 0.3f },
        { "LoadoutC", 0.2f }
    };

    /// <summary>
    /// Gets or sets the dictionary of selected loadouts for each player.
    /// </summary>
    [YamlIgnore]
    public Dictionary<string, LoadoutDefinition> Selections { get; set; } = new();

    /// <summary>
    /// Gets the wave timer for this team, if the respawn timer is enabled.
    /// </summary>
    [YamlIgnore]
    public override CustomTeamTimer<ConfigurableTeamWave>? WaveTimer
    {
        get
        {
            if (!UseRespawnTimer)
                return null;

            return RespawnTimer;
        }
    }

    /// <summary>
    /// Called when the team is registered.
    /// </summary>
    public override void OnRegistered()
    {
        base.OnRegistered();

        ExPlayerEvents.Left += OnPlayerLeft;
        ExRoundEvents.Restarting += OnRoundRestart;

        ApiLog.Info($"Configurable team &3{configId}&r has been registered.");
    }

    /// <summary>
    /// Determines whether the specified player can spawn in this team.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns><c>true</c> if the player can spawn; otherwise, <c>false</c>.</returns>
    public override bool IsSpawnable(ExPlayer player)
    {
        return !player.IsTutorial 
               && !player.IsInOverwatch 
               && (AllowedRoles.Count == 0 || AllowedRoles.Contains(player.Role)) 
               && !BlacklistedRoles.Contains(player.Role);
    }

    /// <summary>
    /// Selects the role for the specified player within this team.
    /// </summary>
    /// <param name="player">The player for whom to select a role.</param>
    /// <param name="selectedRoles">A dictionary of players and their selected roles.</param>
    /// <returns>The role selected for the player.</returns>    
    public override RoleTypeId SelectRole(ExPlayer player, Dictionary<ExPlayer, RoleTypeId> selectedRoles)
    {
        var loadout = Loadouts.GetRandomWeighted(kvp => kvp.Value);

        Selections.Remove(player.UserId);

        if (string.IsNullOrEmpty(loadout.Key))
            return DefaultRole;

        if (!LoadoutManager.TryGet(loadout.Key, out var loadoutDefinition))
        {
            ApiLog.Warn($"Loadout &3{loadout.Key}&r could not be found.");
            return DefaultRole;
        }

        Selections[player.UserId] = loadoutDefinition;

        if (!loadoutDefinition.Role.HasValue)
        {
            ApiLog.Warn($"Loadout &3{loadout.Key}&r does not have a role assigned.");
            return DefaultRole;
        }

        return loadoutDefinition.Role.Value;
    }

    private void OnPlayerLeft(ExPlayer player)
    {
        Selections.Remove(player.UserId);
    }

    private void OnRoundRestart()
    {
        Selections.Clear();
    }

    public static bool TrySpawnWave(List<ExPlayer> players, ConfigurableTeam targetTeam, SpawnableWaveBase originalWave)
    {
        var spawnDelay = 0f;
        var spawnTeam = originalWave.TargetFaction.GetSpawnableTeam();

        if (originalWave is IAnimatedWave animatedWave)
        {
            spawnDelay = animatedWave.AnimationDuration;
            
            WaveUpdateMessage.ServerSendUpdate(originalWave, UpdateMessageFlags.Trigger);
        }
        
        ApiLog.Debug("ConfigurableTeam", $"Spawning - playing effect (wait: &3{spawnDelay}&rs)");
        
        if (spawnDelay > 0f)
            Timing.CallDelayed(spawnDelay, () => InternalSpawn(true));
        else
            InternalSpawn(false);

        void InternalSpawn(bool wasDelayed)
        {
            if (wasDelayed)
            {
                ApiLog.Debug("ConfigurableTeam", $"Re-checking player list: &6{players.Count}&r");

                players.RemoveAll(p => !p.IsValidPlayer() || !p.IsAlive);

                if (players.Count < 1 || (targetTeam.MinPlayers > 0 && players.Count < targetTeam.MinPlayers))
                {
                    WaveManager.Spawn(originalWave);

                    ApiLog.Warn("Not enough valid players to spawn, respawning original team.");
                    return;
                }

                ApiLog.Debug("ConfigurableTeam", $"Validated player list: &3{players.Count}&r");
            }

            if (NamingRulesManager.TryGetNamingRule(spawnTeam, out var namingRule))
                NamingRulesManager.ServerGenerateName(spawnTeam, namingRule);
            
            var wave = targetTeam.Spawn(players);

            if (wave.SpawnedWave != null)
            {
                ApiLog.Debug("ConfigurableTeam", $"Spawned a new wave: &3{wave.SpawnedWave.Id}&r");

                if (targetTeam.ReplacementUseCassie)
                {
                    ApiLog.Debug("ConfigurableTeam", "Playing base-team CASSIE");

                    if (originalWave is IAnnouncedWave announcedWave)
                    {
                        announcedWave.Announcement.PlayAnnouncement(players.Select(x => x.ReferenceHub).ToList(), announcedWave);
                    }
                }
                else if (targetTeam.CassieAnnouncements.Count > 0)
                {
                    var announcement = targetTeam.CassieAnnouncements.RandomItem();

                    if (!string.IsNullOrEmpty(announcement))
                    {
                        ApiLog.Debug("ConfigurableTeam", $"Playing custom CASSIE: &3{announcement}&r");
                        Announcer.Message(announcement, string.Empty);
                    }
                    else
                    {
                        ApiLog.Warn("ConfigurableTeam", $"Selected an empty CASSIE announcement!");
                    }
                }
                else
                {
                    ApiLog.Debug("ConfigurableTeam", "No announcement set for this team.");
                }
            }
            else
            {
                WaveManager.Spawn(originalWave);

                ApiLog.Warn($"Failed to spawn replacement wave: &1{wave.SpawnFail}&r.");
            }
        }

        return true;
    }
    
    public static bool TryPickPlayers(out List<ExPlayer> players)
    {
        ApiLog.Debug("ConfigurableTeam", "Picking players for respawn");
        
        players = ExPlayer.Players
            .Where(p => p.ReferenceHub != null && p.Role.IsSpectator && !p.Role.IsOverwatch && WaveSpawner.CanBeSpawned(p.ReferenceHub))
            .ToList();
        
        ApiLog.Debug("ConfigurableTeam", $"Picked &3{players.Count}&r player(s)");
        return true;
    }
    
    public static bool TryPickTeam(List<ExPlayer> players, SpawnableWaveBase originalWave, out ConfigurableTeam configurableTeam)
    {
        ApiLog.Debug("ConfigurableTeam", $"Selecting configurable team with &3{players.Count}&r player(s) for &3{originalWave.TargetFaction}&r (&6{originalWave.GetType().Name}&r)");

        configurableTeam = null!;

        if (players.Count < 1)
        {
            ApiLog.Debug("ConfigurableTeam", "Not enough players to select!");
            return false;
        }

        var teamPlayers = ListPool<ExPlayer>.Shared.Rent();

        foreach (var kvp in CustomTeamRegistry.RegisteredHandlers)
        {
            if (kvp.Value is not ConfigurableTeam team)
                continue;

            if (!team.ReplacedFaction.HasValue)
                continue;

            if (originalWave.TargetFaction != team.ReplacedFaction.Value)
                continue;
            
            teamPlayers.AddRangeWhere(players.OrderByDescending(p => WaveSpawner.CalculatePriority(p.ReferenceHub, originalWave.TargetFaction.GetSpawnableTeam())), team.IsSpawnable);

            if (team.MinPlayers > 0 && teamPlayers.Count < team.MinPlayers)
                continue;
            
            if (team.TokenCost > 0 && originalWave is ILimitedWave limitedWave && limitedWave.RespawnTokens < team.TokenCost)
                continue;

            if (team.ReplacementChance <= 0f)
                continue;

            if (team.ReplacementChance < 100f && !WeightUtils.GetBool(team.ReplacementChance))
                continue;

            configurableTeam = team;
            
            players.Clear();
            
            players.AddRange(team.MaxPlayers > 0 && teamPlayers.Count > team.MaxPlayers 
                ? teamPlayers.Take(team.MaxPlayers) 
                : teamPlayers);
            
            ListPool<ExPlayer>.Shared.Return(teamPlayers);
            
            ApiLog.Debug("ConfigurableTeam", $"Selected configurable team &3{team.Name}&r for &3{originalWave.TargetFaction}&r (&6{originalWave.GetType().Name}&r)");
            return true;
        }
        
        ListPool<ExPlayer>.Shared.Return(teamPlayers);

        ApiLog.Debug("ConfigurableTeam", $"Failed to select configurable team for &3{originalWave.TargetFaction}&r (&6{originalWave.GetType().Name}&r)");
        return false;
    }

    private static void Initialize()
    {
        var directory = Path.Combine(ObscurisPlugin.RootDirectory, "teams");
        var exampleFile = Path.Combine(directory, "example.yml");

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        FileUtils.TrySaveYamlFile(exampleFile, new ConfigurableTeam() { configId = "example" });

        foreach (var file in Directory.GetFiles(directory, "*.yml"))
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file);

                if (name != "example")
                {
                    if (FileUtils.TryLoadYamlFile<ConfigurableTeam>(file, out var team))
                    {
                        team.configId = name;
                        
                        CustomTeamRegistry.Register(team);
                    }
                    else
                    {
                        ApiLog.Warn($"Failed to load team configuration from file: &1{file}&r.");
                    }
                }
            }
            catch (Exception ex)
            {
                ApiLog.Warn($"Failed to load team configuration from file: &1{file}&r. Exception: &1{ex.Message}&r");
            }
        }
    }
}