using LabExtended.API;
using LabExtended.API.Settings;
using LabExtended.API.Settings.Menus;
using LabExtended.API.Settings.Entries;

using LabExtended.Core;
using LabExtended.Events;
using MEC;
using NiveraAPI.Extensions;
using NiveraAPI.IO.Configs;

using NorthwoodLib.Pools;

using PRTS.Client.Profiles;
using PRTS.Client.Profiles.Objects;

using TMPro;

using UnityEngine;

namespace ObscurisCore.Features.Events.Halloween.Lanterns;

/// <summary>
/// Provides functionality for managing and retrieving data related to the Lantern Event,
/// including operations for fetching the top players based on lantern points collected.
/// </summary>
public static class LanternEvent
{
    /// <summary>
    /// Represents a settings menu for the Lantern Collector Event, providing functionality
    /// to display and manage event-specific data, including statistics on lantern collection.
    /// </summary>
    public class EventMenu : SettingsMenu
    {
        /// <summary>
        /// Gets the unique identifier for the Halloween 2026 Lantern Collector Event menu,
        /// used to distinguish this event menu from others within the settings system.
        /// </summary>
        public override string CustomId { get; } = "Halloween2026_LanternCollectorEvent";

        /// <summary>
        /// Gets the title or display text for the settings menu associated with the Lantern Event,
        /// formatted with specific styling for the Halloween 2026 theme.
        /// </summary>
        public override string Header { get; } = "<color=orange>👻</color> | <color=orange>Halloween <color=red>2026</color>";

        /// <summary>
        /// Represents a configurable multi-line text area entry in the settings menu,
        /// used to display or manage text content associated with the Lantern Collector Event.
        /// </summary>
        public SettingsTextArea CountTextArea { get; private set; }

        /// <summary>
        /// Gets or sets the settings text area used to display the total number of lanterns
        /// collected during the Lantern Collector Event. This property provides a visual
        /// representation of the cumulative event progress for players.
        /// </summary>
        public SettingsTextArea TotalTextArea { get; private set; }

        /// <summary>
        /// Gets the text area for displaying the list of top players in the Lantern Collector Event menu.
        /// This area provides a visual representation of high-ranking participants based on their performance
        /// during the event, allowing users to view the leaderboard directly within the event settings interface.
        /// </summary>
        public SettingsTextArea TopPlayersArea { get; private set; }

        /// <summary>
        /// Updates the displayed total count of lanterns collected in the Lantern Collector Event settings menu.
        /// </summary>
        /// <param name="newCount">
        /// The new total count of lanterns collected to be displayed in the menu. This value specifies the
        /// updated text content for the total statistics area.
        /// </param>
        public void UpdateTotalCount(int newCount)
        {
            try
            {
                CountTextArea.SendText($"<color=blue>👥</color> | <b>Celkový počet nasbíraných dýní</b>: {newCount}");
            }
            catch (Exception ex)
            {
                ApiLog.Error("LanternEventMenu", $"Failed to update menu count for {Player?.ToLogString() ?? "null"}:\n{ex}");
            }
        }   
        
        /// <summary>
        /// Updates the displayed count of lanterns collected in the Lantern Collector Event settings menu.
        /// </summary>
        /// <param name="newCount">
        /// The new count of lanterns collected to be displayed in the menu. This value determines the
        /// updated text content for the statistics area.
        /// </param>
        public void UpdateCollectedCount(int newCount)
        {
            try
            {
                CountTextArea.SendText($"<color=orange>👻</color> | <b>Počet nasbíraných dýní</b>: {newCount}");
            }
            catch (Exception ex)
            {
                ApiLog.Error("LanternEventMenu", $"Failed to update menu count for {Player?.ToLogString() ?? "null"}:\n{ex}");
            }
        }

