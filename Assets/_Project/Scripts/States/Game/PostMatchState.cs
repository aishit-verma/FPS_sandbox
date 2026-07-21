using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Match
{
    public class PostMatchState : IState
    {
        private readonly MatchStateController _context;
        private float _timer;

        public PostMatchState(MatchStateController context)
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
            if (_timer >= _context.PostMatchDuration)
            {
                // Loops back to Warmup for now, since there's no separate
                // lobby/restart flow yet. Real match-restart logic is
                // future work once that exists.
                _context.SetState(typeof(WarmupState), MatchStateController.MatchStateId.Warmup);
            }
        }

        public void Exit() { }
    }
}