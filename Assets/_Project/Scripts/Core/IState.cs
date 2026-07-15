namespace PolyFrontlines.Utils.StateMachine
{
    /// <summary>
    /// Contract for any state used by StateMachine.
    /// Implemented by both match states (Warmup/Live/RoundEnd/PostMatch)
    /// and player states (Alive/Dead/Spawning) — same framework, different states.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}