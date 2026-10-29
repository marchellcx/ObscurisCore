using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;
using LabExtended.Core;
using LabExtended.Extensions;

using LabExtended.Events;
using LabExtended.Events.Player;

using Mirror;
using NiveraAPI.IO.Configs;
using PlayerStatsSystem;

using ProjectMER.Features;
using ProjectMER.Features.Objects;

using ObscurisCore.Extensions;
using ObscurisCore.Features.SchematicModels.Configs;

using UnityEngine;
using UnityExtensions = ObscurisCore.Extensions.UnityExtensions;

namespace ObscurisCore.Features.SchematicModels;

/// <summary>
/// Provides methods for managing and replacing schematic models associated with players.
/// </summary>
public static class SchematicModelReplacer
{
    private static readonly SchematicModelConfig defaultConfig = new();
    private static readonly Dictionary<ExPlayer, SchematicModelInfo> models = new();
    
    /// <summary>
    /// Gets or sets a list of custom schematic model configurations.
    /// </summary>
    [Config("schematicModels", "configs", "List of custom schematic model configurations.")]
    public static Dictionary<string, SchematicModelConfig> ModelConfigs { get; set; } = new()
    {
        ["default"] = new()
    };

    /// <summary>
    /// Provides a read-only dictionary containing the mapping between players and their respective schematic models.
    /// </summary>
    /// <remarks>
    /// The dictionary key represents an <typeparamref name="ExPlayer"/> object, which identifies the player.
    /// The value is a <see cref="SchematicModelInfo"/> object that contains information about the associated schematic model.
    /// </remarks>
    public static IReadOnlyDictionary<ExPlayer, SchematicModelInfo> Models => models;

    /// <summary>
    /// Removes the schematic model associated with the specified player.
    /// </summary>
    /// <param name="player">The player whose associated schematic model will be removed.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the specified player is null.
    /// </exception>
    public static void Remove(ExPlayer player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
        
        if (models.TryGetValue(player, out var model))
            model.Stop(true);
        
        models.Remove(player);
    }
    
    /// <summary>
    /// Replaces the current schematic model of the specified player with a new one.
    /// </summary>
    /// <param name="player">The player whose schematic model will be replaced.</param>
    /// <param name="name">The name of the schematic model to spawn.</param>
    /// <param name="removeOnRoleChange">
    /// Indicates whether the schematic model should be removed when the player's role changes.
    /// </param>
    /// <returns>
    /// true if the schematic model was successfully spawned and replaced; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the specified player is not valid.
    /// </exception>
    public static bool Replace(ExPlayer player, string name, bool removeOnRoleChange = false)
    {
        if (!player.IsValidPlayer())
            throw new ArgumentException("Player is not valid", nameof(player));

        if (!ModelConfigs.TryGetValue(name, out var config))
            config = defaultConfig;

        if (!ObjectSpawner.TrySpawnSchematic(name, player.Position, player.Rotation.Rotation, Vector3.one, out var schematicObject))
        {
            ApiLog.Warn($"Could not spawn schematic: &1{name}&r");
            return false;
        }
        
        Replace(player, schematicObject, config, removeOnRoleChange);
        return true;
    }

    /// <summary>
    /// Replaces the current schematic model of the specified player with the provided schematic model.
    /// </summary>
    /// <param name="player">The player whose schematic model will be replaced.</param>
    /// <param name="schematic">The new schematic object to associate with the player.</param>
    /// <param name="config">The configuration settings to apply to the schematic model.</param>
    /// <param name="removeOnRoleChange">
    /// Indicates whether the schematic model should be removed when the player's role changes.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the specified player is not valid.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the schematic or config parameter is null.
    /// </exception>
    public static void Replace(ExPlayer player, SchematicObject schematic, SchematicModelConfig config, bool removeOnRoleChange)
    {
        if (!player.IsValidPlayer())
            throw new ArgumentException("Player is not valid", nameof(player));

        if (schematic == null)
            throw new ArgumentNullException(nameof(schematic));
        
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        if (models.TryGetValue(player, out var model))
            model.Stop(true);

        if (config.Scale.X != 0f
            && config.Scale.Y != 0f
            && config.Scale.Z != 0f)
            schematic.Scale = config.Scale.Vector;

        model = new(player, schematic, config);
        model.RemoveOnRoleChange = removeOnRoleChange;
        
        models.Add(player, model);
        
        model.Start();
    }

