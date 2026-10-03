using CustomPlayerEffects;

using InventorySystem.Items;

using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.API.Custom.Items;

using LabExtended.API.Settings;
using LabExtended.API.Settings.Menus;

using LabExtended.Extensions;

using MapGeneration;

using MEC;

using ObscurisCore.Features.Elements.Alerts;

using PlayerRoles;

using PlayerStatsSystem;

using UnityEngine;

using Utils;

namespace ObscurisCore.Extensions
{
    /// <summary>
    /// Provides extension methods for the ExPlayer type.
    /// </summary>
    public static class PlayerExtensions
    {
        /// <summary>
        /// Provides an array containing all values defined in the <see cref="Team"/> enumeration.
        /// </summary>
        /// <remarks>The array is ordered according to the underlying values of the <see cref="Team"/>
        /// enum. This member is useful for iterating over all possible teams or performing bulk operations.</remarks>
        public static Team[] AllTeams = EnumUtils<Team>.Values.ToArray();

        /// <summary>
        /// Provides an array containing all defined facility zones except for None and Other.
        /// </summary>
        /// <remarks>This array can be used to iterate over all standard facility zones, excluding special
        /// values such as None and Other that may represent undefined or miscellaneous cases.</remarks>
        public static FacilityZone[] AllZones = EnumUtils<FacilityZone>.Values
            .Where(z => z != FacilityZone.None && z != FacilityZone.Other)
            .ToArray();
        
        /// <summary>
        /// Calculates the player's health amount based on a given percentage of their maximum health.
        /// </summary>
        /// <remarks>This method evaluates the specified percentage of the player's maximum health. If the player is invalid or not alive, the method will return 0.</remarks>
        /// <param name="player">The player whose health amount is being calculated. Must be a valid and alive instance of <see cref="ExPlayer"/>.</param>
        /// <param name="percentage">The percentage of the maximum health to calculate. Should be an integer value between 0 and 100.</param>
        /// <returns>The health amount as an integer value derived from the specified percentage of the player's maximum health. Returns 0 if the player is invalid or not alive.</returns>
        public static int GetHealthAmount(this ExPlayer player, int percentage)
        {
            if (!player.IsValidPlayer())
                return 0;

            if (!player.IsAlive)
                return 0;
            
            return Mathf.CeilToInt(player.MaxHealth * percentage / 100);
        }
        
        /// <summary>
        /// Calculates the player's current health as a percentage of their maximum health.
        /// </summary>
        /// <remarks>This method provides a way to determine the player's health percentage. It returns 0 if the player is invalid or not alive.</remarks>
        /// <param name="player">The player whose health percentage is being calculated. Must be a valid and alive instance of <see cref="ExPlayer"/>.</param>
        /// <returns>The player's health percentage as an integer value. Returns 0 if the player is invalid or dead.</returns>
        public static int GetHealthPercent(this ExPlayer player)
        {
            if (!player.IsValidPlayer())
                return 0;

            if (!player.IsAlive)
                return 0;

            return Mathf.CeilToInt(player.Health / player.MaxHealth * 100);
        }

        /// <summary>
        /// Attempts to cast the specified player to an ExPlayer instance.
        /// </summary>
        /// <remarks>Use this method to safely attempt casting a Player to ExPlayer without throwing an
        /// exception. The method returns false if the player is null, does not have a ReferenceHub, or is not an
        /// ExPlayer.</remarks>
        /// <param name="player">The player to cast. Must not be null and must have a valid ReferenceHub.</param>
        /// <param name="castPlayer">When this method returns, contains the casted ExPlayer instance if the cast is successful; otherwise, null.</param>
        /// <returns>true if the player is successfully cast to ExPlayer; otherwise, false.</returns>
        public static bool CastPlayer(this Player player, out ExPlayer castPlayer)
        {
            if (player?.ReferenceHub == null
                || player is not ExPlayer cast)
            {
                castPlayer = null!;
                return false;
            }

            castPlayer = cast;
            return true;
        }

