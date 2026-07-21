using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Match
{
    // Main gameplay state. Currently empty on purpose — the ticket system
    // and capture points will drive transitions out of this state once
    // they exist (calling MatchStateController.EndRound).
    public class LiveState : IState
    {
        public void Enter() { }
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }
}