using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Core;
using PolyFrontlines.Gameplay.Team;
using PolyFrontlines.Gameplay.Player.LifeStates;

namespace PolyFrontlines.Gameplay.Objectives
{
    [RequireComponent(typeof(BoxCollider))]
    public class CapturePoint : NetworkBehaviour
    {
        [SerializeField] private int pointId;
        [SerializeField] private float captureTime = 10f; // seconds for 1 player to fully capture
        [SerializeField] private int maxEffectivePlayers = 2; // capture speed cap

        // -1 = neutral. Server-written, synced to everyone.
        private readonly NetworkVariable<int> _owningTeam =
            new NetworkVariable<int>(-1, writePerm: NetworkVariableWritePermission.Server);

        // -1..1: negative = progress toward team 0, positive = toward team 1.
        private readonly NetworkVariable<float> _captureProgress =
            new NetworkVariable<float>(0f, writePerm: NetworkVariableWritePermission.Server);

        public int PointId => pointId;
        public int OwningTeam => _owningTeam.Value;

        private static readonly List<CapturePoint> All = new List<CapturePoint>();
        public static IReadOnlyList<CapturePoint> AllPoints => All;

        private readonly HashSet<PlayerTeam> _playersInside = new HashSet<PlayerTeam>();

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            _owningTeam.OnValueChanged += OnOwnershipChanged;
            _captureProgress.OnValueChanged += OnProgressChanged;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            _owningTeam.OnValueChanged -= OnOwnershipChanged;
            _captureProgress.OnValueChanged -= OnProgressChanged;
        }

        private void OnOwnershipChanged(int previous, int current)
        {
            EventBus<CaptureProgressEvent>.Publish(new CaptureProgressEvent(pointId, current, 1f));
            Debug.Log($"[CAPTURE] Point {pointId} owner changed: {previous} -> {current}");
        }

        private void OnProgressChanged(float previous, float current)
        {
            EventBus<CaptureProgressEvent>.Publish(new CaptureProgressEvent(pointId, _owningTeam.Value, current));
            Debug.Log($"[CAPTURE] Point {pointId} progress: {current:F2}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            var team = other.GetComponentInParent<PlayerTeam>();
            if (team != null) _playersInside.Add(team);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;
            var team = other.GetComponentInParent<PlayerTeam>();
            if (team != null) _playersInside.Remove(team);
        }

        private void Update()
        {
            if (!IsServer) return;
            if (Match.MatchStateController.Instance != null &&
                Match.MatchStateController.Instance.CurrentState != Match.MatchStateController.MatchStateId.Live)
            {
                return; // captures only progress while the match is Live
            }

            CountPlayers(out int team0, out int team1);

            if (team0 > 0 && team1 > 0) return;       // contested: freeze
            if (team0 == 0 && team1 == 0) return;     // empty: freeze (no decay)

            int capturingTeam = team0 > 0 ? 0 : 1;
            int effectivePlayers = Mathf.Min(team0 + team1, maxEffectivePlayers);

            // Direction: team 0 pulls toward -1, team 1 toward +1.
            float direction = capturingTeam == 0 ? -1f : 1f;
            float rate = effectivePlayers / captureTime;

            float newProgress = Mathf.Clamp(_captureProgress.Value + direction * rate * Time.deltaTime, -1f, 1f);
            _captureProgress.Value = newProgress;

            if (newProgress <= -1f && _owningTeam.Value != 0)
            {
                _owningTeam.Value = 0;
            }
            else if (newProgress >= 1f && _owningTeam.Value != 1)
            {
                _owningTeam.Value = 1;
            }
        }

        private void CountPlayers(out int team0, out int team1)
        {
            team0 = 0;
            team1 = 0;

            foreach (var player in _playersInside)
            {
                if (player == null) continue;

                // Downed/dead players shouldn't hold or capture points.
                var lifeState = player.GetComponent<PlayerLifeState>();
                if (lifeState != null && lifeState.CurrentVisibleState != PlayerLifeState.LifeStateId.Alive)
                {
                    continue;
                }

                if (player.TeamId == 0) team0++;
                else if (player.TeamId == 1) team1++;
            }
        }
    }
}