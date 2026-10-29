using LabExtended.API;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;

using LabExtended.Events.Player;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Elements.Alerts;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons;

/// <summary>
/// Represents a custom firearm item known as the "Move Gun."
/// This weapon allows players to manipulate the movement of other players
/// by applying a force either pushing them away or pulling them closer.
/// </summary>
public class MoveGun : CustomFirearm
{
    /// <summary>
    /// A singleton instance of the "Move Gun," a custom firearm item that enables players
    /// to manipulate the movement of others by applying a force that either pushes them away
    /// or pulls them closer.
    /// </summary>
    public static readonly MoveGun Singleton = new();
    
    /// <summary>
    /// Represents the movement data associated with the MoveGun.
    /// This includes configuration for the direction, strength, and duration
    /// of the force applied to manipulate the player's movement.
    /// </summary>
    public class MoveData
    {
        /// <summary>
        /// Whether the gun is pushing or pulling the player.
        /// </summary>
        public bool IsPushing = true;

        /// <summary>
        /// The strength of the force applied by the gun when interacting with the player.
        /// </summary>
        public float Force = 1f;

        /// <summary>
        /// The duration of the effect applied by the gun when interacting with the player.
        /// </summary>
        public float Duration = 1f;
    }

    /// <summary>
    /// The unique identifier for the MoveGun object.
    /// </summary>
    public override string Id { get => "MoveGun"; }

    /// <summary>
    /// The display name of the Move Gun item.
    /// </summary>
    public override string Name { get => "Move Gun"; }

    /// <summary>
    /// Represents the type of item that can be picked up for the Move Gun.
    /// </summary>
    public override ItemType PickupType { get; set; } = ItemType.GunCOM15;

    /// <summary>
    /// Represents the type of inventory associated with the item.
    /// Specifies the category or classification of the item within the inventory system.
    /// </summary>
    public override ItemType InventoryType { get; set; } = ItemType.GunCOM15;

    /// <summary>
    /// Invoked when the custom item is added to a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing information about the custom item being added.</param>
    public override void OnItemAdded(CustomItemAddedEventArgs args)
    {
        base.OnItemAdded(args);

        var data = new MoveData();

        args.AddedData = data;
        
        Announce(args.Player, data, false);
    }

    /// <summary>
    /// Invoked when the custom item becomes the active item in a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing information about the player and their inventory state.</param>
    /// <param name="itemData">The data associated with the custom item being selected.</param>
    public override void OnSelected(PlayerSelectedItemEventArgs args, ref object? itemData)
    {
        base.OnSelected(args, ref itemData);

        if (itemData is not MoveData moveData)
            return;
        
        args.Player.Toggles.HasUnlimitedAmmo = true;
        
        Announce(args.Player, moveData, true);
    }

    /// <summary>
    /// Invoked when the custom item is unselected and is no longer the active item in a player's inventory.
    /// </summary>
    /// <param name="args">The event arguments containing information about the player and their inventory state.</param>
    /// <param name="itemData">The data associated with the custom item being unselected.</param>
    public override void OnUnselected(PlayerSelectedItemEventArgs args, ref object? itemData)
    {
        base.OnUnselected(args, ref itemData);
        
        args.Player.Toggles.HasUnlimitedAmmo = false;
    }

    /// <summary>
    /// Invoked when a player fires the custom firearm.
    /// </summary>
    /// <param name="args">The event arguments containing information about the player and the firearm's state during the shot.</param>
    /// <param name="firearmData">A reference to the firearm's custom data, which may include information about its behavior and mechanics.</param>
    public override void OnShot(PlayerShotFirearmEventArgs args, ref object? firearmData)
    {
        base.OnShot(args, ref firearmData);

        if (firearmData is not MoveData moveData)
            return;

        if (args.TargetPlayer?.ReferenceHub == null || !args.TargetPlayer.IsAlive)
            return;

        if (moveData.IsPushing)
            args.TargetPlayer.Push(args.TargetPlayer.GetMoveFactor(), moveData.Force, moveData.Duration, args.Player.CameraTransform.forward.NormalizeIgnoreY());
        else
            args.TargetPlayer.Pull(args.TargetPlayer.GetMoveFactor(), moveData.Force, moveData.Duration, -args.Player.CameraTransform.forward.NormalizeIgnoreY());
    }

    private void Announce(ExPlayer player, MoveData data, bool isSelection)
    {
        var mode = data.IsPushing ? "Push" : "Pull";
        
        if (isSelection)
        {
            player.SendAlert(AlertType.Info, 5f, "Move Gun", $"Držíš <color=red>Move Gun</color>\nMód: <color=yellow>{mode}</color>\nSíla: <color=yellow>{data.Force}</color>\nDélka: <color=yellow>{data.Duration}s</color>");
        }
        else
        {
            player.SendAlert(AlertType.Info, 5f, "Move Gun", $"Zvedl jsi <color=red>Move Gun</color>\nMód: <color=yellow>{mode}</color>\nSíla: <color=yellow>{data.Force}</color>\nDélka: <color=yellow>{data.Duration}s</color>");
        }
    }

    private static void Initialize()
    {
        Singleton.Register();
    }
}