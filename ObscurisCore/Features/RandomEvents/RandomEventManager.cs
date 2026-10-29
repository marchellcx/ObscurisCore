using LabExtended.API;
using LabExtended.API.Custom.Gamemodes;

using LabExtended.Events;
using LabExtended.Utilities;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.RandomEvents.Events;
using ObscurisCore.Features.RandomEvents.Events.Randomizer;

namespace ObscurisCore.Features.RandomEvents;

/// <summary>
/// Manages the loading and execution of random events.
/// </summary>
public static class RandomEventManager
{
    private static List<CustomGamemode> previousEvents = new();

    /// <summary>
    /// Gets the global configuration settings for random events.
    /// </summary>
    [Config("randomEvents", "config", "Global configuration settings for random events.")]
    public static RandomEventConfig Config { get; set; } = new();

    /// <summary>
    /// Gets the list of registered random events.
    /// </summary>
    public static List<RandomEventBase> Events { get; } = new();

    private static void OnRoundStart()
    {
        if (Config.EventWeight <= 0f)
        {
            previousEvents.Clear();
            return;
        }

        if (Config.EventWeight < 100f && !WeightUtils.GetBool(Config.EventWeight))
        {
            previousEvents.Clear();
            return;
        }

        if (Config.GroupWeight > 0f && WeightUtils.GetBool(Config.GroupWeight))
        {
            var groupCount = Config.GroupSize.GetRandom();

            if (groupCount < 1)
            {
                previousEvents.Clear();
                return;
            }

            var availableEvents = CustomGamemode.RegisteredObjects.Values.ToList();

            availableEvents.RemoveAll(x =>
            {
                if (!x.CanActivateMidRound)
                    return true;

                if (previousEvents.Contains(x))
                    return true;

                if (x is RandomEventBase randomEventBase)
                {
                    if (!randomEventBase.CanBeGrouped)
                        return true;

                    if (randomEventBase.MinPlayers != null && ExPlayer.Count < randomEventBase.MinPlayers)
                        return true;

                    if (randomEventBase.MaxPlayers != null && ExPlayer.Count > randomEventBase.MaxPlayers)
                        return true;

                    if (randomEventBase.Weight <= 0f)
                        return true;

                    if (randomEventBase.Weight >= 100f)
                        return false;

                    if (!WeightUtils.GetBool(randomEventBase.Weight))
                        return true;
                }
                else if (Config.Weights.TryGetValue(x.Id, out var weight))
                {
                    if (weight <= 0f)
                        return true;

                    if (weight >= 100f)
                        return false;

                    if (!WeightUtils.GetBool(weight))
                        return true;
                }

                return false;
            });

            if (availableEvents.Count == 0)
            {
                previousEvents.Clear();
                return;
            }

            var groupEvents = new List<CustomGamemode>();

            while (groupEvents.Count < groupCount && availableEvents.Count > 0)
            {
                var selectedEvent = availableEvents.RandomItem();

                if (selectedEvent == null)
                    continue;

                groupEvents.Add(selectedEvent);
                availableEvents.Remove(selectedEvent);
            }

            if (groupEvents.Count == 0)
            {
                previousEvents.Clear();
                return;
            }

            previousEvents.Clear();
            previousEvents.AddRange(groupEvents);

            groupEvents.ForEach(x => x.Enable());
        }
        else
        {
            var randomEvent = CustomGamemode.RegisteredObjects.GetRandomWeighted(y =>
            {
                if (!y.Value.CanActivateMidRound)
                    return 0f;

                if (previousEvents.Contains(y.Value))
                    return 0f;

                if (y.Value is RandomEventBase x)
                {
                    if (x.Weight <= 0f)
                        return 0f;

                    if (x.MinPlayers != null && ExPlayer.Count < x.MinPlayers)
                        return 0f;

                    if (x.MaxPlayers != null && ExPlayer.Count > x.MaxPlayers)
                        return 0f;

                    if (x.Weight >= 100f)
                        return 100f;

                    return x.Weight;
                }

                if (Config.Weights.TryGetValue(y.Value.Id, out var weight))
                {
                    if (weight <= 0f)
                        return 0f;

                    if (weight >= 100f)
                        return 100f;

                    return weight;
                }

                return 0f;
            });

            previousEvents.Clear();

            if (randomEvent.Value == null)
                return;

            previousEvents.Add(randomEvent.Value);

            randomEvent.Value.Enable();
        }
    }

    private static void Initialize()
    {
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "blackout.yml"), new BlackoutEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "downgrade.yml"), new DowngradeEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "boss_fight.yml"), new BossFightEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "midas_touch.yml"), new MidasTouchEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "random_size.yml"), new RandomSizeEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "random_name.yml"), new RandomNameEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "random_speed.yml"), new RandomSpeedEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "stolen_items.yml"), new StolenItemsEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "coin_madness.yml"), new CoinMadnessEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "no_teamkills.yml"), new NoTeamkillsEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "random_scale.yml"), new RandomScaleEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "scp_infection.yml"), new ScpInfectionEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "double_trouble.yml"), new DoubleTroubleEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "switched_spawns.yml"), new SwitchedSpawnsEvent(), true));
        Events.Add(FileUtils.LoadYamlFileOrDefault(FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "events", "its_raining_coins.yml"), new ItsRainingCoinsEvent(), true));

        Events.ForEach(e => e.Register());

        ExRoundEvents.Started += OnRoundStart;
    }
}