using LabExtended.API;

using LabExtended.Utilities;
using LabExtended.Extensions;

using NorthwoodLib.Pools;

using PlayerRoles;

namespace ObscurisCore.Utilities
{
    /// <summary>
    /// Provides static methods for selecting valid players and assigning roles based on spawn range criteria.
    /// </summary>
    public static class SpawnSelector
    {
        /// <summary>
        /// Selects a valid set of players from the specified source collection based on the provided spawn range and
        /// adds them to the target list.
        /// </summary>
        /// <remarks>The number of players selected depends on the values of <paramref name="range"/>. If
        /// the source contains fewer players than required, all available players are added. The method may perform
        /// random selection when the spawn range specifies a range of possible counts.</remarks>
        /// <param name="range">The spawn range criteria used to determine the number of players to select. If <paramref name="range"/> is
        /// <see langword="null"/>, no players are selected and the method returns <see langword="false"/>.</param>
        /// <param name="source">The collection of candidate players to select from. Cannot be <see langword="null"/>.</param>
        /// <param name="target">The list to which the selected valid players will be added. Cannot be <see langword="null"/>.</param>
        /// <returns>true if a valid set of players was selected and added to the target list; otherwise, false if the spawn
        /// range is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        public static bool GetValidPlayers(this SpawnRange? range, IEnumerable<ExPlayer> source, List<ExPlayer> target)
        {
            if (range == null)
                return false;

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var playerCount = 0;
            var sourceCount = source.Count();

            if (range.MinSpawnCount > 0 && range.MaxSpawnCount > 0)
            {
                if (range.MinSpawnCount == range.MaxSpawnCount)
                {
                    playerCount = range.MinSpawnCount;
                }
                else
                {
                    playerCount = UnityEngine.Random.Range(range.MinSpawnCount, range.MaxSpawnCount);
                }
            }
            else
            {
                if (range.MinSpawnCount > 0)
                {
                    playerCount = sourceCount;
                }
                else
                {
                    playerCount = range.MaxSpawnCount;
                }
            }

            if (sourceCount <= playerCount)
            {
                target.AddRange(source);
                return true;
            }

            // yes yes very expensive called once per spawn but who cares
            while (target.Count < playerCount && source.Any(x => !target.Contains(x)))
            {
                var randomPlayer = source.GetRandomItem(x => !target.Contains(x));

                if (randomPlayer?.ReferenceHub == null)
                    continue;

                target.Add(randomPlayer);
            }

            return true;
        }

        /// <summary>
        /// Returns the first spawn range from the collection that is valid based on player count and chance
        /// constraints, or null if no valid range is found.
        /// </summary>
        /// <remarks>A spawn range is considered valid if the current player count meets the range's
        /// minimum and maximum player requirements, and the overall chance condition is satisfied. The method evaluates
        /// ranges in order and returns the first valid one.</remarks>
        /// <param name="ranges">A collection of spawn ranges to evaluate for validity. Cannot be null.</param>
        /// <returns>A valid <see cref="SpawnRange"/> if one exists in the collection; otherwise, null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranges"/> is null.</exception>
        public static SpawnRange? GetValidRange(this IEnumerable<SpawnRange> ranges)
        {
            if (ranges is null)
                throw new ArgumentNullException(nameof(ranges));

            foreach (var range in ranges)
            {
                if (range.MinPlayers > 0 && ExPlayer.Count < range.MinPlayers)
                    continue;

                if (range.MaxPlayers > 0 && ExPlayer.Count > range.MaxPlayers)
                    continue;

                if (range.OverallChance <= 0f || (range.OverallChance < 100f && !WeightUtils.GetBool(range.OverallChance)))
                    continue;

                return range;
            }

            return null;
        }

