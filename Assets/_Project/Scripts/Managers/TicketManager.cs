using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Core;
using PolyFrontlines.Gameplay.Match;

namespace PolyFrontlines.Gameplay.Objectives
{
    public class TicketManager : NetworkBehaviour
    {
        public static TicketManager Instance { get; private set; }

        [SerializeField] private int startingTickets = 250;
        [SerializeField] private float bleedRatePerDifferential = 1f; // tickets/sec per point differential
        [SerializeField] private float timeLimit = 600f; // failsafe, seconds

        private void Awake()
        {
            Instance = this;
        }

        private readonly NetworkVariable<int> _team0Tickets =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _team1Tickets =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

        public int Team0Tickets => _team0Tickets.Value;
        public int Team1Tickets => _team1Tickets.Value;

        private float _matchTimer;
        private float _bleedAccumulator0;
        private float _bleedAccumulator1;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ResetTickets();
            }

            EventBus<MatchStateChangedEvent>.Subscribe(OnMatchStateChanged);
            _team0Tickets.OnValueChanged += OnTeam0TicketsChanged;
            _team1Tickets.OnValueChanged += OnTeam1TicketsChanged;
        }

        public override void OnNetworkDespawn()
        {
            EventBus<MatchStateChangedEvent>.Unsubscribe(OnMatchStateChanged);
            _team0Tickets.OnValueChanged -= OnTeam0TicketsChanged;
            _team1Tickets.OnValueChanged -= OnTeam1TicketsChanged;
        }

        private void OnTeam0TicketsChanged(int previous, int current) =>
            Debug.Log($"[TICKETS] Team 0: {current}");

        private void OnTeam1TicketsChanged(int previous, int current) =>
            Debug.Log($"[TICKETS] Team 1: {current}");

        private void OnMatchStateChanged(MatchStateChangedEvent e)
        {
            if (!IsServer) return;
            if (e.NewState == MatchStateController.MatchStateId.Live.ToString())
            {
                ResetTickets();
                _matchTimer = 0f;
            }
        }

        private void ResetTickets()
        {
            _team0Tickets.Value = startingTickets;
            _team1Tickets.Value = startingTickets;
            _bleedAccumulator0 = 0f;
            _bleedAccumulator1 = 0f;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (MatchStateController.Instance == null) return;
            if (MatchStateController.Instance.CurrentState != MatchStateController.MatchStateId.Live) return;

            _matchTimer += Time.deltaTime;

            CountOwnedPoints(out int team0Points, out int team1Points);
            int differential = team0Points - team1Points;

            if (differential > 0)
            {
                DrainTeam(1, differential); // team 0 controls more -> team 1 bleeds
            }
            else if (differential < 0)
            {
                DrainTeam(0, -differential);
            }

            if (_team0Tickets.Value <= 0)
            {
                MatchStateController.Instance.EndRound(1);
            }
            else if (_team1Tickets.Value <= 0)
            {
                MatchStateController.Instance.EndRound(0);
            }
            else if (_matchTimer >= timeLimit)
            {
                int winner = _team0Tickets.Value == _team1Tickets.Value
                    ? -1
                    : (_team0Tickets.Value > _team1Tickets.Value ? 0 : 1);
                MatchStateController.Instance.EndRound(winner);
            }
        }

        // Fractional bleed accumulates here first, since NetworkVariable<int>
        // can only hold whole numbers — without this, low bleed rates would
        // round to zero every single frame and tickets would never drain.
        private void DrainTeam(int team, int differential)
        {
            float drainAmount = differential * bleedRatePerDifferential * Time.deltaTime;

            if (team == 0)
            {
                _bleedAccumulator0 += drainAmount;
                int whole = Mathf.FloorToInt(_bleedAccumulator0);
                if (whole > 0)
                {
                    _team0Tickets.Value = Mathf.Max(0, _team0Tickets.Value - whole);
                    _bleedAccumulator0 -= whole;
                }
            }
            else
            {
                _bleedAccumulator1 += drainAmount;
                int whole = Mathf.FloorToInt(_bleedAccumulator1);
                if (whole > 0)
                {
                    _team1Tickets.Value = Mathf.Max(0, _team1Tickets.Value - whole);
                    _bleedAccumulator1 -= whole;
                }
            }
        }

        private void CountOwnedPoints(out int team0, out int team1)
        {
            team0 = 0;
            team1 = 0;

            foreach (var point in CapturePoint.AllPoints)
            {
                if (point.OwningTeam == 0) team0++;
                else if (point.OwningTeam == 1) team1++;
            }
        }
    }
}