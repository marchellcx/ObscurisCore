using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using Interactables.Interobjects.DoorButtons;

using InventorySystem.Items.Keycards;

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Extensions;

using LabExtended.Events.Firearms;

using MapGeneration;
using MapGeneration.Distributors;

using PlayerRoles;

using ObscurisCore.Extensions;

using UnityEngine;

using KeycardItem = InventorySystem.Items.Keycards.KeycardItem;

namespace ObscurisCore.Features;

/// <summary>
/// Provides methods to manage and evaluate player permissions for accessing doors within the game.
/// This class extends the functionality of a player object to allow checking
/// if they possess the necessary permissions for specific door-related actions.
/// </summary>
public static class RemoteKeycard
{
    private static LayerMask mask = LayerMask.GetMask("InteractableNoPlayerCollision");

    /// <summary>
    /// Gets an array of all possible <see cref="DoorPermissionFlags"/> values.
    /// This property provides a comprehensive list of all defined permission flags
    /// used for managing door access and related actions in the game.
    /// </summary>
    public static DoorPermissionFlags[] AllFlags { get; } = EnumUtils<DoorPermissionFlags>.Values.ToArray();

    /// <summary>
    /// Determines whether the available permissions meet or exceed the required permissions for a specific action or interaction.
    /// </summary>
    /// <param name="availablePerms">The set of permissions currently available to the player or entity.</param>
    /// <param name="requiredPerms">The set of permissions required to perform the action or interact with the object.</param>
    /// <returns>
    /// <c>true</c> if the available permissions satisfy the required permissions; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasPermissions(DoorPermissionFlags availablePerms, DoorPermissionFlags requiredPerms)
    {
        if (requiredPerms == DoorPermissionFlags.None)
            return true;

        if (requiredPerms.HasFlag(DoorPermissionFlags.ScpOverride) &&
            availablePerms.HasFlag(DoorPermissionFlags.ScpOverride))
            return true;

        for (var x = 0; x < AllFlags.Length; x++)
        {
            var flag = AllFlags[x];
            
            if (flag == DoorPermissionFlags.None || flag == DoorPermissionFlags.ScpOverride)
                continue;
            
            if (!requiredPerms.HasFlag(flag))
                continue;

            if (!availablePerms.HasFlag(flag))
                return false;
        }

        return true;
    }
    
    /// <summary>
    /// Retrieves all door permissions available to a player based on their role,
    /// carried keycards, and any custom or predefined permission details associated with those keycards.
    /// </summary>
    /// <param name="player">The player whose permissions are being retrieved.</param>
    /// <returns>
    /// A <see cref="DoorPermissionFlags"/> value representing the combined permissions
    /// available to the player. Returns <c>DoorPermissionFlags.None</c> if the player is invalid
    /// or no permissions are found.
    /// </returns>
    public static DoorPermissionFlags GetAllPermissions(this ExPlayer player)
    {
        try
        {
            if (!player.IsValidPlayer())
            {
                ApiLog.Debug("RemoteKeycard", "Invalid player instance");
                return DoorPermissionFlags.None;
            }

            var flags = player.Role.IsScp
                ? DoorPermissionFlags.ScpOverride
                : DoorPermissionFlags.None;

            foreach (var item in player.Inventory.Items)
            {
                if (item is not KeycardItem keycardItem)
                    continue;

                var perms = DoorPermissionFlags.None;

                if (keycardItem.Details.TryGetFirst<CustomPermsDetail>(out _)
                    && CustomPermsDetail.CustomPermissions.TryGetValue(keycardItem.ItemSerial, out var customPerms))
                    perms = customPerms;
                else if (keycardItem.Details.TryGetFirst<PredefinedPermsDetail>(out var predefinedPermsDetail))
                    perms = predefinedPermsDetail.Levels.Permissions;
                
                flags |= perms;
            }

            return flags;
        }
        catch (Exception ex)
        {
            ApiLog.Error("RemoteKeycard", ex);
            return DoorPermissionFlags.None;
        }
    }
    
    private static void OnInteractingGenerator(PlayerInteractingGeneratorEventArgs args)
    {
        if (args.Generator.IsUnlocked || args.ColliderId != Scp079Generator.GeneratorColliderId.Door)
            return;
        
        if (!args.Player.CastPlayer(out var player))
            return;

        var perms = player.GetAllPermissions();
        
        if (HasPermissions(perms, args.Generator.RequiredPermissions))
        {
            args.IsAllowed = false;
            args.Generator.IsUnlocked = true;
        }
    }

