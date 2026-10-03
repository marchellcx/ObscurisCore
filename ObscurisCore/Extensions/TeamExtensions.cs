using LabExtended.Extensions;
using PlayerRoles;

namespace ObscurisCore.Extensions
{
    /// <summary>
    /// Provides extension methods and role groupings for working with teams and roles within the application.
    /// </summary>
    /// <remarks>The TeamExtensions class includes predefined arrays of role type identifiers for common team
    /// groupings, such as Chaos, Nine-Tailed Fox (NTF), and SCPs. It also provides extension methods to facilitate
    /// operations like selecting a random role for a given team. Use these members to simplify team and role management
    /// tasks, such as determining group membership or assigning roles based on team affiliation.</remarks>
    public static class TeamExtensions
    {
        /// <summary>
        /// Gets the set of role type identifiers representing all Chaos roles.
        /// </summary>
        /// <remarks>This array includes all roles classified as Chaos, such as Chaos Marauder, Chaos
        /// Repressor, Chaos Rifleman, and Chaos Conscript. Use this collection to check if a given role is part of the
        /// Chaos faction.</remarks>
        public static RoleTypeId[] ChaosRoles = [RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosConscript];

        /// <summary>
        /// Defines the set of role type identifiers that represent members of the Nine-Tailed Fox (NTF) unit.
        /// </summary>
        /// <remarks>This array includes all role types considered part of the NTF team, such as NTF
        /// Private, Sergeant, Specialist, Captain, and Facility Guard. Use this array to check if a given role belongs
        /// to the NTF group.</remarks>
        public static RoleTypeId[] NtfRoles = [RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain, RoleTypeId.FacilityGuard];

        /// <summary>
        /// Defines the set of role type identifiers that represent SCP entities.
        /// </summary>
        /// <remarks>This array includes all role types considered SCPs within the application. The order
        /// of elements may be significant for certain operations. Duplicate entries may be present.</remarks>
        public static RoleTypeId[] ScpRoles = [RoleTypeId.Scp049, RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173, RoleTypeId.Scp939, RoleTypeId.Scp3114, RoleTypeId.Scp049];

        /// <summary>
        /// Selects a random role from a randomly chosen team, excluding any specified roles.
        /// </summary>
        /// <param name="teams">The collection of teams from which to select a random team. Must contain at least one team.</param>
        /// <param name="excludedRoles">An array of roles to exclude from selection. If specified, these roles will not be considered when choosing
        /// a random role.</param>
        /// <returns>A randomly selected role from one of the teams, excluding any roles specified in <paramref
        /// name="excludedRoles"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="teams"/> is null or contains no teams.</exception>
        public static RoleTypeId GetRandomRole(this IEnumerable<Team> teams, params RoleTypeId[] excludedRoles)
        {
            if (teams == null || !teams.Any())
                throw new ArgumentException("The teams collection must contain at least one team.", nameof(teams));

            var team = teams.GetRandomItem();
            return team.GetRandomRole(excludedRoles);
        }

        /// <summary>
        /// Selects a random role associated with the specified team, excluding any roles provided.
        /// </summary>
        /// <param name="team">The team for which to select a random role.</param>
        /// <param name="exludedRoles">An array of roles to exclude from selection. If all roles for the team are excluded, an exception is thrown.</param>
        /// <returns>A randomly selected role for the specified team that is not in the excluded roles list.</returns>
        /// <exception cref="InvalidOperationException">Thrown if all possible roles for the specified team are excluded, leaving no available roles to select.</exception>
        /// <exception cref="NotImplementedException">Thrown if the specified team is not supported by this method.</exception>
        public static RoleTypeId GetRandomRole(this Team team, params RoleTypeId[] exludedRoles)
        {
            switch (team)
            {
                case Team.ChaosInsurgency:
                    {
                        if (ChaosRoles.All(r => exludedRoles.Contains(r)))
                            throw new InvalidOperationException("No available roles to select from for Chaos Insurgency.");

                        var role = ChaosRoles.RandomItem();

                        while (exludedRoles.Contains(role))
                            role = ChaosRoles.RandomItem();

                        return role;
                    }

                case Team.FoundationForces:
                    {
                        if (NtfRoles.All(r => exludedRoles.Contains(r)))
                            throw new InvalidOperationException("No available roles to select from for Foundation Forces.");

                        var role = NtfRoles.RandomItem();

                        while (exludedRoles.Contains(role))
                            role = NtfRoles.RandomItem();

                        return role;
                    }

                case Team.SCPs:
                    {
                        if (ScpRoles.All(r => exludedRoles.Contains(r)))
                            throw new InvalidOperationException("No available roles to select from for SCPs.");

                        var role = ScpRoles.RandomItem();

                        while (exludedRoles.Contains(role))
                            role = ScpRoles.RandomItem();

                        return role;
                    }

                case Team.ClassD: 
                    return exludedRoles.Contains(RoleTypeId.ClassD) 
                        ? throw new InvalidOperationException($"No available roles to select from for Class D.") 
                        : RoleTypeId.ClassD;

                case Team.Dead:
                    return exludedRoles.Contains(RoleTypeId.Spectator) 
                        ? throw new InvalidOperationException($"No available roles to select from for Dead.") 
                        : RoleTypeId.Spectator;

                case Team.OtherAlive:
                    return exludedRoles.Contains(RoleTypeId.Tutorial) 
                        ? throw new InvalidOperationException($"No available roles to select from for Other Alive.") 
                        : RoleTypeId.Tutorial;

                case Team.Scientists: 
                    return exludedRoles.Contains(RoleTypeId.Scientist)
                        ? throw new InvalidOperationException($"No available roles to select from for Scientists.") 
                        : RoleTypeId.Scientist;

                default: throw new NotImplementedException(team.ToString());
            }
        }
    }
}