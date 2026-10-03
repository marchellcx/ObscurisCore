using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

using LabExtended.API;

using LabExtended.Core;
using LabExtended.Core.Pooling.Pools;

using LabExtended.Utilities;
using LabExtended.Extensions;

using NiveraAPI.IO.Configs;

using NorthwoodLib.Pools;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities.Configs;
using ObscurisCore.Extensions;

namespace ObscurisCore.Features.Coin;

/// <summary>
/// Manages the registration and execution of coin actions.
/// </summary>
public static class CoinManager
{
    /// <summary>
    /// All registered coin actions.
    /// </summary>
    public static List<CoinAction> Actions { get; } = new();

    /// <summary>
    /// Defines the range for generating a random number of actions
    /// that can occur simultaneously.
    /// </summary>
    [Config("coinManager", "actionCount", "The range for generating a random amount of actions that can occur simultaneously.")]
    public static Int32Range ActionCount { get; set; } = new()
    {
        MinValue = 1,
        MaxValue = 1
    };

    /// <summary>
    /// Defines the weight (chance) of multiple coin actions occurring simultaneously.
    /// </summary>
    [Config("coinManager", "multipleWeight", "Defines the weight (chance) of multiple actions occuring.")]
    public static WeightConfig MultipleActionsWeight { get; set; } = new();

    /// <summary>
    /// The message to be sent to players when they fail to flip a coin.
    /// </summary>
    [Config("coinManager", "flipFailedMessage", "The message to be sent to players when they fail to flip a coin.")]
    public static string FlipFailedMessage { get; set; } = "Zkus to znova!";

    /// <summary>
    /// Executes a coin flip action for a given player.
    /// </summary>
    /// <param name="player">The player for whom the coin flip action is executed. Cannot be null.</param>
    /// <param name="allowMultiple">
    /// Specifies whether multiple coin flip actions can occur simultaneously. If set to true,
    /// the number of actions is determined by the configuration's <c>CoinFlipMultipleRange</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="player"/> or its <c>ReferenceHub</c> is null.
    /// </exception>
    public static bool ExecuteCoinFlip(ExPlayer player, bool allowMultiple = false)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        try
        {
            if (Actions.TryGetFirst(a => a.ShouldForce(player), out var forcedAction))
            {
                forcedAction.Execute(player);
                return true;
            }
        }
        catch (Exception ex)
        {
            ApiLog.Error("CoinManager", ex);
            return false;
        }

        var count = ActionCount.GetRandom();

        if (count < 1)
            return false;

        if (allowMultiple && count > 1)
        {
            if (!ExecuteMultipleFlip(player, count))
            {
                return ExecuteSingleFlip(player);
            }
        }
        else
        {
            return ExecuteSingleFlip(player);
        }

        return false;
    }

    private static bool ExecuteMultipleFlip(ExPlayer player, int count)
    {
        var selected = ListPool<CoinAction>.Shared.Rent();
        var available = ListPool<CoinAction>.Shared.Rent(Actions);

        while (selected.Count < count && available.Count > 0)
        {
            for (var x = 0; x < available.Count; x++)
            {
                var action = available[x];

                if (!action.CanBeCombined)
                    continue;

                if (!action.IsAvailable(player))
                    continue;

                var weight = player.GetFloatWeight(action.Multipliers, action.Weight);

                if (weight <= 0f)
                    continue;

                if (weight >= 100f || WeightUtils.GetBool(weight))
                {
                    selected.Add(action);
                    available.Remove(action);
                }
            }
        }

        ListPool<CoinAction>.Shared.Return(available);
        
        if (selected.Count < count)
        {
            ListPool<CoinAction>.Shared.Return(selected);
                
            ApiLog.Warn("CoinManager", $"Not enough coin actions were selected for player {player.ToLogString()}!");
            return false;
        }

        for (var x = 0; x < selected.Count; x++)
        {
            try
            {
                selected[x].Execute(player);
            }
            catch (Exception ex)
            {
                ApiLog.Error("CoinManager", $"Error while invoking action &1{selected[x].GetType()}&r for player {player.ToLogString()}:\n{ex}");
            }
        }

        ListPool<CoinAction>.Shared.Return(selected);
        return true;
    }

    private static bool ExecuteSingleFlip(ExPlayer player)
    {
        var actions = DictionaryPool<CoinAction, float>.Shared.Rent();

        try
        {
            for (var x = 0; x < Actions.Count; x++)
            {
                var action = Actions[x];

                if (!action.IsAvailable(player))
                    continue;

                var weight = player.GetFloatWeight(action.Multipliers, action.Weight);

                if (weight <= 0f)
                    continue;

                if (weight >= 100f || WeightUtils.GetBool(weight))
                    actions.Add(action, weight);
            }

            if (actions.Count == 0)
            {
                DictionaryPool<CoinAction, float>.Shared.Return(actions);
                return false;
            }

            var selected = actions.GetRandomWeighted(kvp => kvp.Value);

            if (selected.Key == null)
            {
                ApiLog.Warn("CoinManager", $"No coin action was selected for player {player.ToLogString()}!");
                return false;
            }

            selected.Key.Execute(player);
        }
        catch (Exception ex)
        {
            ApiLog.Error("CoinManager", $"Error while invoking action for player {player.ToLogString()}:\n{ex}");
            return false;
        }

        DictionaryPool<CoinAction, float>.Shared.Return(actions);
        return true;
    }
    
    private static void OnFlippingCoin(PlayerFlippingCoinEventArgs args)
    {
        if (!args.Player.CastPlayer(out var player))
            return;

        try
        {
            if (!ExecuteCoinFlip(player,
                WeightUtils.GetBool(
                    MultipleActionsWeight.GetWeight(player))))
            {
                player.SendFormattedAlert(FlipFailedMessage, true, AlertType.Warn, 5f, "Coin Manager");
                return;
            }
        }
        catch (Exception ex)
        {
            ApiLog.Error("CoinManager", ex);
        }

        player.Inventory.RemoveHeldItem();
    }
    
    private static void Initialize()
    {
        foreach (var type in typeof(CoinManager).Assembly.GetTypes())
        {
            try
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (type.Namespace !=
                    "ObscurisCore.Features.Coin.Actions") // lets avoid checking other types for inheritance
                    continue;

                if (typeof(CoinAction).IsAssignableFrom(type))
                {
                    var path = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "coin_actions", $"{type.Name}.yml");

                    if (!FileUtils.TryLoadYamlFile<CoinAction>(path, type, out var action))
                        FileUtils.TrySaveYamlFile(path, action = Activator.CreateInstance(type) as CoinAction);

                    Actions.Add(action);
                    
                    action.Enable();

                    ApiLog.Debug("CoinManager", $"Registered &1{type.Name}&r as a coin action!");
                }
            }
            catch (Exception ex)
            {
                ApiLog.Error("CoinManager", $"Error while registering action &1{type}&r:\n{ex}");
            }
        }

        if (Actions.Count > 0)
        {
            PlayerEvents.FlippingCoin += OnFlippingCoin;
            
            ApiLog.Info("CoinManager", $"Registered &1{Actions.Count}&r coin actions");
        }
        else
        {
            ApiLog.Error("CoinManager", "No coin actions were registered!");
        }
    }
}