        /// <summary>
        /// Modifies the roles assigned to players in the dictionary based on the specified spawn range and role
        /// selection function.
        /// </summary>
        /// <remarks>Players are selected for modification based on the spawn range and optional
        /// predicate. Only players matching these criteria will have their roles updated.</remarks>
        /// <param name="roles">The dictionary containing player-role assignments to be modified. Cannot be null.</param>
        /// <param name="spawnRange">The spawn range criteria used to select which players' roles will be modified. If null, no changes are made.</param>
        /// <param name="getRole">A function that determines the new role to assign to each selected player.</param>
        /// <param name="predicate">An optional predicate used to filter which players are considered for modification. If null, all players in
        /// the dictionary are considered.</param>
        /// <returns>true if at least one player's role was modified; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the roles dictionary is null.</exception>
        public static bool ModifySpawnRoles(this Dictionary<ExPlayer, RoleTypeId> roles, SpawnRange? spawnRange, Func<ExPlayer, RoleTypeId> getRole, Predicate<ExPlayer>? predicate = null)
        {
            if (roles is null)
                throw new ArgumentNullException(nameof(roles));

            if (spawnRange == null)
                return false;

            var source = predicate == null 
                ? roles.Keys
                : roles.Keys.Where(x => predicate(x));

            var players = ListPool<ExPlayer>.Shared.Rent();

            spawnRange.GetValidPlayers(source, players);

            if (players.Count < 1)
            {
                ListPool<ExPlayer>.Shared.Return(players);
                return false;
            }

            for (var x = 0; x < players.Count; x++)
                roles[players[x]] = getRole(players[x]);
        
            ListPool<ExPlayer>.Shared.Return(players); 
            return true;
        }

        /// <summary>
        /// Assigns roles to players within the specified spawn range, optionally filtering the players, and returns a
        /// value indicating whether any roles were set.
        /// </summary>
        /// <remarks>If no players are eligible within the specified spawn range, no roles are assigned
        /// and the method returns false.</remarks>
        /// <param name="players">The collection of players to consider for role assignment. Cannot be null.</param>
        /// <param name="spawnRange">The spawn range used to determine which players are eligible for role assignment. If null, no roles are set.</param>
        /// <param name="setRole">The action to perform for each eligible player to assign their role.</param>
        /// <param name="predicate">An optional filter to select which players from the collection are considered for role assignment. If null,
        /// all players are considered.</param>
        /// <returns>true if at least one player's role was set; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the players collection is null.</exception>
        public static bool SetRoles(this IEnumerable<ExPlayer> players, SpawnRange? spawnRange, Action<ExPlayer> setRole, Predicate<ExPlayer>? predicate = null)
        {
            if (players is null)
                throw new ArgumentNullException(nameof(players));

            if (spawnRange == null)
                return false;

            var source = predicate == null
                ? players
                : players.Where(x => predicate(x));

            var targets = ListPool<ExPlayer>.Shared.Rent();

            spawnRange.GetValidPlayers(source, targets);

            if (targets.Count < 1)
            {
                ListPool<ExPlayer>.Shared.Return(targets);
                return false;
            }

            for (var x = 0; x < targets.Count; x++)
                setRole(targets[x]);

            ListPool<ExPlayer>.Shared.Return(targets);
            return true;
        }

        /// <summary>
        /// Assigns roles to the specified players based on a valid spawn range and an optional predicate.
        /// </summary>
        /// <param name="ranges">A collection of spawn ranges used to determine valid role assignments. Cannot be null.</param>
        /// <param name="players">The collection of players to whom roles will be assigned.</param>
        /// <param name="setRole">An action delegate that assigns a role to a player. This is invoked for each player that meets the criteria.</param>
        /// <param name="predicate">An optional predicate used to filter which players should have roles assigned. If null, all players are
        /// considered.</param>
        /// <returns>true if roles were successfully assigned to at least one player; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranges"/> is null.</exception>
        public static bool SetRoles(this IEnumerable<SpawnRange> ranges, IEnumerable<ExPlayer> players, Action<ExPlayer> setRole, Predicate<ExPlayer>? predicate = null)
        {
            if (ranges == null)
                throw new ArgumentNullException(nameof(ranges));

            var range = ranges.GetValidRange();

            if (range == null)
                return false;

            return players.SetRoles(range, setRole, predicate);
        }
    }
}