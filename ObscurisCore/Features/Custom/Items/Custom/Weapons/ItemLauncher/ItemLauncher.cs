using System.ComponentModel;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API;
using LabExtended.API.Custom.Items;

using LabExtended.Attributes;
using LabExtended.Core.Configs.Objects;
using LabExtended.Events.Player;

using LabExtended.Utilities;
using UnityEngine;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons.ItemLauncher;

/// <summary>
/// A firearm that launches items.
/// </summary>
[LoaderIgnore]
public class ItemLauncher : CustomFirearm
{
    internal string launcherId;

    /// <inheritdoc/>
    public override string Id => launcherId!;

    /// <inheritdoc/>
    public override string Name { get; } = "Item Launcher";

    /// <inheritdoc/>
    public override ItemType PickupType { get; set; } = ItemType.GunCOM15;

    /// <inheritdoc/>
    public override ItemType InventoryType { get; set; } = ItemType.GunCOM15;

    /// <summary>
    /// Gets or sets the identifier of the item to be launched. The value can be a standard item type or a custom
    /// item ID.
    /// </summary>
    [Description("Sets the item that will be launched.")]
    public ItemType LaunchedItem { get; set; } = ItemType.GrenadeHE;

    /// <summary>
    /// Gets or sets the force with which the item will be launched.
    /// </summary>
    [Description("Sets the force with which the item will be launched. Default is 3.")]
    public float Force { get; set; } = 3f;

    /// <summary>
    /// Gets or sets the fuse time, in seconds, for throwable projectiles after they are launched.
    /// </summary>
    [Description("Sets the fuse time for throwable projectiles when launched.")]
    public float FuseTime { get; set; } = 3f;

    /// <summary>
    /// Gets or sets the number of items to launch per shot.
    /// </summary>
    [Description("Sets how many items to launch per shot.")]
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the scale applied to launched items.
    /// </summary>
    [Description("Sets the scale of launched items.")]
    public YamlVector3 ItemScale { get; set; } = new(Vector3.one);

    /// <inheritdoc/>
    public override void OnShooting(PlayerShootingFirearmEventArgs args, ref object? firearmData)
    {
        base.OnShooting(args, ref firearmData);

        args.IsAllowed = false;

        if (!LaunchedItem.TryGetTemplate<ItemBase>(out var template))
            return;

        if (template is ThrowableItem throwable)
            ThrowProjectileItem(args.Player, throwable);
        else
            ThrowBasicItem(args.Player, template);
    }

    private void ThrowBasicItem(ExPlayer player, ItemBase template)
    {
        for (var i = 0; i < Amount; i++)
            player.Inventory.ThrowItem<ItemPickupBase>(template.ItemTypeId, Force, ItemScale.Vector);
    }

    private void ThrowProjectileItem(ExPlayer player, ThrowableItem template)
    {
        for (var i = 0; i < Amount; i++)
            ExMap.SpawnProjectile(template.ItemTypeId, player.CameraTransform.position, ItemScale.Vector,
                player.CameraTransform.forward * Force, player.Rotation, Force, FuseTime);
    }

    private static void Initialize()
    {
        var path = Path.Combine(ObscurisPlugin.RootDirectory, "item_launchers");
        var example = Path.Combine(path, "example.yml");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var exampleLauncher = new ItemLauncher();

        exampleLauncher.launcherId = "example_launcher";

        FileUtils.TrySaveYamlFile(example, exampleLauncher);

        foreach (var file in Directory.GetFiles(path, "*.yml"))
        {
            if (file == example)
                continue;

            if (!FileUtils.TryLoadYamlFile<ItemLauncher>(file, out var launcher))
                continue;

            launcher.launcherId = Path.GetFileNameWithoutExtension(file);
            launcher.Register();
        }
    }
}