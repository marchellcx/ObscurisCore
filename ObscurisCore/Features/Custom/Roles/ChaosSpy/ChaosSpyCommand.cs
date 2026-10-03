using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

namespace ObscurisCore.Features.Custom.Roles.ChaosSpy;

/// <summary>
/// Chaos Spy commands.
/// </summary>
[Command("cispy", "Chaos Spy commands")]
public class ChaosSpyCommand : CommandBase, IServerSideCommand
{
    [CommandOverload("list", "Lists all active chaos spy instances.", null)]
    private void List()
    {
        Ok(x =>
        {
            if (ChaosSpyManager.Spies.Count == 0)
            {
                x.Append("No active chaos spy instances.");
            }
            else
            {
                x.AppendLine($"Active chaos spy instances: {ChaosSpyManager.Spies.Count}");

                foreach (var ply in ChaosSpyManager.Spies)
                {
                    x.AppendLine($"- {ply.ToCommandString()}");
                }
            }
        });
    }

    [CommandOverload("set", "Sets the chaos spy role for a player.", null)]
    private void Set([CommandParameter("Player", "The player to add to spies.")] ExPlayer player)
    {
        Ok(x =>
        {
            if (ChaosSpyManager.Spies.Contains(player))
            {
                x.Append($"The player {player.ToCommandString()} is already a Chaos Spy.");
                return;
            }
            
            if (ChaosSpyManager.SpawnChaosSpy(player))
            {
                x.Append($"Successfully assigned the Chaos Spy role to {player.ToCommandString()}.");
            }
            else
            {
                x.Append($"Failed to assign the Chaos Spy role to {player.ToCommandString()}.");
            }
        });
    }

    [CommandOverload("remove", "Removes the chaos spy role from a player.", null)]
    private void Remove([CommandParameter("Player", "The player to remove from spies.")] ExPlayer player)
    {
        Ok(x =>
        {
            if (!ChaosSpyManager.Spies.Remove(player))
            {
                x.Append($"The player {player.ToCommandString()} is not a Chaos Spy.");
            }
            else
            {
                x.Append($"Successfully removed the Chaos Spy role from {player.ToCommandString()}.");
            }
        });
    }
}