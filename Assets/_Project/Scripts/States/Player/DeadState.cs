using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Player.LifeStates
{
    public class DeadState : IState
    {
        private readonly PlayerLifeState _context;
        private float _respawnTimer;

        public DeadState(PlayerLifeState context)
        {
            _context = context;
        }

        public void Enter()
        {
            _respawnTimer = 0f;
            _context.SetGameplayEnabled(false);
            _context.BroadcastDeath();
        }

        public void Tick(float deltaTime)
        {
            _respawnTimer += deltaTime;
            if (_respawnTimer >= _context.RespawnDelay)
            {
                _context.Respawn();
            }
        }

        public void Exit() { }
    }
}