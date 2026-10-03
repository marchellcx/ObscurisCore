using System.Collections.ObjectModel;

using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Utilities;
using LabExtended.Extensions;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Looting.Configs;

namespace ObscurisCore.Features.Looting;

/// <summary>
/// Provides methods and data structures for managing loot tables
/// and executing loot-related functionalities, such as spawning items
/// or running custom loot functions.
/// </summary>
public static class LootManager
{
    private static Dictionary<string, LootFunction> functions = new();
    
    /// <summary>
    /// Gets a read-only dictionary of all registered loot tables.
    /// </summary>
    public static ReadOnlyDictionary<string, List<LootTable>> Tables { get; private set; }
    
    /// <summary>
    /// Gets a read-only dictionary of all registered loot functions.
    /// </summary>
    public static ReadOnlyDictionary<string, LootFunction> Functions { get; private set; }

    /// <summary>
    /// Executes the loot entry associated with the specified entry identifier for a given player.
    /// Processes and spawns items, ammo, or executes custom functions as defined in the loot table.
    /// </summary>
    /// <param name="target">The player on whom the loot entry actions will be executed. Cannot be null.</param>
    /// <param name="entry">The unique identifier of the loot entry to execute. Cannot be null or empty.</param>
    /// <returns>
    /// True if the loot entry was successfully processed, or false if the entry identifier is invalid
    /// or no associated loot table is found.
    /// </returns>
    public static bool ExecuteEntry(ExPlayer target, string entry)
    {
        if (string.IsNullOrEmpty(entry))
            return false;
        
        var table = GetTable(entry);

        if (!table.HasValue)
            return false;

        foreach (var item in table.Value.Items)
        {
            try
            {
                if (item.IsAmmo && item.Type.IsAmmo())
                {
                    if (item.SpawnOnGround)
                    {
                        var pickup =
                            ExMap.SpawnItem<AmmoPickup>(item.Type, target.Position, item.Scale, target.Rotation);

                        if (pickup != null)
                        {
                            pickup.NetworkSavedAmmo = (ushort)item.Amount;
                        }
                    }
                    else
                    {
                        target.Ammo.AddAmmo(item.Type, (ushort)item.Amount);
                    }
                }
                else if (item.IsItem && item.Type != ItemType.None)
                {
                    for (var x = 0; x < item.Amount; x++)
                    {
                        if (item.SpawnOnGround || target.Inventory.ItemCount >= 8)
                        {
                            if (item.FuseTime > 0f
                                && item.Type.TryGetItemPrefab(out var prefab) && prefab is ThrowableItem)
                            {
                                ExMap.SpawnProjectile(item.Type, target.Position, item.Scale,
                                    (item.UsePlayerVelocity ? target.Velocity : item.Velocity), target.Rotation,
                                    item.ThrowForce, item.FuseTime);
                            }
                            else
                            {
                                ExMap.SpawnItem(item.Type, target.Position, item.Scale, target.Rotation);
                            }
                        }
                        else
                        {
                            target.Inventory.AddItem(item.Type, ItemAddReason.AdminCommand);
                        }
                    }
                }
                else if (item.IsCustomItem && item.CustomItem != null)
                {
                    for (var x = 0; x < item.Amount; x++)
                    {
                        if (item.SpawnOnGround || target.Inventory.ItemCount >= 8)
                        {
                            item.CustomItem.SpawnItem(target.Position, target.Rotation);
                        }
                        else
                        {
                            item.CustomItem.AddItem(target);
                        }
                    }
                }
                else if (item.IsAction && item.Function != null)
                {
                    item.Function(target, item.Args ?? Array.Empty<string>());
                }
            }
            catch (Exception ex)
            {
                ApiLog.Error("LootManager", $"Error while executing loot item:\n{ex}");
                return false;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Retrieves a loot table associated with the specified entry identifier.
    /// </summary>
    /// <param name="entry">The unique identifier of the loot entry to search for. Cannot be null or empty.</param>
    /// <returns>
    /// The selected loot table associated with the provided entry identifier,
    /// or null if no valid loot table is found.
    /// </returns>
    public static LootTable? GetTable(string entry)
    {
        if (string.IsNullOrEmpty(entry))
            return null;

        if (!Tables.TryGetValue(entry, out var list) || list?.Count < 1)
        {
            ApiLog.Error("LootManager", $"Could not find loot tables for entry &1{entry}&r");
            return null;
        }

        var table = list.GetRandomWeighted(t => t.Weight);

        if (table.Items?.Count < 1)
        {
            ApiLog.Error("LootManager", $"Selected table for &1{entry}&r does not contain any items!");
            return null;
        }

        return table;
    }
    
    /// <summary>
    /// Registers a new loot function with the specified identifier and implementation.
    /// </summary>
    /// <param name="id">The unique identifier for the loot function. Cannot be null or empty.</param>
    /// <param name="function">The delegate representing the loot function implementation. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="id"/> is null or empty, or when <paramref name="function"/> is null.
    /// </exception>
    public static void RegisterFunction(string id, LootFunction function)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        
        if (function == null)
            throw new ArgumentNullException(nameof(function));
        
        functions[id] = function;
    }
    
    private static void Initialize()
    {
        Functions = new(functions);
        
        var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "loot_tables.txt");

        void WriteExample()
        {
            File.WriteAllText(path,
                $"# This file contains all loot tables used by features in ObscurisCore\n" +
                $"# Lines starting with a hashtag are ignored, the file will never be overwritten by the plugin\n" +
                $"# Any parsing errors will be logged to the console and the associated table will be skipped\n" +
                $"# Arguments are split by spaces, you can use quotation marks to make parsing ignore these\n" +
                $"# Every line must start with a number representing the weight of the table\n" +
                $"# Lines that do NOT begin with numbers are treated as the start of a new entry\n" +
                $"# Entry names should be the same as the intended feature requires them to be.\n" +
                $"# Loot tables support item IDs, custom item IDs and custom action IDs defined by other plugins\n" +
                $"# Anything after the action's ID is treated as a parameter for the action until the next empty space\n" +
                $"# Parameters for items: ItemID:Amount:SpawnOnGround:Scale\n" +
                $"# Parameters for explosive items: ItemID:Amount:SpawnOnGround:Scale:FuseTime:ThrowForce:Velocity (set Velocity to true to use player's current velocity)" +
                $"# Parameters for custom items: CustomItemID:Amount:SpawnOnGround\n" +
                $"# Parameters for functions: FunctionID:Amount:Arguments\n" +
                $"# Amount = integer number\n" +
                $"# SpawnOnGround = true / false (will not be added if the target's inventory is full when set to false)\n" +
                $"# Scale = item scale vector (X,Y,Z)\n" +
                $"# FuseTime = seconds till explosion\n" +
                $"# ThrowForce = throw velocity multiplier (number)\n" +
                $"# Velocity = player velocity vector (direction and speed the item will be thrown in) (X,Y,Z)\n" +
                $"# Arguments = a list of strings split by comma\n" +
                $"# Formatting:\n" +
                $"ExampleEntryName\n" +
                $"30 ItemID:2:false ActionID:1 CustomItemID:2:true");
        }
        
        if (!File.Exists(path))
        {
            WriteExample();
        }
        else
        {
            var lines = File.ReadAllLines(path);

            if (lines.Length == 0)
            {
                WriteExample();
            }
            else
            {
                ApiLog.Debug("LootManager", "Parsing loot tables ...");
             
                var dict = new Dictionary<string, List<LootTable>>();
                
                float? curEntryWeight = null;
                string? curEntryName = null;

                List<LootItem>? curItems = null;

                void SaveTable()
                {
                    if (!string.IsNullOrEmpty(curEntryName) 
                        && curEntryWeight != null
                        && curItems?.Count > 0)
                    {
                        if (!dict.TryGetValue(curEntryName, out var list))
                            dict.Add(curEntryName, list = new());
                            
                        list.Add(new(curEntryWeight.Value, curItems.AsReadOnly()));
                            
                        ApiLog.Debug("LootManager", $"Added a table to entry &1{curEntryName}&r (&1{curEntryWeight.Value}&r) with &3{curItems.Count}&r item(s)");

                        curItems = null;
                        curEntryName = null;
                        curEntryWeight = null;
                    }
                }
                
                for (var x = 0; x < lines.Length; x++)
                {
                    try
                    {
                        var line = lines[x].Trim();

                        if (string.IsNullOrEmpty(line))
                            continue;

                        if (line[0] == '#')
                            continue;

                        var splits = line.SplitOutsideQuotes(' ', true, true, false);

                        if (curEntryName != null
                            && splits.Length > 1
                            && float.TryParse(splits[0], out var weight))
                        {
                            SaveTable();

                            curItems = new();
                            curEntryWeight = weight;

                            LootItem.ParseItems(splits.Skip(1).ToArray(), curItems);

                            SaveTable();
                        }
                        else if (splits.Length == 1)
                        {
                            SaveTable();

                            curEntryName = splits[0];

                            ApiLog.Debug("LootManager", $"Found entry: {curEntryName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ApiLog.Error("LootManager", $"Error while parsing line &1{lines[x]}&r:\n{ex}");
                    }
                }
                
                SaveTable();

                Tables = new ReadOnlyDictionary<string, List<LootTable>>(dict);
                
                ApiLog.Info("LootManager", $"Finished parsing loot tables: &1{Tables.Count}&r entries");
            }
        }
    }
}