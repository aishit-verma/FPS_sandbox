using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Utils.Prediction;

namespace PolyFrontlines.Networking.Movement
{
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float correctionThreshold = 0.05f;
        [SerializeField] private float interpolationDelay = 0.1f; // seconds rendered in the past, for remote players

        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }

        [Header("Testing Only")]
        [SerializeField] private bool simulateAutoMove = false;
        [SerializeField] private float autoMoveSpeed = 1f;

        private const int BufferCapacity = 256;

        private struct ServerState : INetworkSerializeByMemcpy
        {
            public int Tick;
            public Vector3 Position;
            public float Yaw;
        }

        private struct Snapshot
        {
            public float ReceivedAt;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private readonly NetworkVariable<ServerState> _serverState =
            new NetworkVariable<ServerState>(writePerm: NetworkVariableWritePermission.Server);

        private TickHistoryBuffer<Vector3> _history;
        private Vector3 _authoritativePosition;

        // Only used by remote viewers (not owner, not server) to smooth movement.
        private readonly List<Snapshot> _snapshots = new List<Snapshot>();

        private void Awake()
        {
            _history = new TickHistoryBuffer<Vector3>(BufferCapacity);
        }

        public override void OnNetworkSpawn()
        {
            transform.position = _serverState.Value.Position;
            _authoritativePosition = _serverState.Value.Position;

            if (IsOwner)
            {
                _serverState.OnValueChanged += OnServerStateChanged;
            }
            else if (!IsServer)
            {
                _serverState.OnValueChanged += OnSnapshotReceived;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _serverState.OnValueChanged -= OnServerStateChanged;
            }
            else if (!IsServer)
            {
                _serverState.OnValueChanged -= OnSnapshotReceived;
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            int tick = NetworkTick.Current;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            if (simulateAutoMove)
            {
                h = Mathf.Sin(Time.time * autoMoveSpeed);
                v = 0f;
            }

            Vector3 localInput = new Vector3(h, 0f, v);
            if (localInput.sqrMagnitude > 1f) localInput.Normalize();

            // W/A/S/D relative to current facing, not fixed world axes.
            Vector3 input = transform.right * localInput.x + transform.forward * localInput.z;

            Vector3 predictedPosition = transform.position + input * moveSpeed * NetworkTick.TickInterval;
            transform.position = predictedPosition;

            _history.Record(tick, input, predictedPosition);

            SubmitMovementServerRpc(tick, input, transform.eulerAngles.y);
        }

        private void Update()
        {
            if (IsOwner) return;

            if (IsServer)
            {
                transform.position = _serverState.Value.Position;
                transform.rotation = Quaternion.Euler(0f, _serverState.Value.Yaw, 0f);
                return;
            }

            RenderInterpolatedPosition();
        }

        private void OnSnapshotReceived(ServerState previous, ServerState confirmed)
        {
            _snapshots.Add(new Snapshot
            {
                ReceivedAt = Time.time,
                Position = confirmed.Position,
                Rotation = Quaternion.Euler(0f, confirmed.Yaw, 0f)
            });

            float oldestNeeded = Time.time - interpolationDelay - 1f;
            _snapshots.RemoveAll(s => s.ReceivedAt < oldestNeeded);
        }

        private void RenderInterpolatedPosition()
        {
            if (_snapshots.Count == 0) return;

            float renderTime = Time.time - interpolationDelay;

            // Find the two snapshots that bracket renderTime.
            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                if (_snapshots[i].ReceivedAt <= renderTime && renderTime <= _snapshots[i + 1].ReceivedAt)
                {
                    float span = _snapshots[i + 1].ReceivedAt - _snapshots[i].ReceivedAt;
                    float t = span > 0f ? (renderTime - _snapshots[i].ReceivedAt) / span : 0f;
                    t = Mathf.Clamp01(t);
                    transform.position = Vector3.Lerp(_snapshots[i].Position, _snapshots[i + 1].Position, t);
                    transform.rotation = Quaternion.Slerp(_snapshots[i].Rotation, _snapshots[i + 1].Rotation, t);
                    return;
                }
            }

            // renderTime is newer than everything we have (e.g. just started
            // receiving snapshots) — snap to the latest known position.
            transform.position = _snapshots[_snapshots.Count - 1].Position;
            transform.rotation = _snapshots[_snapshots.Count - 1].Rotation;
        }

        private void OnServerStateChanged(ServerState previous, ServerState confirmed)
        {
            if (!_history.TryGet(confirmed.Tick, out _, out Vector3 predictedAtTick))
            {
                return;
            }

            float error = Vector3.Distance(predictedAtTick, confirmed.Position);
            if (error <= correctionThreshold)
            {
                return;
            }

            Vector3 replayedPosition = confirmed.Position;
            for (int t = confirmed.Tick + 1; t <= NetworkTick.Current; t++)
            {
                if (!_history.TryGetInput(t, out Vector3 recordedInput))
                {
                    continue;
                }

                replayedPosition += recordedInput * moveSpeed * NetworkTick.TickInterval;
                _history.UpdateResultingPosition(t, replayedPosition);
            }

            transform.position = replayedPosition;
        }

        [ServerRpc]
        private void SubmitMovementServerRpc(int tick, Vector3 input, float yaw)
        {
            if (input.sqrMagnitude > 1f) input.Normalize();

            _authoritativePosition += input * moveSpeed * NetworkTick.TickInterval;
            _serverState.Value = new ServerState { Tick = tick, Position = _authoritativePosition, Yaw = yaw };
        }
    }
}