using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Player.LifeStates
{
    public class AliveState : IState
    {
        private readonly PlayerLifeState _context;

        public AliveState(PlayerLifeState context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.SetGameplayEnabled(true);
        }

        public void Tick(float deltaTime) { }

        public void Exit() { }
    }
}