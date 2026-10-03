using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;
using ObscurisCore.Features;

namespace ObscurisCore.Commands
{
    /// <summary>
    /// Provides commands for managing whitelist tables, including adding user IDs or IP addresses to specified tables
    /// for access control.
    /// </summary>
    [Command("whitelist", "Manages the whitelist.")]
    public class WhitelistCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("add", "Adds entries to a whitelist table.", null)]
        private void Add(
            [CommandParameter("Table", "Name of the whitelist table.")] string table,
            [CommandParameter("List", "List of user IDs (IPs) to add.")] List<string> list)
        {
            WhitelistManager.AddToWhitelist(table, list);

            Ok($"Added &1{list.Count}&r entries to whitelist &3{table}&r");
        }

        [CommandOverload("remove", "Removes entries from a whitelist table.", null)]
        private void Remove(
            [CommandParameter("Table", "Name of the whitelist table.")] string table,
            [CommandParameter("List", "List of user IDs (IPs) to remove.")] List<string> list)
        {
            WhitelistManager.RemoveFromWhitelist(table, list);

            Ok($"Removed &1{list.Count}&r entries from whitelist &3{table}&r");
        }

        [CommandOverload("enable", "Enables a whitelist tabl.", null)]
        private void Enable(
            [CommandParameter("Table", "Name of the whitelist table.")] string table)
        {
            if (WhitelistManager.EnableWhitelist(table))
            {
                Ok($"Enabled whitelist &3{table}&r");
            }
            else
            {
                Fail($"Failed to enable whitelist &3{table}&r");
            }
        }

        [CommandOverload("disable", "Disables the active whitelist.", null)]
        private void Disable()
        {
            if (WhitelistManager.DisableWhitelist())
            {
                Ok("Disabled whitelist");
            }
            else
            {
                Fail("Failed to disable whitelist");
            }
        }

        [CommandOverload("list", "Lists all whitelist tables and their statuses.", null)]
        private void List()
        {
            var tables = WhitelistManager.Whitelists;

            if (tables.Count == 0)
            {
                Ok("No whitelist tables found.");
                return;
            }

            var response = "Whitelist tables:\n";

            foreach (var table in tables)
                response += $"- &3{table}&r {(WhitelistManager.ActiveTable != null && table.Key == WhitelistManager.ActiveTable ? "[&2ACTIVE&r]" : "[&1INACTIVE&r]")}\n";

            Ok(response);
        }
    }
}