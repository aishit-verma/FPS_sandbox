using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Player.LifeStates
{
    public class DeadState : IState
    {
        private readonly PlayerLifeState _context;

        public DeadState(PlayerLifeState context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.SetGameplayEnabled(false);
            _context.BroadcastDeath();
        }

        public void Tick(float deltaTime) { }

        public void Exit() { }
    }
}