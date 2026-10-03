using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API;
using LabExtended.API.Custom.Items;

using UnityEngine;

namespace ObscurisCore.Utilities
{
    /// <summary>
    /// Provides extension methods for adding, throwing, and spawning both base and custom items or projectiles for
    /// players and at specified locations.
    /// </summary>
    /// <remarks>The methods in this class attempt to resolve an item or projectile by name, supporting both
    /// standard (base) types and custom implementations. They offer a unified approach for interacting with items and
    /// projectiles, abstracting the distinction between base and custom types. All methods return a Boolean indicating
    /// success and output the created or thrown object if successful. These methods do not throw exceptions for invalid
    /// item names; instead, they return <see langword="false"/> if the operation cannot be completed.</remarks>
    public static class ItemHelper
    {
        /// <summary>
        /// Attempts to add an item to the specified player's inventory by interpreting the provided value as either a
        /// base item type or a custom item identifier.
        /// </summary>
        /// <remarks>The method first attempts to parse the value as a base item type. If unsuccessful, it
        /// then checks for a matching custom item. If neither is found, no item is added and the method returns
        /// false.</remarks>
        /// <param name="target">The player whose inventory will receive the item.</param>
        /// <param name="value">The string representing either the name of a base item type or a custom item identifier. Must not be null or
        /// empty.</param>
        /// <param name="item">When this method returns, contains the added item if successful; otherwise, null.</param>
        /// <returns>true if an item was successfully added to the player's inventory; otherwise, false.</returns>
        public static bool TryAddCustomOrBaseItem(this ExPlayer target, string value, out ItemBase? item)
        {
            item = null;

            if (value?.Length < 1)
                return false;

            if (Enum.TryParse<ItemType>(value, true, out var itemType))
            {
                item = target.Inventory.AddItem(itemType);
                return item != null;
            }

            if (CustomItem.TryGet(value, out var customItem))
            {
                item = customItem.AddItem(target);
                return item != null;
            }

            return false;
        }

        /// <summary>
        /// Attempts to throw either a custom or base item from the specified player's inventory, based on the provided
        /// item identifier.
        /// </summary>
        /// <remarks>If the specified value matches a base item type, that item is thrown. If it matches a
        /// custom item, the custom item is thrown. If neither is found, no item is thrown and the method returns
        /// false.</remarks>
        /// <param name="target">The player from whose inventory the item will be thrown.</param>
        /// <param name="value">The identifier of the item to throw. This can be either the name of a base item type or a custom item name.
        /// Must not be null or empty.</param>
        /// <param name="scale">The scale to apply to the thrown item, represented as a <see cref="Vector3"/>.</param>
        /// <param name="force">The force with which the item will be thrown.</param>
        /// <param name="item">When this method returns, contains the thrown <see cref="ItemPickupBase"/> instance if successful;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>true if the item was successfully thrown; otherwise, false.</returns>
        public static bool TryThrowCustomOrBaseItem(this ExPlayer target, string value, Vector3 scale, float force, out ItemPickupBase? item)
        {
            item = null;

            if (value?.Length < 1)
                return false;

            if (Enum.TryParse<ItemType>(value, true, out var itemType))
            {
                item = target.Inventory.ThrowItem<ItemPickupBase>(itemType, force, scale);
                return item != null;
            }

            if (CustomItem.TryGet(value, out var customItem))
            {
                item = customItem.ThrowItem(customItem.AddItem(target), force);
                return item != null;
            }

            return false;
        }

