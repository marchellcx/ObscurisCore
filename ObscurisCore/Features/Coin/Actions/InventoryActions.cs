using System.ComponentModel;

using InventorySystem.Items.ThrowableProjectiles;

using LabExtended.API;
using LabExtended.Core;
using LabExtended.Events;
using LabExtended.Utilities;
using LabExtended.Extensions;

using NorthwoodLib.Pools;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Features.Looting;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.Coin.Actions;

/// <summary>
/// Defines a set of inventory-related actions that can be performed when flipping a coin.
/// </summary>
public class InventoryActions : CoinAction
{
    /// <summary>
    /// Specifies the different types of actions that can be performed on a player's inventory when flipping a coin.
    /// </summary>
    public enum ActionType : byte
    {
        /// <summary>
        /// Drops all items in the inventory.
        /// </summary>
        DropAll,
        
        /// <summary>
        /// Drops the item currently in the player's hand.
        /// </summary>
        DropCurrent,
        
        /// <summary>
        /// Removes all items in the inventory.
        /// </summary>
        RemoveAll,
        
        /// <summary>
        /// Removes the item currently in the player's hand.
        /// </summary>
        RemoveCurrent,
        
        /// <summary>
        /// Switches the player's inventory for another player's inventory.
        /// </summary>
        SwitchAll,
        
        /// <summary>
        /// Switches the item held by the player for an item held by another random player.
        /// </summary>
        SwitchCurrent,
        
        /// <summary>
        /// Explodes all projectiles in the inventory.
        /// </summary>
        ExplodeAll,
        
        /// <summary>
        /// Explodes the currently held projectile.
        /// </summary>
        ExplodeCurrent,
        
        /// <summary>
        /// Adds loot to the player's inventory.
        /// </summary>
        Loot,
    }

    /// <summary>
    /// The weights of the actions.
    /// </summary>
    [Description("Sets the weights of the actions.")]
    public Dictionary<ActionType, float> Weights { get; set; } = new()
    {
        { ActionType.DropAll, 0f },
        { ActionType.DropCurrent, 0f },
        
        { ActionType.RemoveAll, 0f },
        { ActionType.RemoveCurrent, 0f },
        
        { ActionType.SwitchAll, 0f },
        { ActionType.SwitchCurrent, 0f },
        
        { ActionType.ExplodeAll, 0f },
        { ActionType.ExplodeCurrent, 0f },
        
        { ActionType.Loot, 0f }
    };

    /// <summary>
    /// The messages to be sent to players when they perform the actions.
    /// </summary>
    [Description("Sets the messages to be sent to players when they perform the actions.")]
    public Dictionary<ActionType, string> Messages { get; set; } = new()
    {
        { ActionType.DropAll, "" },
        { ActionType.DropCurrent, "" },

        { ActionType.RemoveAll, "" },
        { ActionType.RemoveCurrent, "" },

        { ActionType.SwitchAll, "" },
        { ActionType.SwitchCurrent, "" },

        { ActionType.ExplodeAll, "" },
        { ActionType.ExplodeCurrent, "" },
        
        { ActionType.Loot, "" }
    };
    
    private Dictionary<ExPlayer, ActionType> actions = new();

    /// <summary>
    /// Enables the coin action.
    /// </summary>
    public override void Enable()
    {
        base.Enable();
        
        ExPlayerEvents.Left += OnLeft;
        ExRoundEvents.WaitingForPlayers += OnWaiting;
    }