        /// <summary>
        /// Updates the displayed list of top players in the Lantern Collector Event settings menu.
        /// </summary>
        public void UpdateTopPlayers()
        {
            try
            {
                if (TopPlayers?.Count > 0)
                {
                    var sb = StringBuilderPool.Shared.Rent();
                    var position = 1;

                    sb.AppendLine("<color=blue>👥</color> | <b>Top hráči</b>:");

                    foreach (var kvp in TopPlayers)
                    {
                        if (position == 1)
                        {
                            sb.AppendLine($"<color=#FFD700>🥇 {position}.</color> {kvp.Key.GetNickname()} (<color=red>{kvp.Value}</color>x)");
                        }
                        else if (position == 2)
                        {
                            sb.AppendLine($"<color=#C0C0C0>🥈 {position}.</color> {kvp.Key.GetNickname()} (<color=red>{kvp.Value}</color>x)");
                        }
                        else if (position == 3)
                        {
                            sb.AppendLine($"<color=#CD7F32>🥉 {position}.</color> {kvp.Key.GetNickname()} (<color=red>{kvp.Value}</color>x)");
                        }
                        else
                        {
                            sb.AppendLine($"<color=#FFA500>🎖️ {position}.</color> {kvp.Key.GetNickname()} (<color=red>{kvp.Value}</color>x)");
                        }

                        position++;
                    }
                    
                    TopPlayersArea?.SendText(StringBuilderPool.Shared.ToStringReturn(sb));
                }
            }
            catch (Exception ex)
            {
                ApiLog.Error("LanternEventMenu", $"Could not update top players for {Player?.ToLogString() ?? "null"}:\n{ex}");
            }
        }

        /// <summary>
        /// Constructs the settings menu for the Lantern Collector Event by populating it
        /// with the appropriate entries, including the text area for displaying lantern collection statistics.
        /// </summary>
        /// <param name="settings">
        /// A list of settings entries to be customized and populated within the menu.
        /// This list should not be null and will be modified directly within the method.
        /// </param>
        public override void BuildMenu(List<SettingsEntry> settings)
        {
            settings.WithEntry(CountTextArea = SettingsTextArea.Create("Halloween2026_LanternCollector_Points", "<color=orange>🎃</color> | <b>Počet nasbíraných dýní</b>: 0", "<color=orange>🎃</color> | <b>Počet nasbíraných dýní</b>: 0", TextAlignmentOptions.Left));
            settings.WithEntry(TopPlayersArea = SettingsTextArea.Create("Halloween2026_LanternCollector_TopPlayers", "<color=blue>👥</color> | <b>Top hráči</b>: ", "<color=blue>👥</color> | <b>Top hráči</b>: ", TextAlignmentOptions.Left));
            settings.WithEntry(TotalTextArea = SettingsTextArea.Create("Halloween2026_LanternCollector_Total", "<color=blue>👥</color> | <b>Celkový počet nasbíraných dýní</b>: 0", "<color=blue>👥</color> | <b>Celkový počet nasbíraných dýní</b>: 0", TextAlignmentOptions.Left));
            
            Timing.CallDelayed(2f, () =>
            {
                if (Player?.ReferenceHub != null)
                {
                    UpdatePlayer(Player);
                }
            });
        }
    }
    
    [Config("halloween", "lantern-collector-max-top-players", "The amount of top players to display in the menu.")]
    private static int maxTopPlayers = 10;
    
    /// <summary>
    /// Specifies the name of the database table that stores point records for Jack-O-Lantern kills during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-points-table-name", "The name of the table containing lantern kill points.")]
    public static string PointsDataName = "Halloween2026_LanternPoints";

    /// <summary>
    /// Represents the total number of lanterns collected during the Halloween Lantern Event.
    /// This variable is updated dynamically as players collect lanterns and determines the overall
    /// accumulation of collected lanterns across all participants.
    /// </summary>
    public static int TotalCount = 0;
    
    /// <summary>
    /// Gets a dictionary of top players and their associated scores in the Lantern Event,
    /// ordered by the number of lantern points collected. The key represents the player's
    /// profile information, and the value represents their total points.
    /// </summary>
    public static Dictionary<ProfileInfo, int> TopPlayers { get; private set; }
    
