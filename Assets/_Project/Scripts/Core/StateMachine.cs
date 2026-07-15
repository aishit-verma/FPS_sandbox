using System;
using System.Collections.Generic;

namespace PolyFrontlines.Utils.StateMachine
{
    /// <summary>
    /// Generic finite state machine. Not tied to Unity's MonoBehaviour lifecycle
    /// on purpose — the owning component (e.g. MatchStateController, PlayerStateController)
    /// calls Tick() from its own Update/NetworkUpdate, keeping this framework
    /// reusable and independently unit-testable outside PlayMode.
    ///
    /// Usage:
    ///   var sm = new StateMachine();
    ///   sm.RegisterState(typeof(AliveState), new AliveState(this));
    ///   sm.RegisterState(typeof(DeadState), new DeadState(this));
    ///   sm.ChangeState(typeof(AliveState));
    ///   ...
    ///   sm.Tick(Time.deltaTime); // called every frame/tick by the owner
    /// </summary>
    public class StateMachine
    {
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        public IState CurrentState { get; private set; }
        public Type CurrentStateType { get; private set; }

        public event Action<Type, Type> OnStateChanged; // (previous, next)

        public void RegisterState(Type stateType, IState state)
        {
            _states[stateType] = state;
        }

        public void ChangeState(Type nextStateType)
        {
            if (!_states.TryGetValue(nextStateType, out var nextState))
            {
                throw new InvalidOperationException(
                    $"StateMachine: state {nextStateType.Name} was never registered.");
            }

            if (CurrentStateType == nextStateType)
                return; // no-op re-entry guard; avoid double Enter() calls

            var previousType = CurrentStateType;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentStateType = nextStateType;
            CurrentState.Enter();

            OnStateChanged?.Invoke(previousType, nextStateType);
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }
    }
}