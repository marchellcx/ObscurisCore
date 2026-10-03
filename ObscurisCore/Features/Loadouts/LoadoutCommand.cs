using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

namespace ObscurisCore.Features.Loadouts;

/// <summary>
/// Loadout commands.
/// </summary>
[Command("loadout", "Loadout management commands.")]
public class LoadoutCommand : CommandBase, IServerSideCommand
{
    [CommandOverload("list", "Lists all loaded loadouts.", null)]
    private void List()
    {
        if (LoadoutManager.Loadouts.Count == 0)
        {
            Fail("No loadouts were loaded.");
            return;
        }

        Ok(x =>
        {
            x.AppendLine();

            foreach (var loadout in LoadoutManager.Loadouts)
            {
                x.AppendLine($"- {loadout.Name} ({loadout.Items.Count} items; {loadout.Ammo.Count} ammo items)");
            }
        });
    }

    [CommandOverload("apply", "Applies a loadout.", null)]
    private void Apply(
        [CommandParameter("Name", "Name of the loadout.")] string name,
        [CommandParameter("Player", "The target player (defaults to you if not specified.)")] ExPlayer? player = null)
    {
        player ??= Sender;

        if (LoadoutManager.TryApply(player, name))
        {
            Ok($"Loadout '{name}' applied to '{player.Nickname} ({player.UserId})'");
        }
        else
        {
            Fail($"Loadout '{name}' could not be applied - check the console for more details.");
        }
    }
}