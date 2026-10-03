using LabApi.Events.Arguments.ServerEvents;
using LabExtended.API;
using LabExtended.Extensions;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;

namespace ObscurisCore.Features.RandomEvents.Events
{
    /// <summary>
    /// Represents a random event that swaps the spawn locations of the Chaos Insurgency and Nine-Tailed Fox factions at
    /// the start of a round or when a wave spawns.
    /// </summary>
    public class SwitchedSpawnsEvent : RandomEventBase
    {
        /// <inheritdoc/>
        public override string Id { get; } = "SwitchedSpawns";

        /// <inheritdoc/>
        public override bool CanActivateMidRound { get; set; } = true;

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            base.OnEnabled();

            ExPlayer.Players.ForEach(p => p.SendAlert(AlertType.Info, 10f, "Náhodné Eventy",
                "<b>Vypadá to že si <color=green>Chaos Insurgency</color> a <color=blue>Nine-Tailed Fox</color> " +
                "vyměnili spawn pozice!</b>"));
        }

        /// <inheritdoc/>
        public override void OnPlayerJoined(ExPlayer player)
        {
            base.OnPlayerJoined(player);

            player.SendAlert(AlertType.Info, 10f, "Náhodné Eventy",
                "<b>Vypadá to že si <color=green>Chaos Insurgency</color> a <color=blue>Nine-Tailed Fox</color> " +
                "vyměnili spawn pozice!</b>");
        }

        /// <inheritdoc/>
        public override void OnWaveSpawned(WaveRespawnedEventArgs args)
        {
            base.OnWaveSpawned(args);

            if (args.Wave.Faction is Faction.FoundationEnemy)
            {
                RoleTypeId[] chaosRoles = [RoleTypeId.ChaosRifleman, RoleTypeId.ChaosRepressor, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosConscript];

                args.Players.ForEach(p => p.Position = chaosRoles.GetRandomItem().GetSpawnPosition().position);
            }
            else if (args.Wave.Faction is Faction.FoundationStaff)
            {
                RoleTypeId[] ntfRoles = [RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain];

                args.Players.ForEach(p => p.Position = ntfRoles.GetRandomItem().GetSpawnPosition().position);
            }
        }   
    }
}