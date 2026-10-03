using LabExtended.API;
using LabExtended.API.Toys;

using LabExtended.Core;
using LabExtended.Extensions;
using LabExtended.Utilities.Update;

using ProjectMER.Features.Objects;

using AdminToys;

using ObscurisCore.Features.Audio.Clips;

using ObscurisCore.Features.RandomPickup.Enums;
using ObscurisCore.Features.RandomPickup.Configs;

using ObscurisCore.Utilities.Interactions;

using UnityEngine;

namespace ObscurisCore.Features.RandomPickup
{
    /// <summary>
    /// Represents a spawned instance of a random pickup.
    /// </summary>
    public class RandomPickupInstance : InteractableObject
    {
        private LightToy light;
        private SchematicObject schematic;

        private ClipManager<RandomPickupClip> clips;

        private float spawnTime;
        private float initialY = 0f;

        private bool isUpdating;
        private bool isSpawning;
        private bool isDespawning;

        /// <summary>
        /// Gets the ID of this instance.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets or sets the configuration properties for the random pickup instance.
        /// </summary>
        public RandomPickupProperties Properties
        {
            get
            {
                return field;
            }
            set
            {
                if (value is null)
                    return;

                field = value;

                SetProperties(value);
            }
        }

        /// <summary>
        /// Gets or sets the position of the schematic in world space.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (schematic != null)
                    return schematic.Position;

                return Vector3.zero;
            }
            set
            {
                if (schematic != null)
                    schematic.Position = value;
            }
        }

        /// <summary>
        /// Gets or sets the rotation of the schematic as a quaternion.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                if (schematic != null)
                    return schematic.Rotation;

                return Quaternion.identity;
            }
            set
            {
                if (schematic != null)
                    schematic.Rotation = value;
            }
        }

        /// <summary>
        /// Gets the estimated number of seconds remaining before the pickup is despawned.
        /// </summary>
        /// <remarks>If the despawn time is not set or has already elapsed, the property returns <see
        /// cref="float.MaxValue"/>.</remarks>
        public float RemainingSeconds
        {
            get
            {
                if (Properties is null)
                    return float.MaxValue;

                if (Properties.SecondsUntilDespawn <= 0f)
                    return float.MaxValue;

                var spawnedFor = Time.realtimeSinceStartup - spawnTime;
                var remaining = Properties.SecondsUntilDespawn - spawnedFor;

                if (remaining < 0f)
                    remaining = 0f;

                return remaining;
            }
        }

        /// <summary>
        /// Initializes a new instance of the RandomPickupInstance class using the specified schematic object.
        /// </summary>
        /// <param name="schematic">The schematic object that defines the initial state and position for the pickup instance. Cannot be null.</param>
        public RandomPickupInstance(SchematicObject schematic)
        {
            this.schematic = schematic;
        }

        /// <summary>
        /// Handles player interaction with the random pickup, awarding experience and triggering loot actions as
        /// appropriate.
        /// </summary>
        /// <param name="player">The player who interacted with the random pickup. Cannot be null.</param>
        public override void OnInteracted(ExPlayer player)
        {
            base.OnInteracted(player);

            if (isDespawning)
                return;

            if (clips.PlayRandomClip(RandomPickupClip.Opened))
            {
                isSpawning = false;
                isDespawning = true;

                DestroyInteractable();

                if (light?.Base != null)
                {
                    light.Delete();
                    light = null!;
                }

                if (schematic != null)
                {
                    schematic.Destroy();
                    schematic = null!;
                }
            }
            else
            {
                Destroy();
            }
        }

        /// <summary>
        /// Releases all resources used by the instance.
        /// </summary>
        public void Destroy()
        {
            if (isUpdating)
                PlayerUpdateHelper.OnUpdate -= Update;

            isUpdating = false;
            isSpawning = false;
            isDespawning = false;

            DestroyInteractable();

            if (light?.Base != null)
            {
                light.Delete();
                light = null!;
            }

            if (clips != null)
            {
                clips.ClipEnded -= OnClipEnded;
                clips.Destroy();
                clips = null!;
            }

            if (schematic != null)
            {
                schematic.Destroy();
                schematic = null!;
            }

            if (Id != 0)
            {
                RandomPickupManager.instances.Remove(Id);

                ApiLog.Debug("RandomPickup", $"Destroyed RandomPickupInstance with ID &3{Id}&r");
            }

            Id = 0;
        }

        private void Update()
        {
            if (schematic == null 
                || Properties == null
                || isDespawning)
                return;

            if (RemainingSeconds <= 0f) 
            {
                Destroy();
                return;
            }

            if (Properties.FloatPickup)
                Float();

            if (Properties.RotatePickup)
                Rotate();
        }

        private void Rotate()
        {
            schematic.Rotation *=
                Quaternion.Inverse(schematic.Rotation)
                * Quaternion.Euler(0f, Properties.RotationAngle * Time.deltaTime, 0f)
                * schematic.Rotation;
        }

        private void Float()
        {
            var pos = schematic.Position;

            pos.y = initialY + Mathf.Sin(Time.time * Properties.FloatSpeed) * Properties.FloatAmplitude;

            schematic.Position = pos;
        }

        private void SetProperties(RandomPickupProperties value)
        {
            if (schematic == null)
                return;

            if (Id != 0)
                RandomPickupManager.instances[Id] = this;

            if (value.SpawnLight)
            {
                if (light?.Base == null)
                {
                    light = new(Position, Rotation);
                    light.Transform.parent = schematic.transform;
                }

                light.Range = value.LightRange;
                light.Color = value.LightColor.Color.FixPrimitiveColor();
                light.Intensity = value.LightIntensity;
            }
            else
            {
                if (light?.Base != null)
                {
                    light.Delete();
                    light = null!;
                }
            }

            if (clips != null)
            {
                clips.Config = value.Clips;
                clips.ClipTimes.Clear();
            }
            else
            {
                clips = new(value.Clips, schematic.transform);
                clips.ClipEnded += OnClipEnded;
            }

            InteractableShape = InvisibleInteractableToy.ColliderShape.Box;
            InteractableScale = Vector3.one;
            InteractionDuration = 0.5f;

            DestroyInteractable();
            SpawnInteractable(Position, Rotation, schematic.transform);

            spawnTime = Time.realtimeSinceStartup;
            initialY = Position.y;

            if (!isUpdating)
            {
                PlayerUpdateHelper.OnUpdate += Update;

                isUpdating = true;
            }

            if (clips.PlayRandomClip(RandomPickupClip.Spawned))
            {
                isSpawning = true;
            }
            else
            {
                clips.PlayRandomClip(RandomPickupClip.Waiting);
            }
        }

        private void OnClipEnded()
        {
            if (isSpawning)
            {
                isSpawning = false;

                clips.PlayRandomClip(RandomPickupClip.Waiting);
                return;
            }

            if (isDespawning)
            {
                isDespawning = false;

                Destroy();
            }
        }
    }
}