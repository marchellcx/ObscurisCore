using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Extensions;
using LabExtended.Utilities;

using NiveraAPI.IO.Configs;

namespace ObscurisCore.Features;

/// <summary>
/// Manages whitelist tables.
/// </summary>
public static class WhitelistManager
{
    private static string? activeTable;
    private static List<string>? activeList;

    private static Dictionary<string, List<string>> whitelists = new();

    /// <summary>
    /// Gets the name of the currently active whitelist table.
    /// </summary>
    public static string? ActiveTable => activeTable;

    /// <summary>
    /// Gets the list of currently active whitelist.
    /// </summary>
    public static IReadOnlyList<string>? ActiveList => activeList;

    /// <summary>
    /// Gets the collection of whitelists, where each whitelist is represented as a dictionary mapping a string key
    /// to a list of associated string values.
    /// </summary>
    public static IReadOnlyDictionary<string, List<string>> Whitelists => whitelists;

    /// <summary>
    /// The name of the default whitelist table to enable by default.
    /// </summary>
    [Config("whitelistManager", "defaultWhitelist", "The name of the default whitelist table to enable by default.")]
    public static string? DefaultWhitelist { get; set; } = null;

    /// <summary>
    /// Disables the currently active whitelist, if one exists.
    /// </summary>
    /// <remarks>If a whitelist is active, it is disabled and the associated active table and list are
    /// set to null. This method logs an informational message when the whitelist is disabled and a warning if no
    /// whitelist is active.</remarks>
    /// <returns>true if the whitelist was successfully disabled; otherwise, false if no whitelist is currently active.</returns>
    public static bool DisableWhitelist()
    {
        if (activeTable != null)
        {
            ApiLog.Info($"Whitelist &1{activeTable}&r has been disabled.");

            activeTable = null;
            activeList = null;

            return true;
        }

        ApiLog.Warn("No whitelist is currently active.");
        return false;
    }

