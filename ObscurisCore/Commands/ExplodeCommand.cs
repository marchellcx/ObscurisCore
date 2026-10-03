using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using ObscurisCore.Extensions;

namespace ObscurisCore.Commands
{
    /// <summary>
    /// Represents a server-side command that explodes specified players using a given item and velocity.
    /// </summary>
    [Command("explode", "Explodes specified players with given item and velocity.")]
    public class ExplodeCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("Explodes specified players with given item and velocity.", "explode")]
        private void Execute(
            [CommandParameter("Players", "List of players to explode.")] List<ExPlayer> players,
            [CommandParameter("Item", "The item to use for the explosion.")] ItemType item,
            [CommandParameter("Amount", "Amount of explosions to spawn.")] int amount,
            [CommandParameter("Velocity", "The velocity of the explosion.")] float velocity,
            [CommandParameter("Reason", "The reason for the explosion.")] string reason = "Game Over")
        {
            var count = 0;

            players.ForEach(p =>
            {
                if (p.Explode(amount, item, reason, false, true, velocity))
                {
                    count++;
                }
            });

            if (count > 0)
            {
                Ok($"Successfully exploded {count} players.");
            }
            else
            {
                Fail("No players were exploded.");
            }
        }
    }
}