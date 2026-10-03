using LabExtended.API.Custom.Items;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using LabExtended.Utilities;
using UnityEngine;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons.ItemLauncher
{
    /// <summary>
    /// Represents a server-side command that creates and registers a new item launcher with specified parameters.
    /// </summary>
    [Command("launcher", "Creates and registers a new item launcher.")]
    public class ItemLauncherCommand : CommandBase, IServerSideCommand
    {
        /// <summary>
        /// Creates and registers a new item launcher with the specified parameters.
        /// </summary>
        [CommandOverload("Creates and registers a new item launcher with the specified parameters.", null)]
        public void Invoke(
             [CommandParameter("ID", "The ID of the launcher.")] string launcherId,
             [CommandParameter("Item", "The type of the item to launch.")] ItemType launchedItem, 
             [CommandParameter("Firearm", "The firearm used to launch the item.")] ItemType firearmType, 
             [CommandParameter("Save", "Whether or not to save the launcher to the plugin's config file.")] bool saveLauncher = true, 
             [CommandParameter("Force", "The force to launch the item with.")] float launchForce = 3f,
             [CommandParameter("Scale", "The scale of launched items.")] Vector3 scale = default)
        {
            if (CustomItem.RegisteredObjects.ContainsKey(launcherId))
            {
                Fail($"An item with ID '{launcherId}' is already registered.");
                return;
            }

            if (scale == default) scale = Vector3.one;

            var launcher = new ItemLauncher();

            launcher.launcherId = launcherId;

            launcher.PickupType = firearmType;
            launcher.InventoryType = firearmType;

            launcher.LaunchedItem = launchedItem;
            launcher.Force = launchForce;

            launcher.Scale = new(scale);

            if (saveLauncher)
            {
                var directory = Path.Combine(ObscurisPlugin.RootDirectory, "item_launchers");
                var path = Path.Combine(directory, launcherId);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                FileUtils.TrySaveYamlFile(path, launcher);
            }

            if (launcher.Register())
                Ok($"Registered a new launcher.");
            else
                Fail("Failed to register the launcher.");
        }
    }
}