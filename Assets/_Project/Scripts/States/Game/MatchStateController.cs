using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Utils.StateMachine;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Match
{
    public class MatchStateController : NetworkBehaviour
    {
        public enum MatchStateId { Warmup, Live, RoundEnd, PostMatch }

        public static MatchStateController Instance { get; private set; }

        [SerializeField] private float warmupDuration = 10f;
        [SerializeField] private float roundEndDuration = 8f;
        [SerializeField] private float postMatchDuration = 10f;

        public float WarmupDuration => warmupDuration;
        public float RoundEndDuration => roundEndDuration;
        public float PostMatchDuration => postMatchDuration;

        private readonly NetworkVariable<MatchStateId> _currentState =
            new NetworkVariable<MatchStateId>(MatchStateId.Warmup, writePerm: NetworkVariableWritePermission.Server);

        public MatchStateId CurrentState => _currentState.Value;

        private int _winningTeam = -1;
        public int WinningTeam => _winningTeam;

        private StateMachine _stateMachine;

        private void Awake()
        {
            Instance = this;

            _stateMachine = new StateMachine();
            _stateMachine.RegisterState(typeof(WarmupState), new WarmupState(this));
            _stateMachine.RegisterState(typeof(LiveState), new LiveState());
            _stateMachine.RegisterState(typeof(RoundEndState), new RoundEndState(this));
            _stateMachine.RegisterState(typeof(PostMatchState), new PostMatchState(this));
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _stateMachine.ChangeState(typeof(WarmupState));
            }

            _currentState.OnValueChanged += OnStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentState.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(MatchStateId previous, MatchStateId current)
        {
            EventBus<MatchStateChangedEvent>.Publish(new MatchStateChangedEvent(previous.ToString(), current.ToString()));
            Debug.Log($"[MATCH STATE] {previous} -> {current}");
        }

        private void Update()
        {
            if (!IsServer) return;
            _stateMachine.Tick(Time.deltaTime);
        }

        public void SetState(System.Type stateType, MatchStateId id)
        {
            _stateMachine.ChangeState(stateType);
            _currentState.Value = id;
        }

        // Called by the ticket system once it exists — a team hitting 0
        // tickets ends the round early instead of waiting on a timer.
        public void EndRound(int winningTeam)
        {
            if (!IsServer) return;
            if (_stateMachine.CurrentStateType != typeof(LiveState)) return;

            _winningTeam = winningTeam;
            SetState(typeof(RoundEndState), MatchStateId.RoundEnd);
        }
    }
}