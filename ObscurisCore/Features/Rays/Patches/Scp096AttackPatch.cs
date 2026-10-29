using HarmonyLib;

using LabExtended.API;

using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Rays.Enums;

using PlayerRoles.PlayableScps.Scp096;

using UnityEngine;

namespace ObscurisCore.Features.Rays.Patches;

/// <summary>
/// A static class that provides functionality related to modifying the behavior of SCP-096 when it interacts with ray-based objects.
/// </summary>
public static class Scp096AttackPatch
{
    /// <summary>
    /// Specifies the amount of damage dealt by SCP-096 when hitting ray objects.
    /// </summary>
    [Config("ray-manager", "scp-096-hit-damage", "The damage dealt by SCP-096 when hitting ray objects.")]
    public static float HitDamage = 10f;
    
    [HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.DamageSphere))]
    private static bool SpherePrefix(Scp096HitHandler __instance, Vector3 position, float radius)
    {
        if (!__instance._scpRole.TryGetOwner(out var owner) || !ExPlayer.TryGet(owner, out var player))
            return true;
        
        var hits = Physics.OverlapSphereNonAlloc(position, radius, Scp096HitHandler.Hits, Scp096HitHandler.AttackHitMask);
        
        ProcessHits(player, Scp096HitHandler.Hits, hits);

        __instance.ProcessHits(hits);
        return false;
    }

    [HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.DamageBox))]
    private static bool BoxPrefix(Scp096HitHandler __instance, Vector3 position, Vector3 halfExtents, Quaternion orientation)
    {
        if (!__instance._scpRole.TryGetOwner(out var owner) || !ExPlayer.TryGet(owner, out var player))
            return true;
        
        var hits = Physics.OverlapBoxNonAlloc(position, halfExtents, Scp096HitHandler.Hits, orientation, Scp096HitHandler.AttackHitMask);
        
        ProcessHits(player, Scp096HitHandler.Hits, hits);
        
        __instance.ProcessHits(hits);
        return false;
    }

    private static void ProcessHits(ExPlayer player, Collider[] hits, int count)
    {
        for (var x = 0; x < count; x++)
        {
            var hit = hits[x];
            
            if (hit == null)
                continue;
            
            if (!hit.gameObject.TryFindComponent<RayComponent>(out var rayComponent))
                continue;
            
            rayComponent.CallHit(player, ObjectHitSource.Scp096, HitDamage);
        }
    }
}