    private static void OnShooting(PlayerShootingFirearmEventArgs args)
    {
        if (models.Count < 1)
            return;
        
        if (args.TargetPlayer?.ReferenceHub != null)
        {
            ApiLog.Debug("TargetPlayer != null");
            return;
        }

        if (args.Hit.collider == null || args.Hit.collider.gameObject == null)
        {
            ApiLog.Debug("Collider == null");
            return;
        }

        if (UnityExtensions.TryFindComponent<SchematicObject>(args.Hit.collider.gameObject, out var schematicObject))
        {
            ApiLog.Debug($"Found SchematicObject on &1{args.Hit.collider.gameObject.name}&r: {schematicObject.Name}");

            var pair = models.FirstOrDefault(kvp => kvp.Value.Schematic != null && kvp.Value.Schematic == schematicObject);

            if (pair.Key?.ReferenceHub == null)
            {
                ApiLog.Debug($"Could not find model owner");
                return;
            }
            
            ApiLog.Debug($"Found model owner: {pair.Key.ToLogString()} (Damage: {args.TargetDamage})");

            pair.Key.ReferenceHub.playerStats.DealDamage(new FirearmDamageHandler(args.Firearm, args.TargetDamage, 0f, true));
            
            args.Player.SendHitMarker(1f);
        }
        else if (UnityExtensions.TryFindComponent<NetworkIdentity>(args.Hit.collider.gameObject, out var identity))
        {
            ApiLog.Debug($"Found NetworkIdentity on &1{args.Hit.collider.gameObject.name}&r: {identity.netId} {identity.name}");
            
            var pair = models.FirstOrDefault(kvp => kvp.Value.Schematic != null && kvp.Value.Schematic.NetworkIdentities.Contains(identity));

            if (pair.Key?.ReferenceHub == null)
            {
                ApiLog.Debug($"Could not find model owner");
                return;
            }
            
            ApiLog.Debug($"Found model owner: {pair.Key.ToLogString()} (Damage: {args.TargetDamage})");
            
            pair.Key.ReferenceHub.playerStats.DealDamage(new FirearmDamageHandler(args.Firearm, args.TargetDamage, 0f, false));
            
            args.Player.SendHitMarker(1f);
        }
        else
        {
            ApiLog.Debug($"Could not find SchematicObject or NetworkIdentity on target: &1{args.Hit.collider.name}&r");
        }
    }

    private static void OnDied(PlayerDeathEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (!models.TryGetValue(player, out var model))
            return;
        
        model.Stop(true);

        models.Remove(player);
    }

    private static void OnChangedRole(PlayerChangedRoleEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        if (!models.TryGetValue(player, out var model))
            return;

        if (!model.RemoveOnRoleChange)
            return;
        
        model.Stop(true);

        models.Remove(player);
    }
    
    private static void OnLeft(ExPlayer player)
    {
        if (models.TryGetValue(player, out var model))
            model.Stop(true);
        
        models.Remove(player);
    }

    private static void OnRestarting()
    {
        foreach (var pair in models)
            pair.Value.Stop(false);
        
        models.Clear();
    }

    private static void Initialize()
    {
        PlayerEvents.Death += OnDied;
        PlayerEvents.ChangedRole += OnChangedRole;
        
        ExPlayerEvents.Left += OnLeft;
        ExPlayerEvents.ShootingFirearm += OnShooting;
        
        ExRoundEvents.Restarting += OnRestarting;
    }
}