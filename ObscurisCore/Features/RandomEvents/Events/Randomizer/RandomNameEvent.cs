using System.ComponentModel;
using LabExtended.API;
using NiveraAPI.Utilities;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.RandomEvents.Events.Randomizer;

/// <summary>
/// Represents a random event that changes the name of a player.
/// </summary>
public class RandomNameEvent : RandomEventBase
{
    /// <summary>
    /// Gets the unique identifier for the random event.
    /// </summary>
    public override string Id { get; } = "randomName";

    /// <summary>
    /// Gets or sets a value indicating whether the event can be grouped with other events.
    /// </summary>
    public override bool CanBeGrouped { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the event can be activated during the mid-round.
    /// </summary>
    public override bool CanActivateMidRound { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the event should be disabled when the round ends.
    /// </summary>
    public override bool ShouldDisableOnRoundEnd { get; set; } = true;

    /// <summary>
    /// Gets or sets the names to be used for the random event.
    /// </summary>
    public string[] Names { get; set; } =
    [
        "Diddy"
    ];
    
    /// <summary>
    /// The message to be sent to players when the event is enabled.
    /// </summary>
    [Description("The message to be sent to players when the event is enabled. ($Name can be used for the player's name.)")]
    public string EnabledMessage { get; set; }
    
    /// <summary>
    /// The message to be sent to players when they join the server with the event enabled.
    /// </summary>
    [Description("The message to be sent to players when they join the server with the event enabled. ($Name can be used for the player's name.)")]
    public string JoinedMessage { get; set; }

    /// <summary>
    /// Handles the initialization logic when the random event is enabled.
    /// </summary>
    public override void OnEnabled()
    {
        foreach (var player in ExPlayer.Players)
        {
            if (Names.Length >= ExPlayer.Count)
            {
                var name = Names.RandomItem();

                while (ExPlayer.Players.Any(p => p.DisplayName == name))
                    name = Names.RandomItem();

                player.DisplayName = name;
                player.SendFormattedAlert(EnabledMessage.Replace("$Name", player.DisplayName), true, AlertType.Info, 5f, "Random Name");
            }
            else
            {
                player.DisplayName = Names.GetRandomWeighted(name => 100f - ExPlayer.Players.Count(p => p.DisplayName == name));
                player.SendFormattedAlert(EnabledMessage.Replace("$Name", player.DisplayName), true, AlertType.Info, 5f, "Random Name");
            }
        }
    }

    /// <summary>
    /// Handles the cleanup logic when the random event is disabled.
    /// </summary>
    public override void OnDisabled()
    {
        base.OnDisabled();

        foreach (var player in ExPlayer.Players)
            player.DisplayName = null!;
    }

    /// <summary>
    /// Called when a player joins the game during the execution of this random event.
    /// </summary>
    /// <param name="player">The player instance that has joined the game.</param>
    public override void OnPlayerJoined(ExPlayer player)
    {
        if (Names.Length >= ExPlayer.Count)
        {
            var name = Names.RandomItem();
            
            while (ExPlayer.Players.Any(p => p.DisplayName == name))
                name = Names.RandomItem();
            
            player.DisplayName = name;
            player.SendFormattedAlert(JoinedMessage.Replace("$Name", player.DisplayName), true, AlertType.Info, 5f, "Random Name");
        }
        else
        {
            player.DisplayName = Names.GetRandomWeighted(name => 100f - ExPlayer.Players.Count(p => p.DisplayName == name));
            player.SendFormattedAlert(JoinedMessage.Replace("$Name", player.DisplayName), true, AlertType.Info, 5f, "Random Name");
        }
    }
}