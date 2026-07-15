using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Player.LifeStates
{
    public class DownedState : IState
    {
        private readonly PlayerLifeState _context;
        private float _bleedOutTimer;

        public DownedState(PlayerLifeState context)
        {
            _context = context;
        }

        public void Enter()
        {
            _bleedOutTimer = 0f;
            _context.SetGameplayEnabled(false);
        }

        public void Tick(float deltaTime)
        {
            _bleedOutTimer += deltaTime;
            if (_bleedOutTimer >= _context.BleedOutTime)
            {
                _context.TransitionToDead();
            }
        }

        public void Exit() { }
    }
}