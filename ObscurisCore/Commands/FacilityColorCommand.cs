using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using UnityEngine;

namespace ObscurisCore.Commands
{
    /// <summary>
    /// Provides commands for managing the color of facility lights, including setting a specific color or resetting to
    /// the default.
    /// </summary>
    [Command("fcolor", "Commands for managing facility light colors.")]
    public class FacilityColorCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("set", "Sets the facility light color.", "fcolor.set")]
        private void Set(
            [CommandParameter("Color", "The color to set.")] Color color)
        {
            ExMap.SetLightColor(color);

            Ok($"Set facility lights to: &1{color.ToHex()}&r");
        }

        [CommandOverload("reset", "Resets the facility light color to default.", "fcolor.reset")]
        private void Reset()
        {
            ExMap.ResetLightsColor();

            Ok($"Reset facility lights color.");
        }
    }
}