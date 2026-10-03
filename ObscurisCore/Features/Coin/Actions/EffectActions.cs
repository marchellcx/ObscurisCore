using System.ComponentModel;

using CustomPlayerEffects;

using LabExtended.API;
using LabExtended.Core;
using LabExtended.Utilities;

using NiveraAPI.Extensions;

using NorthwoodLib.Pools;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Coin.Actions;

/// <summary>
/// Represents an action that can be performed by flipping a coin to enable or disable effects on a player.
/// </summary>
public class EffectActions : CoinAction
{
    /// <summary>
    /// The type of action to perform.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Clears all effects from the player.
        /// </summary>
        ClearAllEffects,
        
        /// <summary>
        /// Clears all mixed effects from the player.
        /// </summary>
        ClearMixedEffects,
        
        /// <summary>
        /// Clears all positive effects from the player.
        /// </summary>
        ClearPositiveEffects,
        
        /// <summary>
        /// Clears all negative effects from the player.
        /// </summary>
        ClearNegativeEffects,
        
        /// <summary>
        /// Enables a random mixed effect.
        /// </summary>
        EnableRandomMixedEffect,
        
        /// <summary>
        /// Enables a random positive effect.
        /// </summary>
        EnableRandomPositiveEffect,
        
        /// <summary>
        /// Enables a random negative effect.
        /// </summary>
        EnableRandomNegativeEffect,
        
        /// <summary>
        /// Disables a random mixed effect.
        /// </summary>
        DisableRandomMixedEffect,
        
        /// <summary>
        /// Disables a random positive effect.
        /// </summary>
        DisableRandomPositiveEffect,
        
