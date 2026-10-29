using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using ObscurisCore.Extensions;

namespace ObscurisCore.Commands;

/// <summary>
/// Represents a command that causes the player who executed it to be killed.
/// </summary>
[Command("suicide", "Zabije hráče který využil tento příkaz.", "kms")]
public class SuicideCommand : CommandBase, IPlayerCommand
{
    [CommandOverload("Zabije hráče který využil tento příkaz.", null)]
    private void Invoke()
    {
        Sender.Explode(2, ItemType.GrenadeHE, "Spáchal sebevraždu.", true, true, 20f);
        
        Ok("Sebevražda spáchána.");
    }
}