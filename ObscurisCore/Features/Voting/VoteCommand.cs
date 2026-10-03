using LabExtended.Commands;

using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using ObscurisCore.Features.Voting.API;

namespace ObscurisCore.Features.Voting
{
    /// <summary>
    /// Provides commands for managing voting sessions, including starting and stopping votes, on the server.
    /// </summary>
    /// <remarks>This command is intended for server-side use and allows users to initiate or terminate voting
    /// sessions. Only one vote can be active at a time. Users can only stop votes that they have started.</remarks>
    [Command("vote", "Manages voting sessions.")]
    public class VoteCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("stop", "Stops the current vote.", "vote.stop")]
        private void Stop()
        {
            if (VoteManager.CurrentVote == null)
            {
                Fail("Neprobíhá žádné hlasování!");
                return;
            }

            if (VoteManager.CurrentVote.Starter != Sender)
            {
                Fail("Nemůžeš zastavit hlasování, které jsi nespustil!");
                return;
            }

            VoteManager.StopVote();

            Ok("Hlasování bylo zastaveno.");

        }

        [CommandOverload("start", "Starts a new vote.", "vote.start")]
        private void Start(
            [CommandParameter("Duration", "The duration of the vote (in seconds).")] int duration,
            [CommandParameter("Title", "The title of the vote.")] string title)
        {
            if (VoteManager.CurrentVote != null)
            {
                Fail($"A vote is already in progress: {VoteManager.CurrentVote.Title}");
                return;
            }

            var info = new VoteInfo();

            info.Title = title;
            info.Duration = duration;

            info.Starter = Sender;

            void OnInput(string value)
            {
                if (string.Equals(value, "start", StringComparison.OrdinalIgnoreCase))
                {
                    if (info.Options.Count < 2)
                    {
                        Read("Nespecifikoval si dostatek možností!\n" +
                            "Přidej alespoň dvě možnosti a zkus to znova.", OnInput);
                        return;
                    }

                    if (VoteManager.StartVote(info))
                    {
                        Ok($"Hlasování '{info.Title}' bylo spuštěno na {info.Duration} sekund!");
                    }
                    else
                    {
                        Fail("Nepodařilo se spustit hlasování!");
                    }

                    return;
                }

                if (info.Options.Count >= VoteManager.MaxOptions)
                {
                    Read($"Dosáhl jsi maximálního počtu možností ({VoteManager.MaxOptions}).\n" +
                        "Napiš start pro spuštění hlasování!", OnInput);
                    return;
                }

                if (info.Options.Any(x => x == value))
                {
                    Read($"Možnost '{value}' již existuje!\n" +
                        "Zadej jinou možnost nebo start pro spuštění hlasování!", OnInput);
                    return;
                }

                info.Options.Add(value);

                Read($"Přidána možnost '{value}'.\n" +
                    $"Napiš další možnost nebo start pro spuštění hlasování!", OnInput);
            }

            Read($"Vytváření hlasování '{title}' na {duration} sekund!\n" +
                $"Napiš buď název možnosti pro kterou lze hlasovat nebo start pro spuštění hlasování!", OnInput);
        }
    }
}