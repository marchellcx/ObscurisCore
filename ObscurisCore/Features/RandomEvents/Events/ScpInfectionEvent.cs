using System.ComponentModel;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabExtended.API;
using LabExtended.Extensions;
using MapGeneration;
using ObscurisCore.Extensions;
using ObscurisCore.Features.Elements.Alerts;
using PlayerRoles;

namespace ObscurisCore.Features.RandomEvents.Events
{
    /// <summary>
    /// Represents the SCP Infection random event, in which one player becomes an SCP and others attempt to escape while
    /// avoiding infection.
    /// </summary>
    public class ScpInfectionEvent : RandomEventBase
    {
        /// <inheritdoc/>
        public override string Id { get; } = "ScpInfection";

        /// <inheritdoc/>
        public override bool CanActivateMidRound { get; set; } = true;

        /// <inheritdoc/>
        public override bool PreventWaveSpawns { get; set; } = true;

        /// <inheritdoc/>
        public override bool CanBeGrouped { get; set; } = false;

        /// <inheritdoc/>
        public override int? MinPlayers { get; set; } = 2;

        /// <summary>
        /// Gets or sets the collection of role identifiers available for infection scenarios.
        /// </summary>
        [Description("A list of roles that can be used for infection.")]
        public RoleTypeId[] Roles { get; set; } = TeamExtensions.ScpRoles.ToArray();

        /// <summary>
        /// Gets or sets the amount of experience points awarded to a player for escaping.
        /// </summary>
        [Description("The amount of XP to reward a player for escaping.")]
        public int EscapeExpReward { get; set; } = 200;

        /// <summary>
        /// Gets or sets the amount of experience points awarded to SCPs for killing a player.
        /// </summary>
        [Description("The amount of XP to reward the SCPs for killing a player.")]
        public int KillExpReward { get; set; } = 20;

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            base.OnEnabled();

            var infectRole = Roles.GetRandomItem();
            var patientZero = ExPlayer.Players.GetRandomItem(p => p.ReferenceHub != null && !string.IsNullOrEmpty(p.UserId));

            patientZero.Role.Set(infectRole, RoleChangeReason.RoundStart, RoleSpawnFlags.UseSpawnpoint);

            var patientZeroSpawnPos = Map.Rooms
                .GetRandomItem(r => r != null && r.Zone == FacilityZone.LightContainment && r.Name != RoomName.Pocket).Base
                .GetSafePosition(patientZero);

            var validPlayers = ExPlayer.Players.Where(p => p.ReferenceHub != null && !string.IsNullOrEmpty(p.UserId) && p != patientZero);
            var validPlayer = validPlayers.First();

            foreach (var player in validPlayers)
                player.Role.Set(RoleTypeId.ClassD, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.UseSpawnpoint);

            var startPos = Map.Rooms
                .GetRandomItem(r => r != null && r.Zone == FacilityZone.LightContainment && r.Name != RoomName.Pocket).Base
                .GetSafePosition(validPlayer);

            while (startPos == patientZeroSpawnPos)
            {
                startPos = Map.Rooms
                    .GetRandomItem(r => r != null && r.Zone == FacilityZone.LightContainment && r.Name != RoomName.Pocket).Base
                    .GetSafePosition(validPlayer);
            }

            foreach (var player in validPlayers)
                player.Position.Position = startPos;

            patientZero.Position.Position = patientZeroSpawnPos;

            ExPlayer.Players.ForEach(p =>
            {
                p.SendAlert(AlertType.Info, 10f, "Náhodné Eventy",
                    $"<b>Začal event <color=yellow>SCP Infekce</color></b>!\n" +
                    $"<b>Po smrti se změníte ve stejné SCP jako pacient nula (<color=red>{infectRole.GetName()}</color> <color=orange>{patientZero.Nickname}</color>)</b>\n" +
                    $"<b>Vaším cílem je utéct, můžete brát pouze karty, dveře na karty jsou permanentně otevřené!</b>\n" +
                    $"<b>Jediný způsob smrti je přes SCP, nic jiného vás nezabije - ani Tesla!</b>\n" +
                    $"<b>Upozornění - SCP stačí pouze jeden hit aby vás proměnili.</b>", true);
            });

            Map.Doors.ForEach(d =>
            {
                if (d.Permissions != DoorPermissionFlags.None)
                {
                    d.IsOpened = true;
                    d.Lock(DoorLockReason.AdminCommand, true);
                }
            });

            PlayerEvents.Hurting += OnHurting;
            PlayerEvents.Escaped += OnEscaped;
            PlayerEvents.PickingUpItem += OnPickingUpItem;
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            base.OnDisabled();

            PlayerEvents.Hurting -= OnHurting;
            PlayerEvents.Escaped -= OnEscaped;
            PlayerEvents.PickingUpItem -= OnPickingUpItem;
        }

        private void OnEscaped(PlayerEscapedEventArgs args)
        {
            if (!IsActive)
                return;

            if (args.Player is not ExPlayer player)
                return;

            player.Role.Set(RoleTypeId.Spectator, RoleChangeReason.Escaped, RoleSpawnFlags.None);

            /*
            if (EscapeExpReward > 0)
            {
                player.SendAlert(AlertType.Info, 10f, "Náhodné Eventy", $"<b>Za úspěšný útěk dostáváš <color=yellow>{EscapeExpReward} XP</color>!</b>");
                player.AddExperience("Scp Infection Escape", EscapeExpReward);
            }
            */
        }

        private void OnHurting(PlayerHurtingEventArgs args)
        {
            if (!IsActive)
                return;

            args.IsAllowed = false;

            if (args.Attacker is ExPlayer attacker
                && attacker?.ReferenceHub != null
                && attacker.Role.IsScp
                && args.Player is ExPlayer target)
            {
                /*
                if (KillExpReward > 0)
                    attacker.AddExperience("Scp Infection Kill", KillExpReward);
                */

                target.Role.Set(attacker.Role.Type, RoleChangeReason.Died, RoleSpawnFlags.None);
                return;
            }
        }

        private void OnPickingUpItem(PlayerPickingUpItemEventArgs args)
        {
            if (!IsActive)
                return;

            if (args.Pickup?.Base == null)
                return;

            if (args.Pickup.Base.Info.ItemId.TryGetItemPrefab(out var prefab) && prefab.Category is ItemCategory.Keycard)
                return;

            args.IsAllowed = false;

            if (args.Player is ExPlayer player)
                player.SendAlert(AlertType.Warn, 5f, "Scp Infection", "<b>Můžeš brát pouze karty!</b>", true);
        }
    }
}