using HarmonyLib;

using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API.Custom.Items;
using ObscurisCore.Features.Custom.Items.Custom.Grenades;
using UnityEngine;

namespace ObscurisCore.Patches
{
    /// <summary>
    /// Provides a Harmony patch for the Scp018Projectile.RegisterBounce method to enable custom bounce processing for
    /// ReplicatingScp018 items.
    /// </summary>
    public static class Scp018BouncePatch
    {
        [HarmonyPatch(typeof(Scp018Projectile), nameof(Scp018Projectile.RegisterBounce))]
        private static bool Prefix(Scp018Projectile __instance, float velocity, Vector3 point)
        {
            if (CustomItem.IsCustomItem<ReplicatingScp018>(__instance.Info.Serial, out var replicatingScp018))
                replicatingScp018.ProcessBounce(__instance, velocity, point);

            return true;
        }
    }
}