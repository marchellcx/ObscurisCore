using HarmonyLib;
using LabExtended.API.Hints;
using LabExtended.Core;
using ObscurisCore.Utilities.Configs;

namespace ObscurisCore.Patches
{
    /// <summary>
    /// Provides functionality to override the server name overlay in the Peanut Club overlay system.
    /// </summary>
    public static class BasicOverlaysServerNameOverridePatch
    {
        /// <summary>
        /// Gets the overlay options used to display the server name, or null if no overlay is configured.
        /// </summary>
        public static OverlayOptions? ServerNameOverlay { get; internal set; }

        /// <summary>
        /// Gets the PeanutClub overlay class.
        /// </summary>
        public static readonly Type? PeanutClubOverlayType = Type.GetType("BasicOverlays.Overlays.PeanutClubOverlay", false);

        [HarmonyPatch("BasicOverlays.Overlays.PeanutClubOverlay", "OnUpdate")]
        private static bool UpdatePrefix(object __instance)
        {
            if (ServerNameOverlay is null)
                return true;

            if (__instance == null || PeanutClubOverlayType == null) 
                return true;

            var field = AccessTools.Field(PeanutClubOverlayType, "LocalData");
            var value = field.GetValue(__instance) as IEnumerable<HintData>;

            if (value is null)
            {
                ApiLog.Warn("ObscurisCore", "PeanutClubOverlay.LocalData is null");
                return true;
            }

            var data = value.ElementAtOrDefault(1);

            if (data is null)
            {
                ApiLog.Warn("ObscurisCore", "PeanutClubOverlay.LocalData[1] is null");
                return true;
            }

            if (!ServerNameOverlay.IsEnabled)
                data.Content = string.Empty;

            data.VerticalOffset = ServerNameOverlay.VerticalOffset;

            ServerNameOverlay = null;
            return true;
        }
    }
}
