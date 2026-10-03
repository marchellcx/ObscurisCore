using LabExtended.API;
using LabExtended.API.Settings;
using LabExtended.API.Settings.Entries;
using LabExtended.API.Settings.Entries.Buttons;
using LabExtended.API.Settings.Menus;
using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons.Grab
{
    /// <summary>
    /// Represents a settings menu that provides controls for manipulating grabbed items, including rotation, throwing,
    /// and release actions.
    /// </summary>
    /// <remarks>The GrabMenu exposes UI elements such as sliders, buttons, and key bindings to allow users to
    /// interact with items they have grabbed. It extends the SettingsMenu to provide specialized controls for item
    /// manipulation within the settings interface.</remarks>
    public class GrabMenu : SettingsMenu
    {
        /// <inheritdoc/>
        public override string CustomId { get; } = "secretlabapi.grabmenu";

        /// <inheritdoc/>
        public override string Header { get; } = "<color=yellow> ✋ | Nastavení Grabu</color>";

        /// <summary>
        /// Gets the slider control used to adjust the scale setting.
        /// </summary>
        public SettingsSlider ScaleSlider { get; private set; }

        /// <summary>
        /// Gets the slider control used to adjust the distance setting.
        /// </summary>
        public SettingsSlider DistanceSlider { get; private set; }

        /// <summary>
        /// Gets the slider control used to adjust the rotation angle.
        /// </summary>
        public SettingsSlider RotateSlider { get; private set; }

        /// <summary>
        /// Gets the key binding used to trigger the release-in-place action.
        /// </summary>
        public SettingsKeyBind ReleaseInPlaceKeybind { get; private set; }

        /// <summary>
        /// Gets the key binding used to trigger the release-to-position action.
        /// </summary>
        public SettingsKeyBind ReleaseToPositionKeybind { get; private set; }

        /// <summary>
        /// Gets the key binding used to launch the grabbed item.
        /// </summary>
        public SettingsKeyBind LaunchKeybind { get; private set; }

        /// <summary>
        /// Gets the slider control used to adjust the launch duration setting.
        /// </summary>
        public SettingsSlider LaunchDurationSlider { get; private set; }

        /// <summary>
        /// Gets the slider control used to adjust the launch speed setting.
        /// </summary>
        public SettingsSlider LaunchSpeedSlider { get; private set; }

        /// <summary>
        /// Gets the slider control used to adjust the launch gravity setting.
        /// </summary>
        public SettingsSlider LaunchGravitySlider { get; private set; }

        /// <summary>
        /// Gets the rotation settings for the two-button configuration.
        /// </summary>
        public SettingsTwoButtons RotateSetting { get; private set; }

        /// <summary>
        /// Custom scale applied to the grabbed item.
        /// </summary>
        public Vector3 Scale { get; private set; } = Vector3.one;

        /// <summary>
        /// Gets the current angle value, in degrees.
        /// </summary>
        public float Angle => RotateSlider.Value;

        /// <summary>
        /// Gets the distance.
        /// </summary>
        public float Distance => DistanceSlider.Value;

        /// <summary>
        /// Gets the launch speed value for launching grabbed objects.
        /// </summary>
        public float LaunchSpeed => LaunchSpeedSlider.Value;

        /// <summary>
        /// Gets the launch duration value for launching grabbed objects.
        /// </summary>
        public float LaunchDuration => LaunchDurationSlider.Value;

        /// <summary>
        /// Gets the launch gravity value for launching grabbed objects.
        /// </summary>
        public float LaunchGravity => LaunchGravitySlider.Value;

        /// <inheritdoc/>
        public override void BuildMenu(List<SettingsEntry> settings)
        {
            settings.WithEntries(new SettingsEntry[]
            {
                DistanceSlider = new("secretlabapi.grabmenu.distanceslider", "📏 | Vzdálenost", 1f, 30f, 5f, true, "0.##", "{0}m", "Určuje jak daleko je držený předmět."),

                RotateSlider = new("secretlabapi.grabmenu.rotateslider", "📐 | Úhel", -360f, 360f, 0f, true, "0.##", "{0}°",
                    "Tento slider otočí aktuálně držený předmět o určitý úhel.") { ShouldSyncDrag = false },

                ScaleSlider = new("secretlabapi.grabmenu.scaleslider", "⚖️ | Měřítko", -50f, 50f, 1f, true, "0.##", "{0}x",
                    "Umožňuje změnit velikost aktuálně drženého předmětu.") { ShouldSyncDrag = false },

                RotateSetting = new("secretlabapi.grabmenu.rotatesetting", "🔄 | Otáčet předmět", "<color=green>Ano</color>", "<color=red>Ne</color>", true,
                    "Měl by se předmět otáčet?"),

                ReleaseInPlaceKeybind = new("secretlabapi.grabmenu.releaseinplacekeybind", "<color=red>❌ | Zrušení grabu</color> <i>(zůstane na aktuální pozici)</i>",
                    KeyCode.X, true, false, "Klávesa pro zrušení grabu (předmět zůstane na aktuální pozici)."),

                ReleaseToPositionKeybind = new("secretlabapi.grabmenu.releasetopositionkeybind", "<color=red>❌ | Zrušení grabu</color> <i>(vrátí se na původní pozici)</i>",
                    KeyCode.B, true, false, "Klávesa pro zrušení grabu (předmět se vrátí na původní pozici)."),

                LaunchKeybind = new("secretlabapi.grabmenu.launchkeybind", "🚀 | Vystřelit předmět",
                    KeyCode.L, true, false, "Klávesa pro vystřelení aktuálně drženého předmětu."),

                LaunchGravitySlider = new("secretlabapi.grabmenu.launchgravityslider", "🌐 | Gravitace při vystřelení", 0f, 20f, 9.81f, false, "0.##", "{0}m/s²",
                    "Simulovaná gravitace vystřeleného předmětu."),

                LaunchSpeedSlider = new("secretlabapi.grabmenu.launchspeedslider", "💨 | Rychlost při vystřelení", 1f, 100f, 10f, true, "0.##", "{0}m/s",
                    "Rychlost, jakou bude předmět vystřelen."),

                LaunchDurationSlider = new("secretlabapi.grabmenu.launchdurationslider", "⏱️ | Doba letu při vystřelení", 1f, 20f, 5f, true, "0.##", "{0}s",
                    "Doba, po kterou bude předmět v letu (v sekundách)."),
            });
        }

        /// <inheritdoc/>
        public override void OnKeyBindPressed(SettingsKeyBind keyBind)
        {
            base.OnKeyBindPressed(keyBind);

            if (Player?.ReferenceHub == null)
            {
                ApiLog.Warn("GrabMenu", "Player or Player.ReferenceHub is null in OnKeyBindPressed.");
                return;
            }

            if (!Player.HasCustomItem<GrabGun>(out var grabItem, out var grabGun))
            {
                Player.SendConsoleMessage($"[GRAB] You don't have an active grab gun!");
                return;
            }         

            if (keyBind == ReleaseInPlaceKeybind)
            {
                grabGun.Release(grabItem, false);
            }
            else if (keyBind == ReleaseToPositionKeybind)
            {
                grabGun.Release(grabItem, true);
            }
            else if (keyBind == LaunchKeybind)
            {
                grabGun.Launch(grabItem, LaunchSpeed, LaunchDuration, LaunchGravity);
            }
        }

        /// <inheritdoc/>
        public override void OnSliderMoved(SettingsSlider slider)
        {
            base.OnSliderMoved(slider);

            if (Player?.ReferenceHub == null)
            {
                ApiLog.Warn("GrabMenu", "Player or Player.ReferenceHub is null in OnKeyBindPressed.");
                return;
            }

            if (!Player.HasCustomItem<GrabGun>(out var gunItem, out var grabGun))
            {
                Player.SendConsoleMessage($"[GRAB] You don't have an active grab gun!");
                return;
            }

            if (slider == ScaleSlider)
            {
                var newScale = new Vector3(ScaleSlider.Value, ScaleSlider.Value, ScaleSlider.Value);

                if (newScale == Scale)
                    return;

                Scale = newScale;

                grabGun.UpdateScale(gunItem, Scale);

                Player.SendConsoleMessage($"[GRAB] Updated scale: {newScale.ToPreciseString()}", "green");
            }
        }

        private static void OnVerified(ExPlayer player)
        {
            if (player.TryGetMenu<GrabMenu>(out var grabMenu))
            {
                TimingUtils.AfterSeconds(() => grabMenu.HideMenu(), 1f);
            }
        }

        internal static void Initialize()
        {
            ExPlayerEvents.Verified += OnVerified;
        }
    }
}