        /// <summary>
        /// Disables a random negative effect.
        /// </summary>
        DisableRandomNegativeEffect,
    }

    /// <summary>
    /// The properties of the effect to enable.
    /// </summary>
    public class EffectProperties
    {
        /// <summary>
        /// The type of effect to enable.
        /// </summary>
        [Description("The duration of the effect.")]
        public float Duration { get; set; } = 0f;
        
        /// <summary>
        /// The intensity of the effect.
        /// </summary>
        [Description("The intensity of the effect.")]
        public byte Intensity { get; set; } = 0;
    }

    /// <summary>
    /// Whether the actions can be combined.
    /// </summary>
    public override bool CanBeCombined => true;

    /// <summary>
    /// The weights of the actions.
    /// </summary>
    [Description("Sets the weights of the actions.")]
    public Dictionary<ActionType, float> Weights { get; set; } = new()
    {
        { ActionType.ClearAllEffects, 0f },
        { ActionType.ClearMixedEffects, 0f },
        { ActionType.ClearPositiveEffects, 0f },
        { ActionType.ClearNegativeEffects, 0f },

        { ActionType.EnableRandomMixedEffect, 0f },
        { ActionType.EnableRandomPositiveEffect, 0f },
        { ActionType.EnableRandomNegativeEffect, 0f },

        { ActionType.DisableRandomMixedEffect, 0f },
        { ActionType.DisableRandomPositiveEffect, 0f },
        { ActionType.DisableRandomNegativeEffect, 0f },
    };

    /// <summary>
    /// The messages to be sent to players when they perform the actions.
    /// </summary>
    [Description("Sets the messages to be sent to players when they perform the actions.")]
    public Dictionary<ActionType, string> Messages { get; set; } = new()
    {
        { ActionType.ClearAllEffects, "" },
        { ActionType.ClearMixedEffects, "" },
        { ActionType.ClearPositiveEffects, "" },
        { ActionType.ClearNegativeEffects, "" },

        { ActionType.EnableRandomMixedEffect, "" },
        { ActionType.EnableRandomPositiveEffect, "" },
        { ActionType.EnableRandomNegativeEffect, "" },

        { ActionType.DisableRandomMixedEffect, "" },
        { ActionType.DisableRandomPositiveEffect, "" },
        { ActionType.DisableRandomNegativeEffect, "" },
    };

    /// <summary>
    /// The effects to ignore when performing the action.
    /// </summary>
    [Description("The effects to ignore when performing the action.")]
    public List<string> IgnoreEffects { get; set; } = new()
    {
        "PocketCorroding",
        "Scp1344"
    };

    /// <summary>
    /// Per-effect configurations.
    /// </summary>
    [Description("Per-effect configurations.")]
    public Dictionary<string, EffectProperties> EffectConfigs { get; set; } = new()
    {
        ["example"] = new()
    };

    private Dictionary<ExPlayer, ActionType> actions = new();

    /// <summary>
    /// Determines if the current action is available for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the availability is being checked.</param>
    /// <returns>True if the action is available for the specified player, otherwise false.</returns>
    public override bool IsAvailable(ExPlayer player)
    {
        if (!base.IsAvailable(player))
            return false;
        
        var list = ListPool<ActionType>.Shared.Rent();

        actions.Remove(player);

        foreach (var kvp in Weights)
        {
            if (kvp.Value <= 0f)
                continue;

            var available = false;

            switch (kvp.Key)
            {
                case ActionType.ClearAllEffects:
                    available = player.Effects.ActiveEffects.Any(e => !IgnoreEffects.Contains(e.GetType().Name));
                    break;
                
                case ActionType.ClearMixedEffects:
                    available = player.Effects.ActiveEffects.Any(e =>
                        e.Classification is StatusEffectBase.EffectClassification.Mixed
                        && !IgnoreEffects.Contains(e.GetType().Name));
                    break;
                
                case ActionType.ClearNegativeEffects:
                    available = player.Effects.ActiveEffects.Any(e =>
                        e.Classification is StatusEffectBase.EffectClassification.Negative
                        && !IgnoreEffects.Contains(e.GetType().Name));
                    break;
                
                case ActionType.ClearPositiveEffects:
                    available = player.Effects.ActiveEffects.Any(e =>
                        e.Classification is StatusEffectBase.EffectClassification.Positive
                        && !IgnoreEffects.Contains(e.GetType().Name));
                    break;
                
                case ActionType.EnableRandomMixedEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Mixed 
                                                                && !e.Value.IsEnabled);
                    break;
                
                case ActionType.EnableRandomNegativeEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Negative
                                                                && !e.Value.IsEnabled);
                    break;
                
                case ActionType.EnableRandomPositiveEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Positive
                                                                && !e.Value.IsEnabled);
                    break;
                
                case ActionType.DisableRandomMixedEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Mixed 
                                                                && e.Value.IsEnabled);
                    break;
                
                case ActionType.DisableRandomNegativeEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Negative
                                                                && e.Value.IsEnabled);
                    break;
                
                case ActionType.DisableRandomPositiveEffect:
                    available = player.Effects.Effects.Any(e => !IgnoreEffects.Contains(e.Key.Name)
                                                                && e.Value.Classification is StatusEffectBase.EffectClassification.Positive
                                                                && e.Value.IsEnabled);
                    break;
                
                default:
                    available = true;
                    break;
            }
            
            if (!available)
                continue;
            
            list.Add(kvp.Key);
        }

        if (list.Count > 0)
        {
            actions[player] = list.GetRandomWeighted(a => Weights[a]);

            ListPool<ActionType>.Shared.Return(list);
            return true;
        }
        
        ListPool<ActionType>.Shared.Return(list);
        return false;
    }

    /// <summary>
    /// Executes the action for the specified player, applying the intended effects.
    /// </summary>
    /// <param name="player">The player on whom the action will be executed.</param>
    public override void Execute(ExPlayer player)
    {
        base.Execute(player);

        if (!actions.TryGetValue(player, out var action))
        {
            ApiLog.Warn("CoinManager", $"Could not find action for player {player.ToLogString()}!");
            return;
        }
        
        actions.Remove(player);

        void EnableEffect(StatusEffectBase effect)
        {
            if (effect == null)
                return;

            if (EffectConfigs.TryGetValue(effect.GetType().Name, out var config))
            {
                effect.ServerSetState(config.Intensity, config.Duration);
            }
            else
            {
                effect.ServerSetState(1, 0f);
            }
        }

        switch (action)
        {
            case ActionType.ClearAllEffects:
                player.Effects.DisableAllEffects();
                break;
            
            case ActionType.ClearMixedEffects:
                player.Effects.ActiveEffects.Where(e => e.Classification is StatusEffectBase.EffectClassification.Mixed
                                                        && !IgnoreEffects.Contains(e.GetType().Name))
                                            .ForEach(e => e.ServerDisable());
                break;
            
            case ActionType.ClearNegativeEffects:
                player.Effects.ActiveEffects.Where(e => e.Classification is StatusEffectBase.EffectClassification.Negative
                                                        && !IgnoreEffects.Contains(e.GetType().Name))
                                            .ForEach(e => e.ServerDisable());
                break;
            
            case ActionType.ClearPositiveEffects:
                player.Effects.ActiveEffects.Where(e => e.Classification is StatusEffectBase.EffectClassification.Positive
                                                        && !IgnoreEffects.Contains(e.GetType().Name))
                                            .ForEach(e => e.ServerDisable());
                break;
            
            case ActionType.EnableRandomMixedEffect:
                EnableEffect(player.Effects.Effects.Where(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                        && !kvp.Value.IsEnabled
                                                        && kvp.Value.Classification is StatusEffectBase .EffectClassification.Mixed)
                                            .GetRandomOrDefault().Value);
                break;
            
            case ActionType.EnableRandomNegativeEffect:
                EnableEffect(player.Effects.Effects.GetRandomOrDefault(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                                         && !kvp.Value.IsEnabled
                                                                         && kvp.Value.Classification is StatusEffectBase .EffectClassification.Negative)
                                                .Value);
                break;
            
            case ActionType.EnableRandomPositiveEffect:
                EnableEffect(player.Effects.Effects.GetRandomOrDefault(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                                         && !kvp.Value.IsEnabled
                                                                         && kvp.Value.Classification is StatusEffectBase .EffectClassification.Positive)
                                                .Value);
                break;
            
            case ActionType.DisableRandomMixedEffect:
                player.Effects.Effects.GetRandomOrDefault(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                            && kvp.Value.IsEnabled 
                                                            && kvp.Value.Classification is StatusEffectBase.EffectClassification.Mixed)
                                                .Value?.ServerDisable();
                break;
            
            case ActionType.DisableRandomNegativeEffect:
                player.Effects.Effects.GetRandomOrDefault(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                            && kvp.Value.IsEnabled 
                                                            && kvp.Value.Classification is StatusEffectBase.EffectClassification.Negative)
                                      .Value?.ServerDisable();
                break;
            
            case ActionType.DisableRandomPositiveEffect:
                player.Effects.Effects.GetRandomOrDefault(kvp => !IgnoreEffects.Contains(kvp.Key.Name) 
                                                            && kvp.Value.IsEnabled 
                                                            && kvp.Value.Classification is StatusEffectBase.EffectClassification.Positive)
                                      .Value?.ServerDisable();
                break;
        }
    }
}