    private static void OnInteractingDoor(PlayerInteractingDoorEventArgs args)
    {
        if (args.CanOpen)
            return;

        if (!args.Player.CastPlayer(out var player))
            return;
        
        var lockMode = DoorLockUtils.GetMode(args.Door.Base);

        if (!player.IsBypassEnabled && lockMode == DoorLockMode.FullLock)
            return;

        var permissions = player.GetAllPermissions();

        var lockCanOpen = lockMode.HasFlagFast(DoorLockMode.CanOpen);
        var lockCanClose = lockMode.HasFlagFast(DoorLockMode.CanClose);
        var lockScpOverride = lockMode.HasFlagFast(DoorLockMode.ScpOverride);
        
        if ((lockScpOverride && player.Role.IsScp)
            || (lockCanOpen && args.Door.Base.TargetState)
            || (lockCanClose && !args.Door.Base.TargetState))
        {
            if (player.IsBypassEnabled
                || player.Role.Is(RoleTypeId.Scp079)
                || HasPermissions(permissions, args.Door.Base.RequiredPermissions.RequiredPermissions))
            {
                args.CanOpen = true;
                args.IsAllowed = true;
            }
        }
    }

    private static void OnInteractingLocker(PlayerInteractingLockerEventArgs args)
    {
        if (args.CanOpen)
            return;

        if (!args.Player.CastPlayer(out var player))
            return;

        var perms = player.GetAllPermissions();
        
        if (HasPermissions(perms, args.Chamber.RequiredPermissions))
        {
            args.CanOpen = true;
            args.IsAllowed = true;
        }
    }

    private static void OnShot(FirearmRayCastEventArgs args)
    {
        if (args.Hit.HasValue && ProcessHit(args.Hit.Value, args.Player))
            return;

        if (Physics.Raycast(args.Ray, out var hit, args.MaxDistance, mask))
            ProcessHit(hit, args.Player);
    }

    private static bool ProcessHit(RaycastHit hit, ExPlayer player)
    {
        try
        {
            if (hit.collider.transform.TryGetComponentInParent<ButtonVariant>(out var button))
            {
                if (button.ParentDoor == null)
                {
                    ApiLog.Warn("RemoteKeycard", $"Door button &1{button.name}&r has no parent door!");
                    return true;
                }

                button.ParentDoor.ServerInteract(player.ReferenceHub, 0);
                return true;
            } 
            
            if (hit.collider.transform.TryGetComponentInParent<ElevatorPanel>(out var panel))
            {
                if (panel.AssignedChamber == null)
                {
                    ApiLog.Warn("RemoteKeycard", $"Elevator panel &1{panel.name}&r has no assigned chamber!");
                    return true;
                }

                if (!panel.AssignedChamber.IsReadyForUserInput)
                {
                    ApiLog.Warn("RemoteKeycard", $"Elevator &1{panel.AssignedChamber.AssignedGroup}&r is not ready!");
                    return true;
                }

                var lockMode = DoorLockUtils.GetMode(panel.AssignedChamber.ActiveLocksAllDoors);

                if (lockMode == DoorLockMode.FullLock)
                    return true;

                if (!lockMode.HasFlagFast(DoorLockMode.ScpOverride) || !player.Role.IsScp)
                {
                    if (!lockMode.HasFlagFast(DoorLockMode.CanClose))
                    {
                        return true;
                    }
                }

                if (panel.AssignedChamber.FloorDoors.Any(d => d.IsInZone(FacilityZone.LightContainment)) && Decontamination.IsDecontaminating)
                    return true;

                if (Warhead.IsDetonated)
                    return true;

                panel.AssignedChamber.ServerSetDestination(panel.AssignedChamber.NextLevel, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            ApiLog.Error("RemoteKeycard", ex);
        }

        return false;
    }
    
    private static void Initialize()
    {
        ExFirearmEvents.RayCast += OnShot;
        
        PlayerEvents.InteractingDoor += OnInteractingDoor;
        PlayerEvents.InteractingLocker += OnInteractingLocker;
        PlayerEvents.InteractingGenerator += OnInteractingGenerator;
    }
}