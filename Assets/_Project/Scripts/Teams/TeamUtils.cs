using UnityEngine;

namespace PolyFrontlines.Gameplay.Team
{
    public static class TeamUtils
    {
        // True only if BOTH have a PlayerTeam and are on the same team.
        // Anything without a PlayerTeam is treated as not-a-teammate.
        public static bool AreSameTeam(Component a, Component b)
        {
            var teamA = a.GetComponentInParent<PlayerTeam>();
            var teamB = b.GetComponentInParent<PlayerTeam>();
            if (teamA == null || teamB == null) return false;
            return teamA.TeamId == teamB.TeamId;
        }

        public static int GetTeamId(Component c)
        {
            var team = c.GetComponentInParent<PlayerTeam>();
            return team != null ? team.TeamId : -1;
        }
    }
}