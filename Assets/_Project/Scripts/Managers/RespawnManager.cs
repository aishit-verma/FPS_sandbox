using System.Collections.Generic;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Objectives
{
    public static class RespawnManager
    {
        public static Vector3 GetSpawnPosition(int teamId)
        {
            var ownedPointIds = new List<int>();
            foreach (var point in CapturePoint.AllPoints)
            {
                if (point.OwningTeam == teamId) ownedPointIds.Add(point.PointId);
            }

            var candidates = new List<SpawnPoint>();

            if (ownedPointIds.Count > 0)
            {
                foreach (var sp in SpawnPoint.AllSpawnPoints)
                {
                    if (sp.TeamId == teamId && ownedPointIds.Contains(sp.LinkedPointId))
                    {
                        candidates.Add(sp);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                // No owned points (or none linked) — fall back to home base.
                foreach (var sp in SpawnPoint.AllSpawnPoints)
                {
                    if (sp.TeamId == teamId && sp.LinkedPointId == -1)
                    {
                        candidates.Add(sp);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[RESPAWN] No spawn points found for team {teamId}, defaulting to world origin");
                return Vector3.zero;
            }

            return candidates[Random.Range(0, candidates.Count)].transform.position;
        }
    }
}