        /// <summary>
        /// Determines whether the specified player instance represents a valid, connected player.
        /// </summary>
        /// <param name="player">The player instance to validate. Can be null.</param>
        /// <returns>true if the player is not null, has a non-null ReferenceHub, and a non-empty UserId; otherwise, false.</returns>
        public static bool IsValidPlayer(this ExPlayer player)
            => player?.ReferenceHub != null && !string.IsNullOrEmpty(player.UserId);

        /// <summary>
        /// Retrieves an existing menu of type T associated with the player, or creates and adds a new one using the
        /// specified factory if none exists.
        /// </summary>
        /// <remarks>This method ensures that only one instance of the specified menu type is associated
        /// with the player. If a menu of type T already exists, it is returned; otherwise, the factory is invoked to
        /// create and add a new menu.</remarks>
        /// <typeparam name="T">The type of the settings menu to retrieve or create. Must inherit from SettingsMenu.</typeparam>
        /// <param name="player">The player instance for which the menu is retrieved or added.</param>
        /// <param name="menuFactory">A factory function used to create a new menu of type T if one does not already exist for the player.</param>
        /// <returns>The existing menu of type T if found; otherwise, a newly created and added menu of type T.</returns>
        public static T GetOrAddMenu<T>(this ExPlayer player, Func<T> menuFactory) where T : SettingsMenu
        {
            if (player.TryGetMenu<T>(out var menu))
                return menu;

            menu = menuFactory();

            player.AddMenu(menu);
            return menu;
        }

