using LabExtended.API;
using LabExtended.API.Hints;
using LabExtended.API.Settings;
using LabExtended.API.Settings.Entries.Buttons;

using LabExtended.Core;
using LabExtended.Events;

using LabExtended.Utilities.Update;
using System.Diagnostics;

using NiveraAPI.IO.Configs;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Features.Voting.API;
using ObscurisCore.Features.Voting.Overlay;

namespace ObscurisCore.Features.Voting
{
    /// <summary>
    /// Provides static methods and properties for managing voting sessions, including starting, stopping, and tracking
    /// the current vote within the application.
    /// </summary>
    public static class VoteManager
    {
        private static Stopwatch voteTimer = new();

        /// <summary>
        /// Gets the current vote information for the active voting session, if any.
        /// </summary>
        /// <remarks>This property is static and reflects the vote state for the entire application
        /// domain. If no voting session is active, the value is null.</remarks>
        public static VoteInfo? CurrentVote { get; private set; }

        /// <summary>
        /// Gets the maximum number of options allowed in a vote.
        /// </summary>
        [Config("voteManager", "maxOptions", "Maximum number of options allowed in a vote.")]
        public static int MaxOptions { get; set; } = 10;

        /// <summary>
        /// Gets the remaining time for the current vote, or zero if no vote is active.
        /// </summary>
        public static TimeSpan Remaining => CurrentVote == null ? TimeSpan.Zero : TimeSpan.FromSeconds((CurrentVote.Duration - voteTimer.Elapsed.TotalSeconds));

        /// <summary>
        /// Starts a new vote using the specified vote information if no vote is currently active.
        /// </summary>
        /// <remarks>If a vote is already active, this method does not start a new vote and returns false.
        /// When a vote is started, all players with an active vote menu will have their menus synchronized with the new
        /// vote.</remarks>
        /// <param name="vote">The vote information to use for starting the new vote. Cannot be null.</param>
        /// <returns>true if the vote was successfully started; otherwise, false if a vote is already in progress.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the vote parameter is null.</exception>
        public static bool StartVote(VoteInfo vote)
        {
            if (vote is null)
                throw new ArgumentNullException(nameof(vote));

            if (CurrentVote != null)
                return false;

            CurrentVote = vote;

            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub != null)
                {
                    if (!player.AddHintElement(new VoteElement()))
                        return false;

                    if (player.TryGetMenu<VoteMenu>(out var menu))
                        menu.SyncMenu(vote);
                }
            }

            voteTimer.Restart();
            return true;
        }

        /// <summary>
        /// Stops the current vote and notifies all players of the voting result.
        /// </summary>
        /// <remarks>If there is no active vote, this method does nothing. After stopping the vote, all
        /// players are informed of the winning option, if any, or that no option won. Any open vote menus are also
        /// synchronized to reflect the end of voting.</remarks>
        public static void StopVote()
        {
            if (CurrentVote == null)
                return;

            voteTimer.Stop();
            voteTimer.Reset();

            var winner = CurrentVote.GetWinner();

            if (winner != null)
            {
                ExPlayer.Players.ForEach(p =>
                {
                    p.SendAlert(AlertType.Info, 20f, "Vote System", $"<b>Hlasování vyhrála tato možnost</b>:\n<b><color=green>{winner}</color></b>", true);

                    if (p.TryGetMenu<VoteMenu>(out var menu))
                        menu.SyncMenu(null);

                    p.RemoveHintElement<VoteElement>();
                });
            }
            else
            {
                ExPlayer.Players.ForEach(p =>
                {
                    p.SendAlert(AlertType.Info, 20f, "Vote System", $"<b>Hlasování <color=red>nevyhrála</color> žádná možnost!</b>", true);

                    if (p.TryGetMenu<VoteMenu>(out var menu))
                        menu.SyncMenu(null);

                    p.RemoveHintElement<VoteElement>();
                });
            }

            CurrentVote = null;
        }

        private static void OnUpdate()
        {
            if (CurrentVote != null && voteTimer.Elapsed.TotalSeconds >= CurrentVote.Duration)
            {
                StopVote();
            }
        }

        private static void OnRestarting()
        {
            StopVote();
        }

        private static void OnVerified(ExPlayer player)
        {
            if (player.TryGetMenu<VoteMenu>(out var menu))
                menu.SyncMenu(CurrentVote);

            if (CurrentVote != null)
                player.AddHintElement(new VoteElement());
        }

        private static void OnLeft(ExPlayer player)
        {
            if (CurrentVote != null)
            {
                CurrentVote.Votes.Remove(player);
            }
        }

        internal static void OnButtonInteracted(SettingsButton button)
        {
            if (CurrentVote == null)
                return;

            if (button?.Player?.ReferenceHub == null)
                return;

            if (button.Menu is not VoteMenu voteMenu)
                return;

            if (!voteMenu.ButtonToOption.TryGetValue(button, out var option))
            {
                ApiLog.Error("VoteManager", $"No vote option for button label &3{button.Label}&r!");
                return;
            }

            CurrentVote.Votes[button.Player] = option;

            button.Player.SendAlert(AlertType.Info, 10f, "Vote System", $"<b>Hlasoval si pro <color=yellow>{option}</color>!</b>", true);
        }

        private static void Initialize()
        {
            PlayerUpdateHelper.OnLateUpdate += OnUpdate;

            ExPlayerEvents.Left += OnLeft;
            ExPlayerEvents.Verified += OnVerified;

            ExRoundEvents.Restarting += OnRestarting;

            SettingsManager.AddBuilder(new SettingsBuilder("secretlabapi.vote.builder").WithMenu(() => new VoteMenu()));
        }
    }
}