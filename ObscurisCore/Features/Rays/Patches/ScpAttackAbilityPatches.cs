using System.Reflection;

using LabExtended.API;
using LabExtended.Core;

using NiveraAPI.Extensions;
using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Rays.Enums;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp049.Zombies;

using PlayerRoles.PlayableScps.Subroutines;

using UnityEngine;

namespace ObscurisCore.Features.Rays.Patches;

/// <summary>
/// Provides patching mechanisms for SCP attack abilities in the game.
/// This static class contains methods and configurations that influence the behavior
/// of SCP attack abilities, such as modifying damage values for specific SCPs
/// and handling detection logic for attack targets.
/// </summary>
public static class ScpAttackAbilityPatches
{
    /// <summary>
    /// Represents the damage dealt by SCP-049-2 when it executes an attack.
    /// </summary>
    [Config("ray-manager", "scp-049-2-hit-damage", "Damage dealt by SCP-049-2.")]
    public static float Scp0492HitDamage = 10f;

    /// <summary>
    /// Represents the distance at which SCP-049-2 can hit its target during an attack.
    /// </summary>
    [Config("ray-manager", "scp-049-2-hit-distance", "Distance at which SCP-049-2 can hit targets.")]
    public static float Scp0492HitDistance = 10f;

    /// <summary>
    /// Represents the damage dealt by SCP-939 when it executes an attack.
    /// </summary>
    [Config("ray-manager", "scp-939-hit-damage", "Damage dealt by SCP-939.")]
    public static float Scp939HitDamage = 10f;

    /// <summary>
    /// Represents the distance at which SCP-939 can hit its target during an attack.
    /// </summary>
    [Config("ray-manager", "scp-939-hit-distance", "Distance at which SCP-939 can hit targets.")]
    public static float Scp939HitDistance = 10f;

    /// <summary>
    /// Represents the damage dealt by SCP-3114 when it executes an attack.
    /// </summary>
    [Config("ray-manager", "scp-3114-hit-damage", "Damage dealt by SCP-3114.")]
    public static float Scp3114HitDamage = 10f;

    /// <summary>
    /// Represents the distance at which SCP-3114 can hit its target during an attack.
    /// </summary>
    [Config("ray-manager", "scp-3114-hit-distance", "Distance at which SCP-3114 can hit targets.")]
    public static float Scp3114HitDistance = 10f;

    private static MethodInfo prefixMethod = typeof(ScpAttackAbilityPatches).FindMethod(nameof(Prefix))!;

    private static bool Prefix<T>(ReferenceHub detector, float offset, float radius, bool losTest, ref ArraySegment<IDestructible> __result) where T : PlayerRoleBase, IFpcRole
    {
        if (!ExPlayer.TryGet(detector, out var player))
            return true;
        
        var source = ObjectHitSource.Firearm;
        
        var damage = 0f;
        var distance = 0f;

        if (player.Role.Is(RoleTypeId.Scp0492))
        {
            source = ObjectHitSource.Scp0492;
            
            damage = Scp0492HitDamage;
            distance = Scp0492HitDistance;
        }
        else if (player.Role.Is(RoleTypeId.Scp939))
        {
            source = ObjectHitSource.Scp939;
            
            damage = Scp939HitDamage;
            distance = Scp939HitDistance;
        }
        else if (player.Role.Is(RoleTypeId.Scp3114))
        {
            source = ObjectHitSource.Scp3114;
            
            damage = Scp3114HitDamage;
            distance = Scp3114HitDistance;
        }
        
        if (distance > 0f && player.TryRayCastComponent<RayComponent>(distance, null, null, null, out var rayComponent))
        {
            rayComponent.CallHit(player, source, damage);
        }
        
        var cameraPos = detector.PlayerCameraReference.position;
        
        var num = Physics.OverlapSphereNonAlloc(detector.PlayerCameraReference.TransformPoint(Vector3.forward * offset), radius, ScpAttackAbilityBase<T>.DetectionsNonAlloc, ScpAttackAbilityBase<T>.DetectionMask);
        var count = 0;
        
        for (var i = 0; i < num; i++)
        {
            var collider = ScpAttackAbilityBase<T>.DetectionsNonAlloc[i];
            
            if (collider.TryGetComponent<IDestructible>(out var component) && component.NetworkId != detector.netId && (!losTest || CheckLineOfSight(collider, component.CenterOfMass)))
                ScpAttackAbilityBase<T>.DestDetectionsNonAlloc[count++] = component;
        }
        
        __result = new ArraySegment<IDestructible>(ScpAttackAbilityBase<T>.DestDetectionsNonAlloc, 0, count);
        return false;
        
        bool CheckLineOfSight(Collider hitColldier, Vector3 hitCenterOfMass)
        {
            if (!Physics.Linecast(cameraPos, hitCenterOfMass, out var hitInfo, PlayerRolesUtils.AttackMask))
            {
                return true;
            }
            
            if (hitInfo.colliderInstanceID == hitColldier.GetInstanceID())
            {
                return true;
            }
            
            return false;
        }
    }

    private static void Initialize()
    {
        var scp0492method = prefixMethod.MakeGenericMethod(typeof(ZombieRole));
        var scp0492target = typeof(ScpAttackAbilityBase<ZombieRole>).FindMethod(nameof(ScpAttackAbilityBase<>.DetectDestructibles))!;
        
        var scp939method = prefixMethod.MakeGenericMethod(typeof(Scp939Role));
        var scp939target = typeof(ScpAttackAbilityBase<Scp939Role>).FindMethod(nameof(ScpAttackAbilityBase<>.DetectDestructibles))!;
        
        var scp3114method = prefixMethod.MakeGenericMethod(typeof(Scp3114Role));
        var scp3114target = typeof(ScpAttackAbilityBase<Scp3114Role>).FindMethod(nameof(ScpAttackAbilityBase<>.DetectDestructibles))!;
        
        ApiPatcher.Harmony.Patch(scp0492target, new(scp0492method));
        ApiPatcher.Harmony.Patch(scp939target, new(scp939method));
        ApiPatcher.Harmony.Patch(scp3114target, new(scp3114method));
    }
}