    /// <summary>
    /// Retrieves the top players based on the number of lantern points collected and passes the result to the specified callback function.
    /// </summary>
    /// <param name="playerCount">The maximum number of top players to retrieve. Must be greater than zero.</param>
    /// <param name="callback">
    /// A callback function that receives a dictionary containing player information as keys and their respective lantern points as values.
    /// If no data is available, the callback will receive null.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the retrieval process was started successfully.
    /// Returns false if the profile module singleton is not initialized.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the <paramref name="playerCount"/> is less than one.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="callback"/> is null.
    /// </exception>
    public static bool GetTopPlayers(int playerCount, Action<Dictionary<ProfileInfo, int>?> callback)
    {
        if (playerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(playerCount));

        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (ProfileModule.Singleton == null)
        {
            ApiLog.Warn("LanternEvent", "Profile module singleton is not initialized.");
            return false;
        }
        
        ApiLog.Debug("LanternEvent", $"Sending request for top players ..");
        
        ProfileModule.Singleton.CallCmdGetAllData(PointsDataName, dict =>
        {
            ApiLog.Debug("LanternEvent", $"Received server response with &3{dict?.Count ?? -1}&r profile(s)");
            
            if (dict != null)
            {
                TotalCount = 0;
                
                var topPlayers = new Dictionary<ProfileInfo, int>();

                foreach (var kvp in dict)
                {
                    if (!kvp.Value.CustomData.TryGetValue(PointsDataName, out var dataValue))
                        continue;
                    
                    if (!int.TryParse(dataValue, out var collectedLanterns))
                        continue;

                    TotalCount += collectedLanterns;
                    
                    topPlayers[kvp.Value] = collectedLanterns;
                    
                    ApiLog.Debug("LanternEvent", $"Saved player score &3{kvp.Value.GetNickname()}&r with &3{collectedLanterns}&r lantern(s).");
                }
                
                var orderedPlayers = topPlayers
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(Mathf.Min(topPlayers.Count, playerCount))
                    .ToDictionary();
                
                ApiLog.Debug("LanternEvent", $"Ordered dictionary with &3{orderedPlayers?.Count ?? -1}&r top players");

                TopPlayers = topPlayers;
                
                foreach (var player in ExPlayer.Players)
                {
                    if (player.TryGetMenu<EventMenu>(out var menu))
                    {
                        menu.UpdateTotalCount(TotalCount);
                        menu.UpdateTopPlayers();
                    }
                }
                
                callback(orderedPlayers);
            }
            else
            {
                callback(null);
            }
        });

        return true;
    }

        /// <summary>
    /// Retrieves the points for a specific player in the Halloween event.
    /// The operation requires a connected and operational database to access the stored data.
    /// </summary>
    /// <param name="player">The player whose points are being retrieved.</param>
    /// <param name="callback">
    /// A callback function to be invoked once the operation completes, providing the retrieved points or null if the operation fails.
    /// </param>
    /// <returns>
    /// Returns true if the request to retrieve the points was successfully executed and the database is available;
    /// otherwise, false.
    /// </returns>
    public static bool GetPoints(ExPlayer player, Action<int?> callback)
    {
        if (ProfileModule.Singleton == null)
        {
            ApiLog.Warn("LanternSpawner", $"Could not get points for player {player.ToLogString()}: Network offline");
            
            callback?.Invoke(null);
            return false;
        }
        
        ProfileModule.Singleton.CallCmdGetCustomData(player.UserId, PointsDataName, reply =>
        {
            if (!reply.ProfileFound)
            {
                ApiLog.Warn("LanternSpawner", $"Could not get points for player {player.ToLogString()}: Profile not found");

                callback(null);
                return;
            }

            if (!reply.KeyFound)
            {
                ApiLog.Warn("LanternSpawner", $"Could not get points for player {player.ToLogString()}: Key not found");

                callback(null);
                return;
            }
            
            if (!int.TryParse(reply.DataValue, out var points))
            {
                ApiLog.Warn("LanternSpawner", $"Could not get points for player {player.ToLogString()}: Invalid data value");

                callback(null);
                return;
            }

            callback(points);
        });
        
        return true; 
    }
    
