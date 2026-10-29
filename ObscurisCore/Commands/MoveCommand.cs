using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using ObscurisCore.Extensions;
using ObscurisCore.Features.Custom.Items.Custom.Weapons;

namespace ObscurisCore.Commands;

/// <summary>
/// Represents a command used for moving a player within the system.
/// Implements the server-side behavior for the "move" action.
/// </summary>
[Command("move", "Moves a player.")]
public class MoveCommand : CommandBase, IServerSideCommand
{
    [CommandOverload("push", "Pushes a player.", null)]
    private void Push(
        [CommandParameter("Target", "The target player.")] ExPlayer target, 
        [CommandParameter("Force", "The force to push the player with.")] float force, 
        [CommandParameter("Duration", "The duration of the push.")] float duration)
    {
        if (target.Push(target.GetMoveFactor(), force, duration, Sender.CameraTransform.forward.NormalizeIgnoreY()))
            Ok($"Pushed {target.ToLogString()}!");
        else
            Fail($"Could not push {target.ToLogString()}");
    }
    
    [CommandOverload("pull", "Pulls a player.", null)]
    private void Pull(
        [CommandParameter("Target", "The target player.")] ExPlayer target, 
        [CommandParameter("Force", "The force to pull the player with.")] float force, 
        [CommandParameter("Duration", "The duration of the pull.")] float duration)
    {
        if (target.Pull(target.GetMoveFactor(), force, duration, -Sender.CameraTransform.forward.NormalizeIgnoreY()))
            Ok($"Pulled {target.ToLogString()}!");
        else
            Fail($"Could not pull {target.ToLogString()}");
    }

    [CommandOverload("mode", "Sets whether or not the gun should be pushing.", null)]
    private void Mode([CommandParameter("Push", "Whether or not the gun should be pushing.")] bool push)
    {
        if (Sender.Inventory.CurrentItem == null || !MoveGun.Singleton.CheckItem<MoveGun.MoveData>(Sender.Inventory.CurrentItem, out var moveData))
        {
            Fail("You do not have the &3Move Gun&r equipped.");
            return;
        }

        moveData.IsPushing = push;
        
        Ok($"Held &3Move Gun&r instance will now be pushing.");
    }

    [CommandOverload("force", "Sets the force of the gun pushing / pulling.", null)]
    private void Force([CommandParameter("Force", "The force of the gun pushing / pulling.")] float force)
    {
        if (Sender.Inventory.CurrentItem == null || !MoveGun.Singleton.CheckItem<MoveGun.MoveData>(Sender.Inventory.CurrentItem, out var moveData))
        {
            Fail("You do not have the &3Move Gun&r equipped.");
            return;
        }

        moveData.Force = force;
        
        Ok($"Held &3Move Gun&r instance was set to a force of &3{force}&r.");
    }
    
    [CommandOverload("duration", "Sets the duration of the gun pushing / pulling.", null)]
    private void Duration([CommandParameter("Duration", "The duration of the gun pushing / pulling.")] float duration)
    {
        if (Sender.Inventory.CurrentItem == null || !MoveGun.Singleton.CheckItem<MoveGun.MoveData>(Sender.Inventory.CurrentItem, out var moveData))
        {
            Fail("You do not have the &3Move Gun&r equipped.");
            return;
        }

        moveData.Duration = duration;
        
        Ok($"Held &3Move Gun&r instance was set to a duration of &3{duration}&rs.");
    }
}