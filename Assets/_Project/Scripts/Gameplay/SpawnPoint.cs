using System.Collections.Generic;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Objectives
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private int teamId;
        [SerializeField] private int linkedPointId = -1; // -1 = home base spawn, not tied to a capture point

        public int TeamId => teamId;
        public int LinkedPointId => linkedPointId;

        private static readonly List<SpawnPoint> All = new List<SpawnPoint>();
        public static IReadOnlyList<SpawnPoint> AllSpawnPoints => All;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);
    }
}