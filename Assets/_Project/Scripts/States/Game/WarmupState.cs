using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Match
{
    public class WarmupState : IState
    {
        private readonly MatchStateController _context;
        private float _timer;

        public WarmupState(MatchStateController context)
        {
            _context = context;
        }

        public void Enter()
        {
            _timer = 0f;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= _context.WarmupDuration)
            {
                _context.SetState(typeof(LiveState), MatchStateController.MatchStateId.Live);
            }
        }

        public void Exit() { }
    }
}