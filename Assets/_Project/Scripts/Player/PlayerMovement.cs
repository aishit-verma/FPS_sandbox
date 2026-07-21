using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Utils.Prediction;

namespace PolyFrontlines.Networking.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float correctionThreshold = 0.05f;
        [SerializeField] private float interpolationDelay = 0.1f;

        private const int BufferCapacity = 256;

        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }

        private struct ServerState : INetworkSerializeByMemcpy
        {
            public int Tick;
            public Vector3 Position;
            public float Yaw;
            public float VerticalVelocity;
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
        private CharacterController _controller;

        // Owner-side prediction state
        private float _verticalVelocity;

        // Server-side simulation state
        private float _serverVerticalVelocity;

        private readonly List<Snapshot> _snapshots = new List<Snapshot>();

        private void Awake()
        {
            _history = new TickHistoryBuffer<Vector3>(BufferCapacity);
            _controller = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            transform.position = _serverState.Value.Position;

            if (IsOwner)
            {
                _serverState.OnValueChanged += OnServerStateChanged;
            }
            else if (!IsServer)
            {
                _serverState.OnValueChanged += OnSnapshotReceived;
                // Remote players are moved manually via interpolation, not
                // simulated — their controller must not fight that.
                _controller.enabled = false;
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
            Vector3 localInput = new Vector3(h, 0f, v);
            if (localInput.sqrMagnitude > 1f) localInput.Normalize();

            Vector3 input = transform.right * localInput.x + transform.forward * localInput.z;

            SimulateMove(_controller, input, ref _verticalVelocity);

            _history.Record(tick, input, transform.position);

            SubmitMovementServerRpc(tick, input, transform.eulerAngles.y);
        }

        // The single shared simulation step — used identically by owner
        // prediction, server authority, and reconciliation replay. This
        // being one method is what keeps all three consistent.
        private void SimulateMove(CharacterController controller, Vector3 input, ref float verticalVelocity)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // small downward stick keeps isGrounded stable on slopes
            }

            verticalVelocity += gravity * NetworkTick.TickInterval;

            Vector3 delta = input * moveSpeed * NetworkTick.TickInterval;
            delta.y = verticalVelocity * NetworkTick.TickInterval;

            controller.Move(delta);
        }

        private void Update()
        {
            if (IsOwner) return;

            if (IsServer)
            {
                // Server's own view of remote players: position is already
                // authoritative from the RPC simulation — nothing to do here.
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

            // Correction: snap to the server's confirmed state (position AND
            // vertical velocity — mid-fall corrections must not reset fall
            // speed), then replay buffered inputs through the same collision-
            // aware simulation to catch back up.
            _controller.enabled = false;
            transform.position = confirmed.Position;
            _controller.enabled = true;
            _verticalVelocity = confirmed.VerticalVelocity;

            for (int t = confirmed.Tick + 1; t <= NetworkTick.Current; t++)
            {
                if (!_history.TryGetInput(t, out Vector3 recordedInput))
                {
                    continue;
                }

                SimulateMove(_controller, recordedInput, ref _verticalVelocity);
                _history.UpdateResultingPosition(t, transform.position);
            }
        }

        [ServerRpc]
        private void SubmitMovementServerRpc(int tick, Vector3 input, float yaw)
        {
            if (input.sqrMagnitude > 1f) input.Normalize();

            // On a host, this same object's owner prediction already moved
            // the controller this tick — simulating again would double it.
            // Only simulate when the server is NOT also the owner.
            if (!IsOwner)
            {
                SimulateMove(_controller, input, ref _serverVerticalVelocity);
            }
            else
            {
                _serverVerticalVelocity = _verticalVelocity;
            }

            _serverState.Value = new ServerState
            {
                Tick = tick,
                Position = transform.position,
                Yaw = yaw,
                VerticalVelocity = _serverVerticalVelocity
            };
        }

        public void Teleport(Vector3 newPosition)
        {
            if (!IsServer) return;

            _controller.enabled = false;
            transform.position = newPosition;
            _controller.enabled = true;
            _serverVerticalVelocity = 0f;

            int tick = NetworkTick.Current;
            _serverState.Value = new ServerState
            {
                Tick = tick,
                Position = newPosition,
                Yaw = transform.eulerAngles.y,
                VerticalVelocity = 0f
            };

            TeleportClientRpc(newPosition, tick);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 newPosition, int tick)
        {
            if (!IsOwner) return;

            _controller.enabled = false;
            transform.position = newPosition;
            _controller.enabled = true;
            _verticalVelocity = 0f;
            _history.Record(tick, Vector3.zero, newPosition);
        }
    }
}