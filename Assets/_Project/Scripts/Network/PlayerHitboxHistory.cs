using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Networking.Movement;
using PolyFrontlines.Utils.Prediction;

namespace PolyFrontlines.Gameplay.Weapons
{
    // Server-only: remembers where this player was at every recent tick,
    // so a shot referencing an old tick can rewind this player back to
    // where they actually were when the shooter saw them.
    public class PlayerHitboxHistory : NetworkBehaviour
    {
        private const int BufferCapacity = 256;

        private static readonly List<PlayerHitboxHistory> All = new List<PlayerHitboxHistory>();
        public static IReadOnlyList<PlayerHitboxHistory> AllTargets => All;

        private TickHistoryBuffer<Vector3> _history;

        private void Awake()
        {
            _history = new TickHistoryBuffer<Vector3>(BufferCapacity);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                All.Add(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;
            _history.Record(NetworkTick.Current, Vector3.zero, transform.position);
        }

        public bool TryGetPositionAtTick(int tick, out Vector3 position)
        {
            return _history.TryGet(tick, out _, out position);
        }
    }
}