        /// <summary>
        /// Determines whether the player has an item of the specified custom type in their inventory.
        /// </summary>
        /// <remarks>If multiple items of type T exist in the inventory, only the first one found is
        /// returned in customItem. If the player or their reference hub is null, the method returns false and
        /// customItem is set to its default value.</remarks>
        /// <typeparam name="T">The type of custom item to search for. Must inherit from CustomItem.</typeparam>
        /// <param name="player">The player whose inventory is searched for the custom item.</param>
        /// <param name="customItem">When this method returns, contains the found custom item of type T if one exists; otherwise, the default
        /// value for T.</param>
        /// <returns>true if the player has a custom item of type T in their inventory; otherwise, false.</returns>
        public static bool HasCustomItem<T>(this ExPlayer player, out T customItem) where T : CustomItem
        {
            customItem = null!;

            if (player?.ReferenceHub == null)
                return false;

            foreach (var item in player.Inventory.Items)
            {
                if (CustomItem.IsCustomItem(item.ItemSerial, out customItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the player has an item of the specified custom type in their inventory.
        /// </summary>
        /// <remarks>If multiple items of type T exist in the inventory, only the first one found is
        /// returned in customItem. If the player or their reference hub is null, the method returns false and
        /// customItem is set to its default value.</remarks>
        /// <typeparam name="T">The type of custom item to search for. Must inherit from CustomItem.</typeparam>
        /// <param name="player">The player whose inventory is searched for the custom item.</param>
        /// <param name="targetItem">When this method returns, contains the found inventory item of type T if one exists; otherwise, the default
        /// value for ItemBase.</param>
        /// <param name="customItem">When this method returns, contains the found custom item of type T if one exists; otherwise, the default
        /// value for T.</param>
        /// <returns>true if the player has a custom item of type T in their inventory; otherwise, false.</returns>
        public static bool HasCustomItem<T>(this ExPlayer player, out ItemBase targetItem, out T customItem) where T : CustomItem
        {
            targetItem = null!;
            customItem = null!;

            if (player?.ReferenceHub == null)
                return false;

            foreach (var item in player.Inventory.Items)
            {
                if (CustomItem.IsCustomItem(item.ItemSerial, out customItem))
                {
                    targetItem = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a formatted alert to the specified player using the provided message, with options for customization.
        /// </summary>
        /// <param name="player">The player to whom the alert will be sent. Must be a valid instance of <see cref="ExPlayer"/>.</param>
        /// <param name="str">The message to display in the alert. Must not be null or empty.</param>
        /// <param name="overrideCurrent">Determines whether the current active alert, if any, should be replaced by the new alert.</param>
        /// <param name="defaultType">The type of the alert, which can be informational or a warning. Defaults to <see cref="AlertType.Info"/>.</param>
        /// <param name="defaultDuration">The duration, in seconds, that the alert should stay active. Defaults to 5 seconds.</param>
        /// <param name="defaultTitle">The title of the alert. Can be null, in which case no title will be displayed.</param>
        public static void SendFormattedAlert<T>(this ExPlayer player, T key, IDictionary<T, string> messages,
            bool overrideCurrent,
            AlertType defaultType = AlertType.Info,
            float defaultDuration = 5f, string? defaultTitle = null)
        {
            if (!player.IsValidPlayer())
                return;

            if (messages?.Count < 1)
                return;

            if (!messages.TryGetValue(key, out var msg))
                return;

            if (string.IsNullOrEmpty(msg))
                return;

            player.SendFormattedAlert(msg, overrideCurrent, defaultType, defaultDuration, defaultTitle);
        }

        /// <summary>
        /// Sends a formatted alert to the specified player, using the provided string to determine
        /// the alert's type, duration, title, and message based on its format.
        /// </summary>
        /// <remarks>
        /// This method parses the input string to dynamically determine the alert's parameters.
        /// The input format can be one of:
        /// - "Time:Message" (e.g., "5:Hello" sets a 5-second alert with a message),
        /// - "Type:Time:Message" (e.g., "Warn:10:Warning message"),
        /// - "Type:Time:Title:Message" (e.g., "Info:8:Custom Title:Custom message").
        /// If parsing the string fails, the default values will be used.
        /// </remarks>
        /// <param name="player">The player to send the alert to. Must be a valid <see cref="ExPlayer"/>.</param>
        /// <param name="str">
        /// The formatted string containing alert parameters. Can include combinations of alert type,
        /// duration, title, and message as described. Cannot be null or empty.
        /// </param>
        /// <param name="overrideCurrent">
        /// A flag indicating whether to override the currently displayed alert on the player.
        /// Pass true to replace the current alert.
        /// </param>
        /// <param name="defaultType">
        /// The default alert type to use if the input string does not specify a type.
        /// Default is <see cref="AlertType.Info"/>.
        /// </param>
        /// <param name="defaultDuration">
        /// The default duration, in seconds, for the alert if the input string does not specify a time.
        /// Default is 5 seconds.
        /// </param>
        /// <param name="defaultTitle">
        /// The default title of the alert if the input string does not specify one.
        /// If null, no title will be displayed.
        /// </param>
        public static void SendFormattedAlert(this ExPlayer player, string str, bool overrideCurrent,
            AlertType defaultType = AlertType.Info,
            float defaultDuration = 5f, string? defaultTitle = null)
        {
            if (string.IsNullOrEmpty(str))
                return;

            if (!player.IsValidPlayer())
                return;

            var parts = str.Split(':');

            if (parts.Length == 2
                && float.TryParse(parts[0], out var duration)) // Time:Message
            {
                player.SendAlert(defaultType, duration, defaultTitle, parts[1], overrideCurrent);
            }
            else if (parts.Length == 3
                     && float.TryParse(parts[0], out duration)
                     && Enum.TryParse(parts[1], true, out AlertType type)) // Type:Time:Message
            {
                player.SendAlert(type, duration, defaultTitle, parts[2], overrideCurrent);
            }
            else if (parts.Length == 4
                     && float.TryParse(parts[0], out duration)
                     && Enum.TryParse(parts[1], true, out type)) // Type:Time:Title:Message
            {
                player.SendAlert(type, duration, parts[2], parts[3], overrideCurrent);
            }
            else
            {
                player.SendAlert(defaultType, defaultDuration, defaultTitle, str, overrideCurrent);
            }
        }

        /// <summary>
        /// Swaps the entire inventory, ammunition, and currently held items between two players.
        /// </summary>
        /// <param name="player">The player initiating the inventory switch. Must be an instance of <see cref="ExPlayer"/>.</param>
        /// <param name="target">The target player whose inventory will be swapped with the player's inventory. Must be an instance of <see cref="ExPlayer"/>.</param>
        public static void SwitchFullInventory(this ExPlayer player, ExPlayer target)
        {
            var otherCur = player.Inventory.CurrentItem;
            var otherItems = target.Inventory.Items.ToList();
            var otherAmmo = target.Ammo.Ammo.ToDictionary();
            var otherCustomAmmo = target.Ammo.CustomAmmo.ToDictionary();

            var playerCur = target.Inventory.CurrentItem;
            var playerItems = player.Inventory.Items.ToList();
            var playerAmmo = player.Ammo.Ammo.ToDictionary();
            var playerCustomAmmo = player.Ammo.CustomAmmo.ToDictionary();
            
            player.Inventory.CurrentItem = null!;
            
            player.Inventory.UserInventory.Items.Clear();
            player.Inventory.UserInventory.ReserveAmmo.Clear();

            player.Ammo.CustomAmmo.Clear();
            
            player.ReferenceHub.inventory.ServerSendAmmo();
            player.ReferenceHub.inventory.ServerSendItems();
            
            target.Ammo.CustomAmmo.Clear();
            
            target.Inventory.UserInventory.Items.Clear();
            target.Inventory.UserInventory.ReserveAmmo.Clear();
            
            target.ReferenceHub.inventory.ServerSendAmmo();
            target.ReferenceHub.inventory.ServerSendItems();
        
            otherItems.ForEach(it => it.TransferItem(player.ReferenceHub));
            otherAmmo.ForEach(kv => player.Inventory.UserInventory.ReserveAmmo[kv.Key] = kv.Value);
            otherCustomAmmo.ForEach(kv => player.Ammo.SetCustomAmmo(kv.Key, kv.Value));
        
            playerItems.ForEach(it => it.TransferItem(target.ReferenceHub));
            playerAmmo.ForEach(kv => target.Inventory.UserInventory.ReserveAmmo[kv.Key] = kv.Value);
            playerCustomAmmo.ForEach(kv => target.Ammo.SetCustomAmmo(kv.Key, kv.Value));

            Timing.CallDelayed(0.1f, () =>
            {
                player.ReferenceHub.inventory.ServerSendAmmo();
                player.ReferenceHub.inventory.ServerSendItems();
                
                target.ReferenceHub.inventory.ServerSendAmmo();
                target.ReferenceHub.inventory.ServerSendItems();
                
                target.Inventory.CurrentItem = otherCur;
                player.Inventory.CurrentItem = playerCur;
            });
        }

        /// <summary>
        /// Switches the currently held item between the specified player and target player.
        /// </summary>
        /// <param name="player">The player whose currently held item is to be switched.</param>
        /// <param name="target">The target player whose currently held item is to be exchanged with the player's item.</param>
        public static void SwitchHeldItem(this ExPlayer player, ExPlayer target)
        {
            var playerItem = player.Inventory.CurrentItem;
            var targetItem = target.Inventory.CurrentItem;

            player.Inventory.CurrentItem = null!;
            
            if (playerItem != null)
                playerItem.TransferItem(target.ReferenceHub);
            
            player.ReferenceHub.inventory.ServerSendItems();
            
            target.Inventory.CurrentItem = null!;
            
            if (targetItem != null)
                targetItem.TransferItem(player.ReferenceHub);
            
            target.ReferenceHub.inventory.ServerSendItems();
            
            player.Inventory.CurrentItem = targetItem!;
            target.Inventory.CurrentItem = playerItem!;
        }

        /// <summary>
        /// Resets the movement-related effects on the player to their default state.
        /// </summary>
        /// <remarks>
        /// This method disables any active movement-altering effects, such as slowness or movement speed boosts,
        /// on the specified player. It ensures the player's movement speed is restored to normal.
        /// </remarks>
        /// <param name="player">The player whose movement effects are to be reset. Must not be null and must have a valid ReferenceHub.</param>
        public static void ResetSpeed(this ExPlayer player)
        {
            if (player?.ReferenceHub != null)
            {
                player.Effects.Slowness.ServerDisable();
                player.Effects.MovementBoost.ServerDisable();
            }
        }

        /// <summary>
        /// Modifies the player's movement speed based on the specified multiplier.
        /// </summary>
        /// <remarks>
        /// This method adjusts the player's movement speed by applying a multiplier. A value greater than 1 increases speed,
        /// while a value less than 1 reduces speed. The method safely handles the application's effects for increasing or
        /// decreasing movement speed.
        /// </remarks>
        /// <param name="player">The player whose speed will be modified. Must not be null and must have a valid ReferenceHub.</param>
        /// <param name="multiplier">The multiplier to apply to the player's speed. Values greater than 1 increase speed, and values less than 1 decrease speed.</param>
        public static void ChangeSpeedByMultiplier(this ExPlayer player, float multiplier)
        {
            if (player?.ReferenceHub != null)
            {
                if (multiplier is > 1f or < 1f)
                {
                    if (multiplier < 1f)
                    {
                        player.Effects.Slowness.ServerSetState((byte)Mathf.Min(byte.MaxValue, Mathf.CeilToInt((multiplier + 1f) * 10f)));
                    }
                    else
                    {
                        player.Effects.MovementBoost.ServerSetState((byte)(Mathf.Min(byte.MaxValue, Mathf.CeilToInt(multiplier * 10f))));
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified player is currently located in a room with the given name, and optionally
        /// matches the specified zone and shape.
        /// </summary>
        /// <param name="player">The player to check for room membership. Must not be null.</param>
        /// <param name="name">The name of the room to check against the player's current location.</param>
        /// <param name="zone">An optional facility zone to further restrict the room match. If specified, the player's current room must
        /// be in this zone.</param>
        /// <param name="shape">An optional room shape to further restrict the room match. If specified, the player's current room must have
        /// this shape.</param>
        /// <returns>true if the player is alive and currently in a room with the specified name, and optionally matches the
        /// specified zone and shape; otherwise, false.</returns>
        public static bool IsInRoom(this ExPlayer player, RoomName name, FacilityZone? zone = null, RoomShape? shape = null)
            => player?.ReferenceHub != null
            && player.Role.IsAlive
            && player.Position.Room != null
            && player.Position.Room.Name == name
            && (!zone.HasValue || player.Position.Room.Zone == zone.Value)
            && (!shape.HasValue || player.Position.Room.Shape == shape.Value);

        /// <summary>
        /// Determines whether the specified position is located within a room that matches the given name, and
        /// optionally, the specified zone and shape.
        /// </summary>
        /// <param name="position">The world position to evaluate for room membership.</param>
        /// <param name="name">The name of the room to check against the position.</param>
        /// <param name="zone">An optional facility zone to further restrict the room match. If specified, the room must be in this zone.</param>
        /// <param name="shape">An optional room shape to further restrict the room match. If specified, the room must have this shape.</param>
        /// <returns>true if the position is within a room that matches the specified name and, if provided, the specified zone
        /// and shape; otherwise, false.</returns>
        public static bool IsInRoom(this Vector3 position, RoomName name, FacilityZone? zone = null, RoomShape? shape = null)
        {
            if (!position.TryGetRoom(out var room))
                return false;

            if (room.Name != name)
                return false;

            if (zone.HasValue && room.Zone != zone.Value)
                return false;

            if (shape.HasValue && room.Shape != shape.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Attempts to teleport the player to a random spawn position associated with a randomly selected team and
        /// role, optionally excluding specified teams and roles.
        /// </summary>
        /// <remarks>The method will not teleport the player if no valid teams or roles are available
        /// after applying exclusions, or if the player is not alive. The player's position is updated only on
        /// successful teleportation.</remarks>
        /// <param name="player">The player to teleport. Must not be null and must be alive.</param>
        /// <param name="teams">An array of teams to consider for random selection. If null, all available teams are used.</param>
        /// <param name="excludedTeams">An array of teams to exclude from random selection. If null or empty, no teams are excluded.</param>
        /// <param name="excludedRoles">An array of role types to exclude from random selection within the chosen team. If null or empty, no roles
        /// are excluded.</param>
        /// <returns>true if the player was successfully teleported to a random spawn position; otherwise, false.</returns>
        public static bool RandomSpawnPositionTeleport(this ExPlayer player, Team[]? teams = null, Team[]? excludedTeams = null,
            RoleTypeId[]? excludedRoles = null)
        {
            if (player?.ReferenceHub == null)
                return false;

            if (!player.IsAlive)
                return false;

            teams ??= AllTeams;

            if (excludedTeams?.Length > 0)
                teams = teams.Except(excludedTeams).ToArray();

            if (teams.Length == 0)
                return false;

            var randomRole = teams.GetRandomRole(excludedRoles ?? Array.Empty<RoleTypeId>());

            if (!randomRole.TryGetSpawnPosition(out var position, out _))
                return false;

            player.Position.Position = position;
            return true;
        }

        /// <summary>
        /// Attempts to teleport the player to a random room within the specified facility zones.
        /// </summary>
        /// <remarks>The method does not perform any action if the player is not alive or if required
        /// movement components are unavailable. If no valid destination is found within the specified zones, the
        /// teleportation does not occur.</remarks>
        /// <param name="player">The player to teleport. Must not be null and must be alive.</param>
        /// <param name="zones">An array of facility zones to select the destination room from. If null, all zones are considered.</param>
        /// <returns>true if the player was successfully teleported to a random room; otherwise, false.</returns>
        public static bool RandomRoomTeleport(this ExPlayer player, FacilityZone[]? zones)
        {
            if (player?.ReferenceHub == null)
                return false;

            if (!player.IsAlive)
                return false;

            if (player.Role.MovementModule == null || player.Role.MovementModule.CharController == null)
                return false;

            var position = ExMap.GetPocketExitPosition(player, Warhead.IsDetonated ? [FacilityZone.Surface] : zones ?? AllZones);

            if (position != Vector3.zero)
            {
                player.Position.Position = position;

                if (position.TryGetRoom(out var room)
                    && room.Name is RoomName.Pocket)
                    player.ReferenceHub.playerEffectsController.EnableEffect<PocketCorroding>();
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the effective integer weight for the specified player, applying optional multipliers to the base
        /// weight.
        /// </summary>
        /// <remarks>If no valid multipliers are found for the player, or if the player or multipliers are
        /// null, the method returns the base weight without modification.</remarks>
        /// <param name="player">The player for whom to calculate the weight. Must not be null and must have a valid ReferenceHub.</param>
        /// <param name="multipliers">A dictionary of multiplier values keyed by string identifiers. If null or empty, no multipliers are applied.</param>
        /// <param name="baseWeight">The base weight value to use as the starting point for calculation.</param>
        /// <param name="addMultipliers">If <see langword="true"/>, multipliers are added to the base weight; if <see langword="false"/>, the base
        /// weight is multiplied by the multiplier. The default is <see langword="true"/>.</param>
        /// <returns>The calculated integer weight for the player, adjusted by applicable multipliers. Returns the base weight if
        /// no valid multipliers are found or if the player is invalid.</returns>
        public static int GetInt32Weight(this ExPlayer player, IDictionary<string, int> multipliers, int baseWeight, bool addMultipliers = true)
        {
            if (player?.ReferenceHub == null)
                return baseWeight;

            if (multipliers == null || multipliers.Count == 0)
                return baseWeight;

            var multiplier = player.GetValidInt32Multipliers(multipliers, 0);

            if (multiplier > 0)
            {
                if (addMultipliers)
                    return baseWeight + multiplier;
                
                return baseWeight * multiplier;
            }

            return baseWeight;
        }

        /// <summary>
        /// Calculates the effective weight for the specified player by applying the provided multipliers to the base
        /// weight.
        /// </summary>
        /// <remarks>If no valid multipliers are found or the player is not valid, the method returns the
        /// base weight without modification.</remarks>
        /// <param name="player">The player for whom the weight is being calculated. Cannot be null.</param>
        /// <param name="multipliers">A dictionary containing multiplier names and their corresponding float values to be applied. If null or
        /// empty, no multipliers are applied.</param>
        /// <param name="baseWeight">The base weight value before any multipliers are applied.</param>
        /// <param name="addMultipliers">If <see langword="true"/>, the sum of valid multipliers is added to the base weight; if <see
        /// langword="false"/>, the base weight is multiplied by the sum of valid multipliers. The default is <see
        /// langword="true"/>.</param>
        /// <returns>The calculated weight after applying the specified multipliers to the base weight. Returns the base weight
        /// if the player or multipliers are not valid.</returns>
        public static float GetFloatWeight(this ExPlayer player, IDictionary<string, float> multipliers, float baseWeight, bool addMultipliers = true)
        {
            if (player?.ReferenceHub == null)
                return baseWeight;

            if (multipliers == null || multipliers.Count == 0)
                return baseWeight;

            var multiplier = player.GetValidFloatMultipliers(multipliers, 0f);

            if (multiplier > 0f)
            {
                if (addMultipliers)
                    return baseWeight + multiplier;

                return baseWeight * multiplier;
            }

            return baseWeight;
        }

        /// <summary>
        /// Calculates the total multiplier value for the specified player based on user ID, permission group, and
        /// level, using the provided multipliers dictionary.
        /// </summary>
        /// <remarks>This method checks for multipliers in the following order: user ID, permission group
        /// name, and each level below the player's current level. If multiple multipliers apply, their values are
        /// summed. If the player does not have a valid reference hub or if the multipliers dictionary is null or empty,
        /// the method returns the default value.</remarks>
        /// <param name="player">The player for whom to calculate the multiplier. Cannot be null.</param>
        /// <param name="multipliers">A dictionary containing multiplier values keyed by user ID, permission group name, or level string. Cannot
        /// be null or empty.</param>
        /// <param name="defaultValue">The value to return if the player or multipliers are not valid, or if no applicable multipliers are found.
        /// The default is 0.</param>
        /// <returns>The sum of all applicable multipliers for the player. Returns the specified default value if the player or
        /// multipliers are invalid, or if no multipliers apply.</returns>
        public static int GetValidInt32Multipliers(this ExPlayer player, IDictionary<string, int> multipliers, int defaultValue = 0)
        {
            if (player?.ReferenceHub == null)
                return defaultValue;
            
            if (multipliers == null || multipliers.Count == 0)
                return defaultValue;

            var multiplier = defaultValue;

            if (multipliers.TryGetValue(player.UserId, out var userIdMultiplier))
                multiplier += userIdMultiplier;

            if (!string.IsNullOrEmpty(player.PermissionsGroupName) 
                && multipliers.TryGetValue(player.PermissionsGroupName, out var groupMultiplier))
                multiplier += groupMultiplier;

            return multiplier;
        }

        /// <summary>
        /// Calculates the total float multiplier for the specified player based on user ID, permission group, and
        /// level, using the provided multipliers dictionary.
        /// </summary>
        /// <remarks>This method checks for multipliers in the following order: user ID, permission group
        /// name, and each level up to the player's current level. All applicable multipliers are added together. If no
        /// matching multipliers are found, the default value is returned.</remarks>
        /// <param name="player">The player for whom to calculate the multiplier. Cannot be null.</param>
        /// <param name="multipliers">A dictionary containing multiplier values keyed by user ID, permission group name, or level string. Cannot
        /// be null or empty.</param>
        /// <param name="defaultValue">The default multiplier value to use if no applicable multipliers are found or if the player is invalid.</param>
        /// <returns>The sum of all applicable multipliers for the player. Returns the specified default value if the player is
        /// invalid or if the multipliers dictionary is null or empty.</returns>
        public static float GetValidFloatMultipliers(this ExPlayer player, IDictionary<string, float> multipliers, float defaultValue = 0f)
        {
            if (player?.ReferenceHub == null)
                return defaultValue;

            if (multipliers == null || multipliers.Count == 0)
                return defaultValue;

            var multiplier = defaultValue;

            if (multipliers.TryGetValue(player.UserId, out var userIdMultiplier))
                multiplier += userIdMultiplier;

            if (!string.IsNullOrEmpty(player.PermissionsGroupName) 
                && multipliers.TryGetValue(player.PermissionsGroupName, out var groupMultiplier))
                multiplier += groupMultiplier;

            return multiplier;
        }

        /// <summary>
        /// Calculates a modified velocity for the player by applying the specified multiplier to each component of the vector.
        /// </summary>
        /// <param name="player">The player whose velocity is to be modified.</param>
        /// <param name="multiplier">The multiplier to apply to each component of the player's velocity.</param>
        /// <returns>The modified velocity as a vector after applying the multiplier.</returns>
        public static Vector3 MultipliedVelocity(this ExPlayer player, float multiplier)
        {
            var velocity = player.Velocity;

            if (velocity.x > 0f) velocity.x *= multiplier;
            if (velocity.y > 0f) velocity.y *= multiplier;
            if (velocity.z > 0f) velocity.z *= multiplier;

            if (velocity.x < 0f) velocity.x *= -multiplier;
            if (velocity.y < 0f) velocity.y *= -multiplier;
            if (velocity.z < 0f) velocity.z *= -multiplier;

            return velocity;
        }
        
        /// <summary>
        /// Adjusts the Y-coordinate of the player's current position by the specified value.
        /// </summary>
        /// <param name="player">The player whose position is to be adjusted.</param>
        /// <param name="adjustment">The value by which to adjust the Y-coordinate.</param>
        /// <returns>The updated position with the adjusted Y-coordinate.</returns>
        public static Vector3 PositionAdjustY(this ExPlayer player, float adjustment)
        {
            var position = player.Position.Position;

            if (adjustment != 0f)
                position.y += adjustment;

            return position;
        }

        /// <summary>
        /// Triggers an explosion effect for the specified player, with options for spawning grenades,
        /// applying visual effects, and causing player death.
        /// </summary>
        /// <param name="player">The player entity to target with the explosion effect.</param>
        /// <param name="amount">The number of explosions to trigger.</param>
        /// <param name="grenadeType">The type of grenade to simulate in the explosion.</param>
        /// <param name="deathReason">The reason assigned for the player's death, if applicable.</param>
        /// <param name="effectOnly">Specifies whether only the visual effect should be triggered
        /// without causing damage to entities.</param>
        /// <param name="killPlayer">Indicates whether the targeted player should be killed by the
        /// explosion.</param>
        /// <param name="velocityMultiplier">A multiplier for the velocity applied to the player's ragdoll
        /// upon explosion.</param>
        /// <returns>Returns true if the explosion was successfully applied, false otherwise.</returns>
        public static bool Explode(this ExPlayer player, int amount, ItemType grenadeType, string? deathReason,
            bool effectOnly = false, bool killPlayer = true, float velocityMultiplier = 1f)
        {
            if (player?.ReferenceHub == null)
                return false;

            if (amount < 1)
                return false;

            if (!player.Role.IsAlive)
                return false;

            deathReason ??= "No death reason provided.";

            if (effectOnly)
            {
                for (var x = 0; x < amount; x++)
                {
                    ExplosionUtils.ServerSpawnEffect(player.Position, grenadeType);
                }
            }
            else
            {
                var explosionType = ExplosionType.Grenade;

                switch (grenadeType)
                {
                    case ItemType.ParticleDisruptor:
                        explosionType = ExplosionType.Disruptor;
                        break;

                    case ItemType.SCP018:
                        explosionType = ExplosionType.SCP018;
                        break;

                    case ItemType.SCP207:
                        explosionType = ExplosionType.Cola;
                        break;

                    case ItemType.SCP330:
                        explosionType = ExplosionType.PinkCandy;
                        break;

                    case ItemType.Jailbird:
                        explosionType = ExplosionType.Jailbird;
                        break;
                }

                for (var x = 0; x < amount; x++)
                {
                    ExplosionUtils.ServerExplode(player.Position, player.Footprint, explosionType);
                }
            }

            if (killPlayer)
            {
                if (player.IsGodModeEnabled) 
                    player.IsGodModeEnabled = false;

                var velocity = player.Rotation.Rotation * (Vector3.back * velocityMultiplier);

                velocity.y = 1f;
                velocity.Normalize();

                velocity *= (5f + velocityMultiplier);
                velocity.y += 2f;

                var damageHandler = new CustomReasonDamageHandler(deathReason, -1f);

                damageHandler.ApplyDamage(player.ReferenceHub);
                damageHandler.StartVelocity = velocity;

                player.ReferenceHub.playerStats.KillPlayer(damageHandler);
            }

            return true;
        }
    }
}