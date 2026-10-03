using System.ComponentModel;

using LabExtended.Core;

using LabExtended.API;
using LabExtended.API.Custom.Teams;

using LabExtended.Utilities;

using ObscurisCore.Features.Loadouts;

using PlayerRoles;

using YamlDotNet.Serialization;

using LabExtended.Events;

using LabApi.Events.Arguments.ServerEvents;

using ObscurisCore.Extensions;

using MEC;

using LabApi.Events.Handlers;

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

        ServerEvents.WaveRespawning += OnWaveRespawning;

        ApiLog.Info($"Configurable team &3{configId}&r has been registered.");
    }

    /// <summary>
    /// Determines whether the specified player can spawn in this team.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns><c>true</c> if the player can spawn; otherwise, <c>false</c>.</returns>
    public override bool IsSpawnable(ExPlayer player)
    {
        return !player.IsTutorial && !player.IsInOverwatch && (AllowedRoles.Count == 0 || AllowedRoles.Contains(player.Role.Type));
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

    private void OnWaveRespawning(WaveRespawningEventArgs args)
    {
        if (!ReplacedFaction.HasValue)
            return;

        if (args.Wave.Faction != ReplacedFaction.Value)
            return;

        if (ReplacementChance <= 0f)
            return;

        if (ReplacementChance < 100f && !WeightUtils.GetBool(ReplacementChance))
            return;

        args.IsAllowed = false;

        var list = new List<ExPlayer>();

        foreach (var ply in args.SpawningPlayers)
        {
            if (!ply.CastPlayer(out var exPlayer))
                continue;

            list.Add(exPlayer);
        }

        args.Wave.PlayRespawnEffect();

        Timing.CallDelayed(args.Wave.AnimationTime, () => 
        {
            list.RemoveAll(p => !p.IsValidPlayer());

            if (list.Count < 1)
            {
                ApiLog.Warn("No valid players to spawn.");
                return;
            }

            var wave = Spawn(list);

            if (wave.SpawnedWave != null)
            {
                if (ReplacementUseCassie)
                {
                    args.Wave.PlayAnnouncement();
                }
                else if (CassieAnnouncements.Count > 0)
                {
                    var announcement = CassieAnnouncements.RandomItem();

                    LabApi.Features.Wrappers.Cassie.Message(announcement);
                }
            }
            else
            {
                ApiLog.Warn($"Failed to spawn replacement wave: &1{wave.SpawnFail}&r.");
            }
        });
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