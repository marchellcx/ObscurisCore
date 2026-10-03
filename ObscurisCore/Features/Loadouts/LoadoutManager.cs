using InventorySystem.Items;

using LabExtended.API;
using LabExtended.API.Custom.Items;

using LabExtended.Core;
using LabExtended.Extensions;

using NiveraAPI.ScpSl;
using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Loadouts.Items;
using LabExtended.Utilities;

namespace ObscurisCore.Features.Loadouts;

/// <summary>
/// Manages loadouts for players, allowing for the configuration and application of predefined sets of items and ammo.
/// </summary>
public static class LoadoutManager
{
    /// <summary>
    /// The list of all configured loadouts.
    /// </summary>
    [Config("loadouts", "loadouts", "The list of loadouts")]
    public static List<LoadoutDefinition> Loadouts = new()
    {
        new()
        {
            Name = "Default",

            MaxHealth = 100f,
            StartHealth = 100f,

            Items = new List<LoadoutItem>()
            {
                new()
                {
                    BaseType = ItemType.GunCOM15
                },
                new()
                {
                    BaseType = ItemType.Medkit
                }
            },

            Ammo = new List<LoadoutAmmo>()
            {
                new()
                {
                    BaseType = ItemType.Ammo9x19,
                    Amount = 60
                }
            }
        }
    };

    /// <summary>
    /// Gets called once a loadout is applied.
    /// </summary>
    public static event Action<ExPlayer, LoadoutDefinition>? AppliedLoadout;

    /// <summary>
    /// Gets called once a player receives a vanilla item from a loadout.
    /// </summary>
    public static event Action<ExPlayer, LoadoutDefinition, LoadoutItem, ItemBase>? AddedVanillaItem;

    /// <summary>
    /// Gets called once a player receives a custom item from a loadout.
    /// </summary>
    public static event Action<ExPlayer, LoadoutDefinition, LoadoutItem, CustomItem, ItemBase>? AddedCustomItem;

    /// <summary>
    /// Ensures that a loadout of a specific name exists.
    /// </summary>
    /// <param name="loadoutName">The name of the loadout.</param>
    /// <param name="definition">The config definition of the loadout.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Ensure(string loadoutName, LoadoutDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(loadoutName))
            throw new ArgumentNullException(nameof(loadoutName));

        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        if (TryGet(loadoutName, out _))
            return;

        definition.Name = loadoutName;

        Loadouts.Add(definition);