    /// <summary>
    /// Adds the specified amount of points to a player's total in the Halloween event.
    /// The operation is performed using a connected and authenticated database.
    /// </summary>
    /// <param name="player">The player to whom the points will be added.</param>
    /// <param name="points">The number of points to add for the player.</param>
    /// <param name="callback">The callback to invoke when the operation is complete.</param>
    /// <returns>
    /// Returns true if the points were successfully added and the database is connected and authenticated;
    /// otherwise, false.
    /// </returns>
    public static bool AddPoints(ExPlayer player, int points, Action<int?>? callback = null)
    {
        if (ProfileModule.Singleton != null)
        {
            GetPoints(player, ogPoints =>
            {
                var oldPoints = ogPoints ?? 0;
                var newPoints = oldPoints + points;
                
                ProfileModule.Singleton.CallCmdSetCustomData(player.UserId, PointsDataName, newPoints.ToString(), result =>
                {
                    if (!result)
                    {
                        callback?.Invoke(null);
                    
                        ApiLog.Warn("LanternSpawner", $"Could not add points for player {player.ToLogString()}");
                        return;
                    }
                
                    ApiLog.Debug("LanternSpawner", $"Added &3{points}&r points for player {player.ToLogString()}");

                    callback?.Invoke(newPoints);
                });
            });
            
            return true;
        }

        ApiLog.Warn("LanternSpawner", $"Could not add points for player {player.ToLogString()}: Network offline");
        return false;
    }

    
    /// <summary>
    /// Updates the collected lantern count for a player in the Lantern Collector Event
    /// and manages the display of updated statistics in the corresponding menu.
    /// </summary>
    /// <param name="player">
    /// The player whose lantern collection count is being updated. This player must have
    /// a valid reference hub and be associated with the event menu.
    /// </param>
    /// <param name="nowCollected">
    /// The new collected lantern count for the specified player. This value will be used
    /// to update the player's statistics and re-order the list of top players.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided player is null or does not have a valid reference hub.
    /// </exception>
    public static void UpdateCollected(ExPlayer player, int nowCollected, int collectedAmount)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        if (!player.TryGetMenu<EventMenu>(out var menu))
            return;

        TotalCount += collectedAmount;
        
        menu.UpdateTotalCount(TotalCount);
        menu.UpdateCollectedCount(nowCollected);

        if (TopPlayers != null && TopPlayers.TryGetFirst(kvp => kvp.Key.UserId == player.UserId, out var kvp))
        {
            TopPlayers[kvp.Key] = nowCollected;
            TopPlayers = TopPlayers.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var ply in ExPlayer.Players)
            {
                if (ply?.ReferenceHub == null)
                    continue;
                
                if (!ply.TryGetMenu<EventMenu>(out menu))
                    continue;

                menu.UpdateTopPlayers();
            }
        }
    }

    /// <summary>
    /// Updates the Lantern Event menu for the specified player, ensuring it displays the
    /// latest lantern points and top players list within the event.
    /// </summary>
    /// <param name="player">
    /// The player whose event menu needs to be updated. This parameter must reference
    /// a valid player object with their associated event menu and profile data initialized.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided player or their reference hub is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the player does not have the event menu available or if the profile module
    /// is not properly connected to retrieve custom data.
    /// </exception>
    public static void UpdatePlayer(ExPlayer player)
    {
        if (player?.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        if (!player.TryGetMenu<EventMenu>(out var menu))
            throw new InvalidOperationException("Player does not have the event menu.");
        
        if (ProfileModule.Singleton == null)
            throw new InvalidOperationException("PRTS is not connected.");
        
        menu.UpdateTopPlayers();
        menu.UpdateTotalCount(TotalCount);
        
        ProfileModule.Singleton.CallCmdGetCustomData(player.UserId, PointsDataName, reply =>
        {
            if (reply.KeyFound && int.TryParse(reply.DataValue, out var points))
            {
                menu.UpdateCollectedCount(points);
            }
            else
            {
                ApiLog.Warn("LanternEvent", $"Player {player.UserId} does not have lantern points data.");
            }
        });
    }

    private static void OnWaiting()
    {
        IEnumerator<float> AwaitPrts()
        {
            while (ProfileModule.Singleton == null)
                yield return Timing.WaitForSeconds(1f);
            
            ApiLog.Info("LanternEvent", "Requesting top players ..");
            
            GetTopPlayers(maxTopPlayers, _ => { ApiLog.Info("LanternEvent", $"Updated top players ({TopPlayers.Count}) ({TotalCount})"); });
        }
        
        Timing.RunCoroutine(AwaitPrts());
    }

    private static void Initialize()
    {
        ExRoundEvents.WaitingForPlayers += OnWaiting;
        
        SettingsManager.AddBuilder(new SettingsBuilder("Halloween2026_MenuBuilder").WithMenu(() => new EventMenu()));
    }
}