        /// <summary>
        /// Attempts to throw a projectile for the specified player using either a custom projectile or a base item
        /// type, based on the provided identifier.
        /// </summary>
        /// <remarks>If the specified value matches a base item type, a corresponding projectile is
        /// spawned. If it matches a custom projectile, that projectile is thrown. If neither is found, no projectile is
        /// thrown and the method returns false.</remarks>
        /// <param name="target">The player for whom the projectile will be thrown.</param>
        /// <param name="value">The identifier of the projectile to throw. This can be the name of a base item type or a custom projectile.
        /// Must not be null or empty.</param>
        /// <param name="scale">The scale to apply to the projectile when spawned.</param>
        /// <param name="force">The force with which the projectile is thrown.</param>
        /// <param name="fuse">The fuse duration, in seconds, before the projectile activates or explodes.</param>
        /// <param name="projectile">When this method returns, contains the thrown projectile instance if the operation was successful;
        /// otherwise, null.</param>
        /// <returns>true if the projectile was successfully thrown; otherwise, false.</returns>
        public static bool TryThrowCustomOrBaseProjectile(this ExPlayer target, string value, Vector3 scale, float force, float fuse,
            out ThrownProjectile? projectile)
        {
            projectile = null;

            if (value?.Length < 1)
                return false;

            if (Enum.TryParse<ItemType>(value, true, out var itemType))
            {
                projectile = ExMap.SpawnProjectile(itemType, target.Position, scale, target.Velocity, target.Rotation, force, fuse);
                return projectile != null;
            }

            if (CustomItem.TryGet(value, out var customItem)
                && customItem is CustomProjectile customProjectile)
            {
                projectile = customProjectile.ThrowProjectile(target.Position, target.Velocity, force);
                return projectile != null;
            }

            return false;
        }

        /// <summary>
        /// Attempts to spawn an item pickup at the specified position, using either a custom item or a base item type,
        /// based on the provided identifier.
        /// </summary>
        /// <remarks>If the specified identifier matches a base item type, that item is spawned. If it
        /// matches a custom item, the custom item is spawned instead. If neither is found, no item is spawned and the
        /// method returns false.</remarks>
        /// <param name="value">The identifier of the item to spawn. This can be the name of a base item type or a custom item. Must not be
        /// null or empty.</param>
        /// <param name="position">The world position where the item pickup will be spawned.</param>
        /// <param name="scale">The scale to apply to the spawned item pickup.</param>
        /// <param name="rotation">The rotation to apply to the spawned item pickup.</param>
        /// <param name="pickup">When this method returns, contains the spawned item pickup if successful; otherwise, null.</param>
        /// <returns>true if the item pickup was successfully spawned; otherwise, false.</returns>
        public static bool TrySpawnCustomOrBaseItem(string value, Vector3 position, Vector3 scale, Quaternion rotation, out ItemPickupBase? pickup)
        {
            pickup = null;

            if (value?.Length < 1)
                return false;

            if (Enum.TryParse<ItemType>(value, true, out var itemType))
            {
                pickup = ExMap.SpawnItem(itemType, position, scale, rotation);
                return pickup != null;
            }

            if (CustomItem.TryGet(value, out var customItem))
            {
                pickup = customItem.SpawnItem(position, rotation);
                return pickup != null;
            }

            return false;
        }

        /// <summary>
        /// Attempts to spawn a projectile at the specified position using either a custom projectile definition or a
        /// base item type. Returns a value indicating whether the projectile was successfully spawned.
        /// </summary>
        /// <remarks>If the specified value matches a base item type, a standard projectile is spawned. If
        /// it matches a custom projectile, the custom definition is used. If neither is found, no projectile is spawned
        /// and the method returns false.</remarks>
        /// <param name="value">The identifier of the projectile to spawn. This can be the name of a base item type or a custom projectile.
        /// Must not be null or empty.</param>
        /// <param name="position">The world position at which to spawn the projectile.</param>
        /// <param name="scale">The scale to apply to the spawned projectile.</param>
        /// <param name="velocity">The initial velocity to assign to the projectile.</param>
        /// <param name="rotation">The rotation to apply to the projectile upon spawning.</param>
        /// <param name="force">The force to apply when spawning the projectile.</param>
        /// <param name="projectile">When this method returns, contains the spawned projectile if successful; otherwise, <see langword="null"/>.</param>
        /// <returns>true if the projectile was successfully spawned; otherwise, false.</returns>
        public static bool TrySpawnCustomOrBaseProjectile(string value, Vector3 position, Vector3 scale, Vector3 velocity, Quaternion rotation, float force,
            out ThrownProjectile? projectile)
        {
            projectile = null;

            if (value?.Length < 1)
                return false;

            if (Enum.TryParse<ItemType>(value, true, out var itemType))
            {
                projectile = ExMap.SpawnProjectile(itemType, position, scale, velocity, rotation, force);
                return projectile != null;
            }

            if (CustomItem.TryGet(value, out var customItem)
                && customItem is CustomProjectile customProjectile)
            {
                projectile = customProjectile.ThrowProjectile(position, velocity, force);
                return projectile != null;
            }

            return false;
        }
    }
}