    /// <summary>
    /// Determines if the specified action is available for the given player based on the defined weights and player inventory.
    /// </summary>
    /// <param name="player">The player for whom the action's availability is being checked.</param>
    /// <returns>True if the action is available, otherwise false.</returns>
    public override bool IsAvailable(ExPlayer player)
    {
        if (!base.IsAvailable(player))
            return false;
        
        var available = ListPool<ActionType>.Shared.Rent();

        foreach (var kvp in Weights)
        {
            if (kvp.Value <= 0f)
                continue;
            
            var isAvailable = false;
            
            switch (kvp.Key)
            {
                case ActionType.Loot:
                    isAvailable = LootManager.GetTable("CoinInventoryLoot") != null;
                    break;
                
                case ActionType.DropAll or ActionType.RemoveAll:
                    isAvailable = player.Inventory.ItemCount > 0;
                    break;
                
                case ActionType.DropCurrent or ActionType.RemoveCurrent:
                    isAvailable = player.Inventory.CurrentItem != null;
                    break;
                
                case ActionType.SwitchAll:
                    isAvailable = ExPlayer.Players.Any(p => p != player && !p.IsSCP && p.IsAlive && !p.IsTutorial && p.Inventory.ItemCount > 0);
                    break;
                
                case ActionType.SwitchCurrent:
                    isAvailable = player.Inventory.CurrentItem != null 
                                  && ExPlayer.Players.Any(p => 
                                      p != player 
                                      && p.IsAlive
                                      && !p.IsSCP
                                      && !p.IsTutorial
                                      && p.Inventory.CurrentItem != null);
                    break;
                
                case ActionType.ExplodeAll:
                    isAvailable = player.Inventory.Items.Any(it => 
                        it is ThrowableItem throwableItem && throwableItem.Projectile != null);
                    break;
                
                case ActionType.ExplodeCurrent:
                    isAvailable = player.Inventory.CurrentItem != null 
                                  && player.Inventory.CurrentItem is ThrowableItem throwableItem
                                  && throwableItem.Projectile != null;
                    break;
            }
            
            if (!isAvailable)
                continue;
            
            available.Add(kvp.Key);
        }

        if (available.Count > 0)
        {
            actions[player] = available.GetRandomWeighted(act => Weights[act]);
            
            ListPool<ActionType>.Shared.Return(available);
            return true;
        }

        ListPool<ActionType>.Shared.Return(available);
        return false;
    }

    public override void Execute(ExPlayer player)
    {
        base.Execute(player);

        if (!actions.TryGetValue(player, out var action))
        {
            ApiLog.Warn("CoinManager", $"Could not find action for player {player.ToLogString()}!");
            return;
        }
        
        actions.Remove(player);

        player.SendFormattedAlert(action, Messages, false, AlertType.Info, 5f, "Coin Manager");
        
        switch (action)
        {
            case ActionType.Loot:
                LootManager.ExecuteEntry(player, "CoinInventoryLoot");
                break;
            
            case ActionType.DropAll:
                player.Ammo.DropAllAmmo();
                player.Ammo.ClearCustomAmmo();

                player.Inventory.DropItems();
                break;

            case ActionType.DropCurrent:
                player.Inventory.DropHeldItem();
                break;

            case ActionType.RemoveAll:
                player.Ammo.ClearAmmo();
                player.Ammo.ClearCustomAmmo();

                player.Inventory.Clear();
                break;

            case ActionType.RemoveCurrent:
                player.Inventory.RemoveHeldItem();
                break;

            case ActionType.SwitchAll:
                SwitchAll(player);
                break;

            case ActionType.SwitchCurrent:
                SwitchCurrent(player);
                break;

            case ActionType.ExplodeAll:
            {
                player.Inventory.Items
                    .ToList()
                    .ForEach(it =>
                    {
                        if (it is ThrowableItem throwableItem && throwableItem.Projectile != null)
                        {
                            ExMap.SpawnProjectile(it.ItemTypeId, player.Position, Vector3.one, Vector3.zero,
                                player.Rotation, 0f, 3f);
                            player.Inventory.RemoveItem(it);
                        }
                    });
                break;
            }

            case ActionType.ExplodeCurrent:
                ExMap.SpawnProjectile(player.CurrentItem!.Type, player.Position, Vector3.one, Vector3.zero,
                    player.Rotation, 0f, 3f);
                player.Inventory.RemoveHeldItem();
                break;
        }
    }
    
    private void OnLeft(ExPlayer player)
    {
        actions.Remove(player);
    }
    
    private void OnWaiting()
    {
        actions.Clear();
    }

    private static void SwitchAll(ExPlayer player)
    {
        var other = ExPlayer.Players.GetRandomItem(p => p?.ReferenceHub != null
                                                        && p != player
                                                        && p.IsAlive
                                                        && !p.IsSCP
                                                        && !p.IsTutorial
                                                        && p.Inventory.ItemCount > 0);

        if (other?.ReferenceHub == null)
        {
            ApiLog.Warn("CoinManager", $"Could not find a player to switch inventory with for player {player.ToLogString()}!");
            return;
        }
        
        player.SwitchFullInventory(other);
    }

    private static void SwitchCurrent(ExPlayer player)
    {
        var other = ExPlayer.Players.GetRandomItem(p => p?.ReferenceHub != null
                                                        && p != player
                                                        && p.IsAlive
                                                        && !p.IsSCP
                                                        && !p.IsTutorial
                                                        && p.Inventory.ItemCount > 0
                                                        && p.Inventory.CurrentItem != null);

        if (other?.ReferenceHub == null)
        {
            ApiLog.Warn("CoinManager", $"Could not find a player to switch current item with for player {player.ToLogString()}!");
            return;
        }
        
        player.SwitchHeldItem(other);
    }
}