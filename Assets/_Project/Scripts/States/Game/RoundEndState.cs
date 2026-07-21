using PolyFrontlines.Utils.StateMachine;

namespace PolyFrontlines.Gameplay.Match
{
    public class RoundEndState : IState
    {
        private readonly MatchStateController _context;
        private float _timer;

        public RoundEndState(MatchStateController context)
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
            if (_timer >= _context.RoundEndDuration)
            {
                _context.SetState(typeof(PostMatchState), MatchStateController.MatchStateId.PostMatch);
            }
        }

        public void Exit() { }
    }
}