    /// <summary>
    /// Enables the whitelist for the specified table, allowing only whitelisted entries to be processed for that
    /// table.
    /// </summary>
    /// <remarks>If the specified table does not have an associated whitelist, a warning is logged and
    /// the method returns false.</remarks>
    /// <param name="table">The name of the table for which the whitelist should be enabled. This parameter cannot be null or consist
    /// only of whitespace.</param>
    /// <returns>true if the whitelist for the specified table was successfully enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the table parameter is null or consists only of whitespace.</exception>
    public static bool EnableWhitelist(string table)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table));

        if (whitelists.TryGetValue(table, out var list))
        {
            activeTable = table;
            activeList = list;

            ApiLog.Info($"Whitelist &1{table}&r has been enabled.");

            KickNonWhitelisted();
            return true;
        }

        ApiLog.Warn($"Whitelist &1{table}&r does not exist.");
        return false;
    }

    /// <summary>
    /// Adds the specified entries to the whitelist for the given table, ensuring that only unique entries are
    /// added.
    /// </summary>
    /// <remarks>If the table already exists in the whitelist, only unique entries from the provided
    /// collection will be added. The method saves the updated whitelist for the table after modification.</remarks>
    /// <param name="table">The name of the table to which the entries will be added. This parameter cannot be null or whitespace.</param>
    /// <param name="whitelist">An enumerable collection of strings representing the entries to be added to the whitelist. This parameter
    /// cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="table"/> or <paramref name="whitelist"/> is null or empty.</exception>
    public static void AddToWhitelist(string table, IEnumerable<string> whitelist)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table));

        if (whitelist == null)
            throw new ArgumentNullException(nameof(whitelist));

        if (whitelists.TryGetValue(table, out var list))
        {
            list.AddRangeWhere(whitelist, str => !list.Contains(str));
        }
        else
        {
            list = new(whitelist);

            whitelists.Add(table, list);
        }

        SaveTable(table);
    }

    /// <summary>
    /// Removes the specified entries from the whitelist associated with the given table. If the whitelist becomes
    /// empty after removal, the table is deleted; otherwise, the updated whitelist is saved.
    /// </summary>
    /// <remarks>If all entries are removed from the whitelist, the associated table is deleted.
    /// Otherwise, the table is updated to reflect the changes to the whitelist.</remarks>
    /// <param name="table">The name of the table from which entries are to be removed. This parameter cannot be null, empty, or consist
    /// only of white-space characters.</param>
    /// <param name="whitelist">A collection of strings representing the entries to remove from the whitelist. This parameter cannot be
    /// null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the table name is null, empty, or consists only of white-space characters, or if the whitelist
    /// collection is null.</exception>
    public static void RemoveFromWhitelist(string table, IEnumerable<string> whitelist)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table));

        if (whitelist == null)
            throw new ArgumentNullException(nameof(whitelist));

        if (whitelists.TryGetValue(table, out var list))
        {
            if (list.RemoveAll(str => whitelist.Contains(str)) > 0)
            {
                if (list.Count < 1)
                {
                    DeleteTable(table);
                }
                else
                {
                    SaveTable(table);
                }
            }
        }
    }

    private static void DeleteTable(string table)
    {
        if (whitelists.Remove(table))
        {
            var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "whitelists", $"{table}.txt");

            if (activeTable != null && activeTable == table)
            {
                activeList = null;
                activeTable = null;

                ApiLog.Info($"Currently active whitelist &1{table}&r has been disabled due to being removed.");
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    ApiLog.Error($"Failed to delete whitelist file for &1{table}&r:\n{ex}");
                }
            }

            ApiLog.Info($"Whitelist &1{table}&r has been removed.");
        }
    }

    private static void SaveTable(string table)
    {
        if (whitelists.TryGetValue(table, out var list))
        {
            var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "whitelists", $"{table}.txt");

            try
            {
                File.WriteAllLines(path, list);
            }
            catch (Exception ex)
            {
                ApiLog.Error($"Failed to save whitelist file for &1{table}&r:\n{ex}");
            }

            ApiLog.Info($"Whitelist &1{table}&r has been saved.");
        }
    }

    private static void KickNonWhitelisted()
    {
        if (activeTable == null || activeList == null || activeList.Count < 1)
            return;

        var list = ExPlayer.Players.ToPooledList();

        list.ForEach(p =>
        {
            if (p?.ReferenceHub != null
                && p.IsOnlineAndVerified
                && !p.IsNorthwoodStaff
                && !p.HasPermission(PlayerPermissions.GameplayData))
            {
                if (!activeList.Contains(p.UserId)
                    && !activeList.Contains(p.IpAddress)
                    && !(p.UserId.TrySplit('@', true, 2, out var parts) && activeList.Contains(parts[0])))
                {
                    p.Kick("[CZ] <b><color=red>Na tomto serveru je aktivní whitelist!</color></b>\n" +
                           "[EN] <b><color=red>You are not whitelisted on this server!</color></b>");
                }
            }
        });

        list.ReturnToPool();
    }

    private static void OnPreAuth(PlayerPreAuthenticatingEventArgs args)
    {
        if (!args.CanJoin)
            return;

        if (activeTable == null || activeList == null || activeList.Count < 1)
            return;

        if (args.Flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist))
            return;

        if (activeList.Contains(args.UserId) || activeList.Contains(args.IpAddress))
            return;

        if (args.UserId.TrySplit('@', true, 2, out var parts) && activeList.Contains(parts[0]))
            return;

        if (ServerStatic.PermissionsHandler != null
            && ServerStatic.PermissionsHandler.Groups.TryGetValue(args.UserId, out var group)
            && group != null
            && PermissionsHandler.IsPermitted(group.Permissions, PlayerPermissions.GameplayData))
            return;

        args.RejectCustom(
            "[CZ] <b><color=red>Na tomto serveru je aktivní whitelist!</color></b>\n" +
            "[EN] <b><color=red>You are not whitelisted on this server!</color></b>");
    }

    private static void Initialize()
    {
        var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "whitelists");

        foreach (var file in Directory.GetFiles(path, "*.txt"))
        {
            var table = Path.GetFileNameWithoutExtension(file);
            var list = File.ReadAllLines(file).Where(x => !string.IsNullOrEmpty(x) && x[0] != '#').ToList();

            whitelists.Add(table, list);

            ApiLog.Info($"Whitelist &1{table}&r has been loaded with {list.Count} entries.");

            if (!string.IsNullOrWhiteSpace(DefaultWhitelist) && DefaultWhitelist == table)
            {
                activeTable = table;
                activeList = list;

                ApiLog.Info($"Whitelist &1{table}&r has been enabled by default.");

                KickNonWhitelisted();
            }
        }

        PlayerEvents.PreAuthenticating += OnPreAuth;
    }
}