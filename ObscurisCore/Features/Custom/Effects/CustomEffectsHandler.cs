using LabExtended.API;
using LabExtended.API.Custom.Effects;

using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;
using LabExtended.Extensions;
using ObscurisCore.Features.Custom.Effects.Misc;

namespace ObscurisCore.Features.Custom.Effects
{
    /// <summary>
    /// Provides methods for registering and managing custom player effects within the system.
    /// </summary>
    /// <remarks>Use this class to add new custom player effects so they can be recognized and applied to
    /// players. All custom effects should be registered before player verification to ensure proper initialization.
    /// This class is intended for advanced scenarios where extending player effect functionality is required.</remarks>
    public static class CustomEffectsHandler
    {
        private static readonly List<Type> effects = new();

        /// <summary>
        /// Registers a custom player effect of the specified type so that it can be recognized and managed by the
        /// system.
        /// </summary>
        /// <remarks>Call this method to make a new custom player effect available for use. Effects should
        /// be registered before they are applied to players. If the effect type has already been registered, this
        /// method has no effect.</remarks>
        /// <typeparam name="T">The type of custom player effect to register. Must inherit from <see cref="CustomPlayerEffect"/>.</typeparam>
        public static void AddEffect<T>() where T : CustomPlayerEffect
        {
            if (effects.AddUnique(typeof(T)))
            {
                CustomPlayerEffect.Effects.Add(typeof(T));
            }
        }

        private static void OnJoined(ExPlayer player)
        {
            foreach (var effect in effects)
            {
                var name = effect.Name.CamelCase();
                var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "effects", name + ".yml");
                
                if (!FileUtils.TryLoadYamlFile<CustomPlayerEffect>(path, effect, out var customEffect))
                {
                    if ((customEffect = Activator.CreateInstance(effect) as CustomPlayerEffect) == null)
                    {
                        ApiLog.Error("ObscurisCore", $"Failed to create an effect instance: {effect.FullName}");
                        continue;
                    }

                    FileUtils.TrySaveYamlFile(path, customEffect);
                }

                player.Effects.AddCustomEffect(customEffect);
            }
        }

        internal static void Initialize()
        {
            AddEffect<RocketEffect>();
            AddEffect<DoorInteractExplosionEffect>();

            ExPlayerEvents.Joined += OnJoined;
        }
    }
}