        Loader.SaveConfig();
    }

    /// <summary>
    /// Ensures that a loadout of a specific name exists.
    /// </summary>
    /// <param name="definition">The config definition of the loadout.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Ensure(LoadoutDefinition definition)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        if (TryGet(definition.Name, out _))
            return;

        Loadouts.Add(definition);

        Loader.SaveConfig();
    }

    /// <summary>
    /// Gets a loadout.
    /// </summary>
    /// <param name="loadoutName">The name of the loadout.</param>
    /// <returns>The found loadout.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public static LoadoutDefinition Get(string loadoutName)
    {
        if (string.IsNullOrWhiteSpace(loadoutName))
            throw new ArgumentNullException(nameof(loadoutName));

        if (!Loadouts.TryGetFirst(x => x.Name == loadoutName, out var loadout))
            throw new KeyNotFoundException($"Could not find a loadout named '{loadoutName}'");

        return loadout;
    }

    /// <summary>
    /// Attempts to find a loadout by it's name.
    /// </summary>
    /// <param name="loadoutName">The name of the loadout.</param>
    /// <param name="definition">The found loadout.</param>
    /// <returns>true if the loadout was found</returns>
    public static bool TryGet(string loadoutName, out LoadoutDefinition definition)
    {
        definition = null!;

        if (string.IsNullOrWhiteSpace(loadoutName))
            return false;

        return Loadouts.TryGetFirst(x => x.Name == loadoutName, out definition);
    }

    /// <summary>
    /// Attempts to find a loadout whose name starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="definition">The found loadout.</param>
    /// <returns>true if a matching loadout was found; otherwise, false.</returns>
    public static bool TryGetPrefixed(string prefix, out LoadoutDefinition definition)
    {
        definition = null!;

        if (string.IsNullOrWhiteSpace(prefix))
            return false;

        return Loadouts.TryGetFirst(x => x.Name.StartsWith(prefix), out definition);
    }

    /// <summary>
    /// Attempts to pick a loadout whose name starts with the specified prefix, using the supplied weight picker.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="weightPicker">Returns the selection weight for each matching loadout.</param>
    /// <param name="definition">The selected loadout.</param>
    /// <returns><see langword="true"/> if a matching loadout was selected; otherwise, <see langword="false"/>.</returns>
    public static bool TryPickPrefixed(string prefix, Func<LoadoutDefinition, float> weightPicker, out LoadoutDefinition definition)
    {
        definition = null!;

        if (string.IsNullOrWhiteSpace(prefix))
            return false;

        var candidates = Loadouts.Where(x => x.Name.StartsWith(prefix)).ToList();

        if (candidates.Count == 0)
            return false;

        definition = candidates.GetRandomWeighted(weightPicker);
        return definition != null;
    }
    
    /// <summary>
    /// Attempts to apply a loadout.
    /// </summary>
    /// <param name="player">The player to apply the loadout to.</param>
    /// <param name="loadoutName">The name of the loadout.</param>
    /// <param name="itemProcessor">The delegate used to handle added vanilla items.</param>
    /// <returns>true if the loadout was applied; otherwise, false.</returns>
    public static bool TryApply(ExPlayer player, string loadoutName, Action<LoadoutItem, ItemBase>? itemProcessor = null)
    {
        if (player?.ReferenceHub == null)
            return false;

        if (!TryGet(loadoutName, out var loadout))
            return false;

        if (!player.Role.IsAlive)
            return false;

        if (loadout.MaxHealth.HasValue)
            player.MaxHealth = loadout.MaxHealth.Value;

        if (loadout.StartHealth.HasValue)
            player.Health = loadout.StartHealth.Value;
        else if (loadout.MaxHealth.HasValue)
            player.Health = loadout.MaxHealth.Value;

        for (var i = 0; i < loadout.Items.Count; i++)
        {
            var loadoutItem = loadout.Items[i];

            if (loadoutItem.BaseType.HasValue && loadoutItem.BaseType.Value != ItemType.None && !loadoutItem.BaseType.Value.IsAmmo())
            {
                var addedItem = player.Inventory?.AddItem(loadoutItem.BaseType.Value);

                if (addedItem != null)
                {
                    itemProcessor?.InvokeSafe(loadoutItem, addedItem);

                    AddedVanillaItem?.Invoke(player, loadout, loadoutItem, addedItem);
                }
                else
                {
                    ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for " +
                                               $"player &3{player.Nickname}&r (&6{player.UserId}&r): Could not add vanilla item &6{loadoutItem.BaseType.Value} to inventory!");
                }
            }
            else if (loadoutItem.CustomType != null)
            {
                if (!CustomItem.RegisteredObjects.TryGetValue(loadoutItem.CustomType, out var customItem))
                {
                    ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for " +
                                               $"player &3{player.Nickname}&r (&6{player.UserId}&r): Could not find custom item of ID &6{loadoutItem.CustomType}&r!");
                }
                else
                {
                    var itemInstance = customItem.AddItem(player);

                    if (itemInstance != null)
                    {
                        AddedCustomItem?.Invoke(player, loadout, loadoutItem, customItem, itemInstance);
                    }
                    else
                    {
                        ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for " +
                                                   $"player &3{player.Nickname}&r (&6{player.UserId}&r): Could not add custom item &6{customItem.Name}&r to inventory!");
                    }
                }
            }
            else
            {
                ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for player &3{player.Nickname}&r (&6{player.UserId}&r): Loadout contains an invalid item!");
            }
        }

        for (var i = 0; i < loadout.Ammo.Count; i++)
        {
            var loadoutAmmo = loadout.Ammo[i];

            if (loadoutAmmo.Amount < 1)
            {
                ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for player &3{player.Nickname}&r (&6{player.UserId}&r): Loadout contains an invalid ammo config!");
                continue;
            }

            if (loadoutAmmo.BaseType.HasValue && loadoutAmmo.BaseType.Value.IsAmmo())
            {
                player.Ammo.AddAmmo(loadoutAmmo.BaseType.Value, loadoutAmmo.Amount);
            }
            else if (loadoutAmmo.CustomType != null)
            {
                player.Ammo.AddCustomAmmo(loadoutAmmo.CustomType, loadoutAmmo.Amount);
            }
            else
            {
                ApiLog.Warn("LoadoutManager", $"Error while processing loadout &3{loadoutName}&r for player &3{player.Nickname}&r (&6{player.UserId}&r): Loadout contains an invalid ammo config!");
            }
        }

        AppliedLoadout?.InvokeSafe(player, loadout);
        return true;
    }
}
