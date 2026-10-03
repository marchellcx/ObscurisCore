using LabExtended.API.Settings.Menus;

using LabExtended.API.Settings.Entries;
using LabExtended.API.Settings.Entries.Buttons;

namespace ObscurisCore.Features.Voting.API
{
    /// <summary>
    /// Represents a settings menu for voting, allowing users to select from multiple voting options within the
    /// application.
    /// </summary>
    /// <remarks>The VoteMenu dynamically updates its entries based on the provided voting information. It is
    /// typically used in scenarios where users need to cast votes for one of several options. The menu is hidden if no
    /// voting information is available.</remarks>
    public class VoteMenu : SettingsMenu
    {
        /// <summary>
        /// Gets the unique identifier for the custom vote menu integration.
        /// </summary>
        public override string CustomId { get; } = "secretlabapi.votemenu";

        /// <summary>
        /// Gets the localized header text for the voting section.
        /// </summary>
        public override string Header { get; } = "<color=yellow>🔔</color> | <color=red>Hlasování</color>";

        /// <summary>
        /// Gets the priority value for this instance.
        /// </summary>
        public override int Priority { get; } = int.MaxValue;

        /// <summary>
        /// Gets a mapping between settings buttons and their corresponding vote options.
        /// </summary>
        /// <remarks>This dictionary provides the association used to determine which vote option is
        /// triggered by each settings button. The collection is read-only; to modify the mapping, use the appropriate
        /// methods or initialization logic.</remarks>
        public Dictionary<SettingsButton, string> ButtonToOption { get; } = new();

        /// <summary>
        /// Synchronizes the menu display to reflect the provided voting options.
        /// </summary>
        /// <remarks>This method updates the menu entries to match the options in the specified vote. If
        /// the number of options is less than the maximum allowed, unused menu entries are hidden.</remarks>
        /// <param name="vote">The voting information to display in the menu. If null, the menu is hidden.</param>
        public void SyncMenu(VoteInfo? vote)
        {
            if (vote is null)
            {
                HideMenu();
                return;
            }

            ButtonToOption.Clear();

            for (var x = 0; x < VoteManager.MaxOptions; x++)
            {
                var button = Entries[x] as SettingsButton;

                if (button is null)
                    continue;

                if (x >= vote.Options.Count)
                {
                    button.IsHidden = true;
                    continue;
                }

                button.IsHidden = false;
                button.OnTriggered = VoteManager.OnButtonInteracted;

                ButtonToOption[button] = vote.Options[x];
            }

            ShowMenu();
        }

        /// <summary>
        /// Populates the provided settings list with voting option buttons for each available vote option.
        /// </summary>
        /// <remarks>Each button added represents a voting option and allows users to cast a vote for that
        /// option. The number of buttons created corresponds to the value of VoteManager.MaxOptions.</remarks>
        /// <param name="settings">The list to which voting option buttons are added. Must not be null.</param>
        public override void BuildMenu(List<SettingsEntry> settings)
        {
            for (var x = 0; x < VoteManager.MaxOptions; x++)
            {
                settings.Add(SettingsButton.Create(
                    $"secretlabapi.votemenu.buttons.{x}",
                    $"Možnost {x + 1}",
                    "<color=green>📥 Hlasovat</color>",
                    "Stisknutím tohoto tlačítka hlasuješ pro danou možnost."));
            }
        }
    }
}