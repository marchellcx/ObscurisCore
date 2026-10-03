using LabExtended.API;
using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Extensions;

using LabExtended.Utilities;
using LabExtended.Utilities.Update;

using ProjectMER.Features;
using System.Collections.ObjectModel;

using NiveraAPI.IO.Configs;
using ObscurisCore.Features.RandomPickup.Configs;
using UnityEngine;

namespace ObscurisCore.Features.RandomPickup
{
    /// <summary>
    /// Manages spawning of random pickup instances.
    /// </summary>
    public static class RandomPickupManager
    {
        private static float nextCheck = 0f;

        private static int instanceId = 0;
       
        private static List<string> history = new();
        internal static Dictionary<int, RandomPickupInstance> instances = new();

        /// <summary>
        /// Gets the active config.
        /// </summary>
        [Config("randomPickup", "config", "Random pickup configuration.")]
        public static RandomPickupConfig Config { get; set; } = new();

        /// <summary>
        /// Gets a dictionary of all spawned instances.
        /// </summary>
        public static ReadOnlyDictionary<int, RandomPickupInstance> Instances => field ??= new(instances);

        /// <summary>
        /// Spawns a new random pickup instance at the specified position and rotation using the given schematic and
        /// properties.
        /// </summary>
        /// <param name="schematic">The name of the schematic to use for spawning the pickup. Cannot be null or empty.</param>
        /// <param name="position">The world position where the pickup will be spawned.</param>
        /// <param name="rotation">The rotation to apply to the spawned pickup.</param>
        /// <param name="properties">The properties that define the behavior and appearance of the random pickup. Cannot be null.</param>
        /// <returns>A RandomPickupInstance representing the newly spawned pickup.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schematic"/>, <paramref name="properties"/> is
        /// null or, in the case of <paramref name="schematic"/>, empty.</exception>
        /// <exception cref="Exception">Thrown if the schematic cannot be spawned at the specified position and rotation.</exception>
        public static RandomPickupInstance Spawn(string schematic, Vector3 position, Quaternion rotation, RandomPickupProperties properties)
        {
            if (string.IsNullOrEmpty(schematic))
                throw new ArgumentNullException(nameof(schematic));

            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            if (!ObjectSpawner.TrySpawnSchematic(schematic, position, rotation, Vector3.one, out var instance)
                || instance == null)
                throw new Exception($"Schematic '{schematic}' could not be spawned");

            instance.Position = position;
            instance.Rotation = rotation;

            var id = instanceId++;
            var pickup = new RandomPickupInstance(instance);

            pickup.Id = id;
            pickup.Properties = properties; // Setting this initializes the component

            return pickup;
        }

        private static void Initialize()
        {
            ExRoundEvents.Started += OnStarted;
            ExRoundEvents.Restarting += OnRestarting;

            if (Config.PlayerWeight > 0f)
                PlayerUpdateHelper.OnLateUpdate += Update;
        }

        private static void OnStarted()
        {
            /*
            foreach (var pair in Config.Spawns)
            {
                if (pair.Value <= 0f)
                    continue;

                if (pair.Value < 100f && !WeightUtils.GetBool(pair.Value))
                    continue;

                if (!MapUtilities.TryGet(pair.Key, null, out var position, out var rotation))
                    continue;

                var properties = Config.GlobalProperties;

                if (Config.Properties.TryGetValue(pair.Key, out var specificProps))
                    properties = specificProps;

                var instance = Spawn(properties.Schematic, position, rotation, properties);

                if (instance != null)
                    ApiLog.Info("RandomPickup", $"Spawned a random pickup instance at &3{pair.Key}&r");
                else
                    ApiLog.Warn("RandomPickup", $"Could not spawn a random pickup instance at &3{pair.Key}&r!");
            }
            */
        }

        private static void OnRestarting()
        {
            foreach (var pair in instances.ToDictionary())
                pair.Value.Destroy();

            instances.Clear();
            instanceId = 0;
        }

        private static void Update()
        {
            if (!ExRound.IsRunning || ExPlayer.Count < 1)
                return;

            if (Config.PlayerStartDelay > 0 && ExRound.Duration.TotalSeconds < Config.PlayerStartDelay)
                return;

            if (Config.PlayerDelay > 0f)
            {
                if (Time.realtimeSinceStartup < nextCheck)
                    return;

                nextCheck = Time.realtimeSinceStartup + Config.PlayerDelay;
            }

            var player = ExPlayer.Players.GetRandomWeighted(player =>
            {
                if (player?.ReferenceHub == null)
                    return 0f;

                if (!player.Role.IsAlive || player.Role.IsTutorial)
                    return 0f;

                var weight = Config.PlayerWeight;
                var count = history.Count(x => x == player.UserId);

                if (count > 0)
                    weight /= count / 2;

                return weight;
            });

            if (player?.ReferenceHub == null)
                return;

            history.Add(player.UserId);

            var properties = Config.GlobalProperties;

            if (Config.Properties.TryGetValue(player.UserId, out var specificProps)
                || player.PermissionsGroupName != null && Config.Properties.TryGetValue(player.PermissionsGroupName, out specificProps)
                || Config.Properties.TryGetValue("PlayerSpawn", out specificProps))
                properties = specificProps;

            var instance = Spawn(properties.Schematic, player.Position, player.Rotation, properties);

            if (instance != null)
                ApiLog.Info("RandomPickup", $"Spawned a random pickup instance at player {player.ToLogString()}");
            else
                ApiLog.Warn("RandomPickup", $"Could not spawn a random pickup instance at player {player.ToLogString()}!");
        }
    }
}