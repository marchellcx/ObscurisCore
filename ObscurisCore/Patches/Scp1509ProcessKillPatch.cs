using HarmonyLib;

using InventorySystem.Items.Scp1509;

using LabExtended.API.Custom.Items;
using ObscurisCore.Features.Custom.Items.Custom.Weapons;

namespace ObscurisCore.Patches
{
    /// <summary>
    /// Prevents the SCP-1509 from respawning a spectator if it's a custom item.
    /// </summary>
    [HarmonyPatch(typeof(Scp1509Item), nameof(Scp1509Item.ServerProcessKill))]
    public static class Scp1509ProcessKillPatch
    {
        private static bool Prefix(Scp1509Item __instance, ReferenceHub victim)
        {
            if (CustomItem.IsCustomItem<SimpleMachette>(__instance.ItemSerial, out _))
            {
                __instance.SendRpc(Scp1509MessageType.KilledPlayer);
                return false;
            }

            return true;
        }
    }
}