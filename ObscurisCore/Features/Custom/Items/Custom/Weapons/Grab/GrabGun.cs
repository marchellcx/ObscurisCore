using AdminToys;

using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using LabExtended.API;

using LabExtended.API.Custom.Items;
using LabExtended.API.Custom.Items.Events;

using LabExtended.API.Settings;

using LabExtended.Events.Player;

using LabExtended.Extensions;
using ObscurisCore.Features.Rays;

using UnityEngine;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons.Grab
{
    /// <summary>
    /// Represents a custom firearm that allows players to grab and manipulate objects or other players.
    /// </summary>
    public class GrabGun : CustomFirearm
    {
        /// <summary>
        /// Gets the combined layer mask used for grabbing pickups.
        /// </summary>
        public static LayerMask PickupLayerMask { get; } = LayerMask.GetMask("InteractableNoPlayerCollision");

        /// <summary>
        /// The singleton instance of the grab gun.
        /// </summary>
        public static GrabGun Singleton { get; private set; }

        /// <inheritdoc/>
        public override string Id { get; } = "grab_gun";

        /// <inheritdoc/>
        public override string Name { get; } = "Grab Gun";

        /// <inheritdoc/>
        public override ItemType PickupType { get; set; } = ItemType.None;

        /// <inheritdoc/>
        public override ItemType InventoryType { get; set; } = ItemType.GunCOM15;

        /// <summary>
        /// Releases the specified item, optionally returning it to its original position.
        /// </summary>
        /// <param name="item">The item to release. Cannot be null.</param>
        /// <param name="toOriginalPosition">true to return the item to its original position; otherwise, false to release it in its current location.</param>
        /// <returns>true if the item was successfully released; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if item is null.</exception>
        public bool Release(ItemBase item, bool toOriginalPosition)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CheckItem<GrabHandler>(item, out var grabHandler))
                return false;

            return grabHandler.Release(toOriginalPosition);
        }

        /// <summary>
        /// Attempts to launch the specified item with the given speed, duration, and gravity parameters.
        /// </summary>
        /// <param name="item">The item to be launched. Cannot be null.</param>
        /// <param name="speed">The initial speed at which to launch the item.</param>
        /// <param name="duration">The duration, in seconds, for which the launch effect should persist.</param>
        /// <param name="gravity">The gravity value to apply to the launched item.</param>
        /// <returns>true if the item was successfully launched; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the item parameter is null.</exception>
        public bool Launch(ItemBase item, float speed, float duration, float gravity)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CheckItem<GrabHandler>(item, out var grabHandler))
                return false;

            return grabHandler.Launch(speed, duration, gravity);
        }

        /// <inheritdoc/>
        public override void OnRegistered()
        {
            base.OnRegistered();

            SettingsManager.AddBuilder(new SettingsBuilder("GrabMenuBuilder")
                .WithMenu(() => new GrabMenu()));

            GrabMenu.Initialize();

            Singleton = this;
        }

        /// <inheritdoc/>
        public override void OnItemAdded(CustomItemAddedEventArgs args)
        {
            base.OnItemAdded(args);

            args.AddedData = new GrabHandler(args.Player, args.AddedItem);
        }

        /// <inheritdoc/>
        public override void OnItemDestroyed(CustomItemDestroyedEventArgs args)
        {
            base.OnItemDestroyed(args);

            if (args.Data is GrabHandler grabHandler)
                grabHandler.Dispose();
        }

        /// <inheritdoc/>
        public override void OnShooting(PlayerShootingFirearmEventArgs args, ref object? firearmData)
        {
            base.OnShooting(args, ref firearmData);

            if (firearmData is not GrabHandler grabHandler)
                firearmData = grabHandler = new(args.Player, args.Firearm);

            args.IsAllowed = false;

            if (grabHandler.AnyGrabbed)
                grabHandler.Release(false);

            if (args.TargetPlayer?.ReferenceHub != null)
            {
                grabHandler.GrabPlayer(args.TargetPlayer);

                args.Player.SendConsoleMessage($"[GRAB] Grabbed {args.TargetPlayer.ToLogString()} via TargetPlayer", "green");
            }
            else
            {
                foreach (var destructible in args.Hitscan.Destructibles)
                {
                    args.Player.SendConsoleMessage($"[GRAB] Found destructible of type {destructible.Destructible.GetType().Name} ({destructible.Hit.collider.name})", "yellow");

                    if (destructible.Destructible is HitboxIdentity hitboxIdentity 
                        && ExPlayer.TryGet(hitboxIdentity.TargetHub, out var player)
                        && player != args.Player)
                    {
                        grabHandler.GrabPlayer(player);

                        args.Player.SendConsoleMessage($"[GRAB] Grabbed {player.ToLogString()} via IDestructible", "green");
                        return;
                    }

                    if (destructible.Destructible is AdminToyBase adminToy)
                    {
                        grabHandler.GrabToy(adminToy);

                        args.Player.SendConsoleMessage($"[GRAB] Grabbed admin toy {adminToy.CommandName} via IDestructible", "green");
                        return;
                    }
                }

                foreach (var pair in args.Hitscan.Obstacles)
                {
                    args.Player.SendConsoleMessage($"[GRAB] Found obstacle collider {pair.Hit.collider.name}", "yellow");

                    if (pair.Hit.collider.gameObject.TryFindComponent<ItemPickupBase>(out var pickup))
                    {
                        grabHandler.GrabPickup(pickup);

                        args.Player.SendConsoleMessage($"[GRAB] Grabbed pickup {pickup.Info.ItemId} via collider", "green");
                        return;
                    }

                    if (pair.Hit.collider.gameObject.TryFindComponent<AdminToyBase>(out var toy))
                    {
                        grabHandler.GrabToy(toy);

                        args.Player.SendConsoleMessage($"[GRAB] Grabbed admin toy {toy.CommandName} via collider", "green");
                        return;
                    }
                }

                if (args.Player.TryCast(50f, 0f, PickupLayerMask, out var hit))
                {
                    if (hit.collider.gameObject.TryFindComponent<ItemPickupBase>(out var pickup))
                    {
                        grabHandler.GrabPickup(pickup);

                        args.Player.SendConsoleMessage($"[GRAB] Grabbed pickup {pickup.Info.ItemId} via additional cast", "green");
                        return;
                    }
                }

                args.Player.SendConsoleMessage("[GRAB] Could not grab any supported objects", "red");
            }
        }

        /// <inheritdoc/>
        public override void OnSelected(PlayerSelectedItemEventArgs args, ref object? itemData)
        {
            base.OnSelected(args, ref itemData);
            args.Player.Toggles.HasUnlimitedAmmo = true;
        }

        /// <inheritdoc/>
        public override void OnUnselected(PlayerSelectedItemEventArgs args, ref object? itemData)
        {
            base.OnUnselected(args, ref itemData);
            args.Player.Toggles.HasUnlimitedAmmo = false;
        }

        internal void UpdateScale(ItemBase item, Vector3 scale)
        {
            if (item == null)
                return;

            if (!CheckItem<GrabHandler>(item, out var grabHandler))
                return;

            grabHandler.UpdateScale(scale);
        }
    }
}