using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

namespace ObscurisCore.Features.Coin.Commands;

/// <summary>
/// Coin commands.
/// </summary>
[Command("coin", "Base command for coin utilities")]
public class CoinCommand : CommandBase, IServerSideCommand
{
    [CommandOverload("Executes a random coin action on a player as if they flipped a coin.", null)]
    private void Invoke(
        [CommandParameter("Player", "The player to invoke the coin action on.")] ExPlayer player, 
        [CommandParameter("AllowMultiple", "Whether or not multiple actions can be performed.")] bool allowMultiple = false)
    {
        Ok(x =>
        {
            if (CoinManager.ExecuteCoinFlip(player, allowMultiple))
            {
                x.Append($"Successfully invoked a coin action on {player.ToCommandString()}!");
            }
            else
            {
                x.Append($"Failed to invoke a coin action on {player.ToCommandString()}!");
            }
        });
    }
}