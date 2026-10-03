using LabExtended.Core.Configs.Objects;

using System.ComponentModel;

using ObscurisCore.Features.Audio.Clips;
using ObscurisCore.Features.RandomPickup.Enums;

using UnityEngine;

namespace ObscurisCore.Features.RandomPickup.Configs
{
    /// <summary>
    /// Properties of a spawned random pickup instance.
    /// </summary>
    public class RandomPickupProperties
    {
        /// <summary>
        /// Gets or sets the name of the MER schematic to use.
        /// </summary>
        [Description("Sets the name of the MER schematic to use.")]
        public string Schematic { get; set; } = "RandomPickup";

        /// <summary>
        /// Gets or sets the number of seconds that the instance remains active before it is despawned.
        /// </summary>
        [Description("Sets the amount of seconds the instance will be spawned for.")]
        public float SecondsUntilDespawn { get; set; } = 300f;

        /// <summary>
        /// Gets or sets a value indicating whether a light toy should be spawned with the pickup.
        /// </summary>
        [Description("Whether or not a light toy should be spawned with the pickup.")]
        public bool SpawnLight { get; set; } = true;

        /// <summary>
        /// Gets or sets the color of the light toy.
        /// </summary>
        [Description("Sets the color of the light toy.")]
        public YamlColor LightColor { get; set; } = new(Color.red);

        /// <summary>
        /// Gets or sets the intensity of the light toy.
        /// </summary>
        [Description("Sets the intensity of the light toy.")]
        public float LightIntensity { get; set; } = 0.6f;

        /// <summary>
        /// Gets or sets the range of the light toy.
        /// </summary>
        [Description("Sets the range of the light toy.")]
        public float LightRange { get; set; } = 1.5f;

        /// <summary>
        /// Gets or sets a value indicating whether the pickup should float up and down.
        /// </summary>
        [Description("Whether or not the pickup should float up and down.")]
        public bool FloatPickup { get; set; } = true;

        /// <summary>
        /// Gets or sets the speed at which the pickup floats up and down.
        /// </summary>
        [Description("Sets the speed at which the pickup floats up and down.")]
        public float FloatSpeed { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the amplitude value as a floating-point number.
        /// </summary>
        [Description("Sets the float amplitude value.")]
        public float FloatAmplitude { get; set; } = 0.25f;

        /// <summary>
        /// Gets or sets a value indicating whether the pickup should rotate.
        /// </summary>
        [Description("Whether or not the pickup should rotate.")]
        public bool RotatePickup { get; set; } = true;

        /// <summary>
        /// Gets or sets the rotation angle of the pickup, in degrees.
        /// </summary>
        [Description("Sets the rotation angle of the pickup.")]
        public float RotationAngle { get; set; } = 45f;

        /// <summary>
        /// Gets or sets the collection of clip definitions organized by clip type.
        /// </summary>
        [Description("A list of clip definitions defined per each clip type.")]
        public ClipConfig<RandomPickupClip> Clips { get; set; } = new ClipConfig<RandomPickupClip>()
        {
            Clips = new()
            {
                { RandomPickupClip.Spawned, new() { new() } },
                { RandomPickupClip.Opened, new() { new() } },
                { RandomPickupClip.Waiting, new() { new() } }
            },

            Cooldowns = new()
            {
                { RandomPickupClip.Spawned, 0f },
                { RandomPickupClip.Opened, 0f },
                { RandomPickupClip.Waiting, 5f }
            }
        };
    }
}