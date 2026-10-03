using LabExtended.API;
using LabExtended.Extensions;

using UnityEngine;

namespace ObscurisCore.Features.Voting.API
{
    /// <summary>
    /// Represents the state of a voting session, including available options and votes cast by players.
    /// </summary>
    public class VoteInfo
    {
        /// <summary>
        /// Gets or sets the player who started the vote.
        /// </summary>
        public ExPlayer Starter { get; set; } = ExPlayer.Host;

        /// <summary>
        /// Gets the collection of available vote options.
        /// </summary>
        public List<string> Options { get; } = new();

        /// <summary>
        /// Gets the collection of votes cast by players.
        /// </summary>
        /// <remarks>Each entry in the dictionary maps an individual player to their selected vote option.
        /// The collection is read-only; to modify votes, use the appropriate methods provided by the class.</remarks>
        public Dictionary<ExPlayer, string> Votes { get; } = new();

        /// <summary>
        /// Gets or sets the duration of the vote.
        /// </summary>
        public int Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets the title of the vote.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Sets the title for this vote and returns the updated instance.
        /// </summary>
        /// <param name="title">The title to assign to the vote. Cannot be null.</param>
        /// <returns>The current instance of <see cref="VoteInfo"/> with the updated title.</returns>
        public VoteInfo WithTitle(string title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        /// Sets the duration for the vote and returns the updated instance.
        /// </summary>
        /// <param name="duration">The duration of the vote, in seconds. Must be a non-negative integer.</param>
        /// <returns>The current <see cref="VoteInfo"/> instance with the updated duration.</returns>
        public VoteInfo WithDuration(int duration)
        {
            Duration = duration;
            return this;
        }

        /// <summary>
        /// Adds the specified option to the vote if it does not already exist, and returns the current instance for
        /// method chaining.
        /// </summary>
        /// <remarks>If an option with the specified name already exists (case-insensitive), no new option
        /// is added. This method enables fluent configuration by returning the same instance.</remarks>
        /// <param name="option">The name of the option to add. Comparison is case-insensitive.</param>
        /// <returns>The current <see cref="VoteInfo"/> instance with the specified option ensured.</returns>
        public VoteInfo WithOption(string option)
        {
            Options.AddUnique(option);
            return this;
        }

        /// <summary>
        /// Sets the starter player for the vote and returns the updated instance.
        /// </summary>
        /// <param name="player">The player to set as the starter. If null, the host player is used as the default.</param>
        /// <returns>The current instance of <see cref="VoteInfo"/> with the starter player set.</returns>
        public VoteInfo WithStarter(ExPlayer? player)
        {
            Starter = player ?? ExPlayer.Host;
            return this;
        }

        /// <summary>
        /// Gets the percentage value associated with the specified option name.
        /// </summary>
        /// <param name="optionName">The name of the option for which to retrieve the percentage. Comparison is case-insensitive.</param>
        /// <returns>The percentage value for the specified option if found; otherwise, 0.</returns>
        public int GetPercentage(string optionName)
        {
            if (Votes.Count == 0)
                return 0;

            var count = Votes.Count(kvp => kvp.Value == optionName);
            return Mathf.CeilToInt(((float)count / (float)Votes.Count) * 100);
        }

        /// <summary>
        /// Determines the winning option based on the current votes, if there is a clear winner.
        /// </summary>
        /// <remarks>If two or more options are tied for the highest number of votes, the method returns
        /// null to indicate that there is no single winner.</remarks>
        /// <returns>The option that has received the most votes, or null if there is a tie or no votes have been cast.</returns>
        public string? GetWinner()
        {
            if (Votes.Count == 0)
                return null;

            var first = Options
                .OrderByDescending(opt => Votes.Count(kvp => kvp.Value == opt))
                .First();
            var firstVotes = Votes.Count(kvp => kvp.Value == first);

            for (var x = 0; x < Options.Count; x++)
            {
                var option = Options[x];
                var votes = Votes.Count(kvp => kvp.Value == option);

                if (votes < firstVotes)
                    continue;

                if (votes == firstVotes && option != first)
                    return null;
            }

            return first;
        }

        /// <summary>
        /// Determines whether the specified name matches the current winner, using a case-insensitive comparison.
        /// </summary>
        /// <param name="name">The name to compare with the current winner. The comparison is case-insensitive.</param>
        /// <returns>true if the specified name matches the winner's name; otherwise, false.</returns>
        public bool IsWinner(string name)
        {
            var winner = GetWinner();
            return winner != null && winner.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}