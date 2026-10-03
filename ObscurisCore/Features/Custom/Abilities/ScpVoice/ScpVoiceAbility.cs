using System.Text;

using LabApi.Events.Arguments.PlayerEvents;

using LabExtended.API.Custom.Abilities;
using LabExtended.API.Settings;

using LabExtended.Core;

using LabExtended.Events;
using LabExtended.Events.Player.Settings;

using NiveraAPI.IO.Configs;
using PlayerRoles;

using YamlDotNet.Serialization;

namespace ObscurisCore.Features.Custom.Abilities.ScpVoice;

/// <summary>
/// Represents the SCP Voice ability, allowing players to communicate with SCPs using voice.
/// </summary>
public class ScpVoiceAbility : CustomAbility
{
    [Config("scp-voice", "ability", "Configuration of the SCP voice ability.")]
    private static ScpVoiceAbility config = new();

    [Config("scp-voice", "print-string", "String to be printed in the overlay.")]
    private static string printString = "Mode: {0}";
    
    /// <summary>
    /// The unique identifier for the ability.
    /// </summary>
    public override string Id { get; } = "scp_voice";

    /// <summary>
    /// Whether to add the ability to new players joining the server.
    /// </summary>
    public override bool AddOnJoin { get; set; } = false;

    /// <summary>
    /// Whether to enable the ability on new players joining the server.
    /// </summary>
    public override bool EnableOnJoin { get; set; } = false;

    /// <summary>
    /// Cooldown of the ability.
    /// </summary>
    public override float Cooldown { get; set; } = 0f;

    /// <summary>
    /// Duration of the ability.
    /// </summary>
    public override float Duration { get; set; } = 0f;

    /// <summary>
    /// Maximum number of uses for the ability.
    /// </summary>
    public override int MaxUses { get; set; } = 0;

    /// <summary>
    /// Gets the current status mode for SCP voice communication. Determines whether the voice
    /// is restricted to SCP-only channels, proximity-based communication, or a combination of both.
    /// </summary>
    [YamlIgnore]
    public ScpVoiceStatus Mode { get; set; } = ScpVoiceStatus.Scp;

    /// <summary>
    /// Represents the menu functionality associated with the SCP Voice ability.
    /// This menu allows for the customization and configuration of the SCP Voice settings
    /// within the game, providing users with an interface to adjust relevant parameters.
    /// </summary>
    [YamlIgnore]
    public ScpVoiceMenu Menu { get; private set; }
    
    /// <summary>
    /// Gets the custom voice profile associated with this ability.
    /// </summary>
    [YamlIgnore]
    public ScpVoiceProfile Profile { get; private set; }

    /// <summary>
    /// Executes logic when the ability is enabled. This method is called to initialize and activate
    /// the ability's functionality. Specifically, it adds a custom voice profile to enable SCP voice behavior.
    /// </summary>
    public override void OnEnabled()
    {
        base.OnEnabled();

        if (Player.IsSCP)
        {
            Profile = Player.Voice.AddProfile<ScpVoiceProfile>(true);
            Profile.Ability = this;

            ApiLog.Debug($"Player {Player.ToLogString()} enabled SCP voice ability");
        }
    }

    /// <summary>
    /// Handles the behavior when the ability is disabled. Removes the associated SCP voice profile
    /// from the player's voice system to ensure the custom voice functionality is no longer applied.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();

        Player.Voice?.RemoveProfile<ScpVoiceProfile>();
        Profile = null!;
        
        ApiLog.Debug($"Player {Player.ToLogString()} disabled SCP voice ability");
    }

    /// <summary>
    /// Handles the behavior when the ability is used. Toggles the SCP voice mode between the available
    /// statuses (Scp, Proximity, and Mixed) in a cyclical manner. Logs the updated voice mode for the player,
    /// providing a debug output to track changes in the SCP voice functionality.
    /// </summary>
    public override void OnUsed()
    {
        base.OnUsed();

        switch (Mode)
        {
            case ScpVoiceStatus.Scp:
                Mode = ScpVoiceStatus.Proximity;
                break;

            case ScpVoiceStatus.Proximity:
                Mode = ScpVoiceStatus.Mixed;
                break;

            case ScpVoiceStatus.Mixed:
                Mode = ScpVoiceStatus.Scp;
                break;
        }
        
        ApiLog.Debug($"Player {Player.ToLogString()} changed SCP voice mode to &3{Mode}&r");
    }

    /// <summary>
    /// Appends a formatted string representing the current state of the SCP voice ability to the provided StringBuilder.
    /// Includes the mode of the ability as defined by the current <see cref="ScpVoiceStatus"/>.
    /// </summary>
    /// <param name="builder">The StringBuilder to which the formatted string will be appended.</param>
    /// <returns>
    /// A boolean value indicating success. Returns true if the string was successfully appended, otherwise false.
    /// </returns>
    public override bool Print(StringBuilder builder)
    {
        if (!string.IsNullOrEmpty(printString))
        {
            builder.AppendFormat(printString, Mode);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles behavior when a player's role changes. Enables or disables the ability
    /// based on whether the player's new role belongs to the SCP team.
    /// </summary>
    /// <param name="args">Arguments containing information about the player's role change,
    /// such as the previous role and the new role.</param>
    public override void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        base.OnChangedRole(args);

        if (Player == null)
            return;

        if (args.NewRole.Team != Team.SCPs)
        {
            Disable();
        }
        else
        {
            Enable();
        }
    }

    private static void OnEntryCreated(PlayerSettingsEntryCreatedEventArgs args)
    {
        if (args.Menu is not ScpVoiceMenu menu)
            return;

        if (args.Player.HasAbility<ScpVoiceAbility>())
            return;

        if (args.Player.AddAbility<ScpVoiceAbility>(out var ability))
        {
            ability.Menu = menu;
            
            if (args.Player.IsSCP)
                ability.Enable();
            else
                ability.Disable();
            
            menu.Ability = ability;
        }
        else
        {
            ApiLog.Error($"Failed to add SCP voice ability to player {args.Player.ToLogString()}");
        }
    }

    private static void Initialize()
    {
        config.Register();

        ExPlayerEvents.SettingsEntryCreated += OnEntryCreated;
        
        SettingsManager.AddBuilder(new SettingsBuilder("scp_voice_menu_builder")
            .WithMenu<ScpVoiceMenu>());
    }
}