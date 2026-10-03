using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using LabExtended.Core;
using ObscurisCore.Features;
using ObscurisCore.Utilities;

using UnityEngine;

namespace ObscurisCore.Commands
{
    /// <summary>
    /// Provides server-side commands to start or stop an item rain effect on specified players.
    /// </summary>
    /// <remarks>This command is intended for administrative use to control item rain events for one or more
    /// players. The command supports starting and stopping item rain with customizable parameters such as item type,
    /// amount, delay, and scale. Only users with appropriate permissions should execute these commands, as they affect
    /// gameplay for targeted players.</remarks>
    [Command("rain", "Starts an item rain on the specified players.")]
    public class ItemRainCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("start", "Starts an item rain on the specified players.", "rain.start")]
        private void Start(
            [CommandParameter("Targets", "List of players to start the rain on.")] List<ExPlayer> targets, 
            [CommandParameter("Type", "The item type that should rain.")] ItemType item,
            [CommandParameter("Amount", "The amount of items that should be spawned per drop.")] int amount,
            [CommandParameter("Delay", "The delay between each drop (in milliseconds).")] int delay,
            [CommandParameter("Limit", "The maximum amount of items to drop (0 for infinite).")] int limit,
            [CommandParameter("Fuse", "The duration of a projectile's fuse time - will be spawned as a regular grenade if less than zero (in seconds).")] float fuse,
            [CommandParameter("Scale", "Scale of dropped items (defaults to one)")] Vector3 scale = default)
        {
            try
            {
                if (scale == default)
                    scale = Vector3.one;

                foreach (var target in targets)
                    ItemRain.StartItemRain(target, item, amount, delay, limit, fuse >= 0f, fuse, scale);

                Ok("Started item rain on " + targets.Count + " players.");
            }
            catch (Exception ex)
            {
                ApiLog.Error("ItemRainCommand", ex);
            }
        }

        [CommandOverload("stop", "Stops an item rain on the specified players.", "rain.stop")]
        private void Stop(
            [CommandParameter("Targets", "The list of players to stop the rain on.")] List<ExPlayer> targets)
        {
            var amount = 0;

            foreach (var target in targets)
            {
                if (ItemRain.StopItemRain(target))
                {
                    amount++;
                }
            }

            Ok("Stopped item rain on " + amount + " players.");
        }
    }
}
