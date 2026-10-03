using LabExtended.API;
using LabExtended.Commands;

using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using MapGeneration;

using UnityEngine;

namespace ObscurisCore.Features.RandomPickup.Commands
{
    /// <summary>
    /// Provides server-side commands for managing the Random Pickup system.
    /// </summary>
    [Command("randompickup", "Manages the Random Pickup system", "rpu")]
    public class RandomPickupCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("list", "Lists all active instances.", "randompickup.list")]
        private void List()
        {
            if (RandomPickupManager.Instances.Count(x => x.Value != null) == 0)
            {
                Fail("There are no active Random Pickup instances.");
                return;
            }

            Ok(x =>
            {
                x.AppendLine();

                foreach (var pair in RandomPickupManager.Instances)
                {
                    var instance = pair.Value;

                    if (instance == null)
                        continue;

                    x.AppendLine($"[{pair.Key}] | Schematic: {instance.Properties.Schematic} " +
                        $"| Position: {instance.Position} ({(instance.Position.TryGetRoom(out var room) ? $"{room.Zone}/{room.Name}" : "Unknown Room")})");
                }
            });
        }

        [CommandOverload("spawn", "Spawns a new instance.", "randompickup.spawn")]
        private void Spawn(
            [CommandParameter("Position", "The position to spawn the schematic at (can also be a player ID).")]
            [CommandParameter(ParserType = typeof(ExPlayer), ParserProperty = "Position.Position")]
            Vector3 position)
        {
            var props = RandomPickupManager.Config.GlobalProperties;
            var instance = RandomPickupManager.Spawn(props.Schematic, position, Quaternion.identity, props);

            if (instance != null)
                Ok($"Spawned Random Pickup instance with ID {instance.Id} at {position}.");
            else
                Fail("Failed to spawn Random Pickup instance. Check server logs for details.");
        }
    }
}