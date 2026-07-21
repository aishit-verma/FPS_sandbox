using System.Collections.Generic;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Objectives
{
    // Pure client-side accumulation — every client already receives the
    // same PlayerDiedEvent/PlayerRevivedEvent broadcasts (same pattern as
    // KillFeedUI), so no new networking is needed for this at all.
    public static class ScoreboardTracker
    {
        public class PlayerStats
        {
            public int Kills;
            public int Deaths;
            public int Revives;
        }

        private static readonly Dictionary<ulong, PlayerStats> _stats = new Dictionary<ulong, PlayerStats>();
        public static IReadOnlyDictionary<ulong, PlayerStats> AllStats => _stats;

        // Runs once, automatically, the first time anything touches this
        // class — no manual bootstrap call needed anywhere.
        static ScoreboardTracker()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<PlayerRevivedEvent>.Subscribe(OnPlayerRevived);
            EventBus<MatchStateChangedEvent>.Subscribe(OnMatchStateChanged);
        }

        private static void OnPlayerDied(PlayerDiedEvent e)
        {
            GetOrCreate(e.KillerClientId).Kills++;
            GetOrCreate(e.VictimClientId).Deaths++;
        }

        private static void OnPlayerRevived(PlayerRevivedEvent e)
        {
            GetOrCreate(e.ReviverClientId).Revives++;
        }

        private static void OnMatchStateChanged(MatchStateChangedEvent e)
        {
            if (e.NewState == "Live")
            {
                _stats.Clear();
            }
        }

        private static PlayerStats GetOrCreate(ulong clientId)
        {
            if (!_stats.TryGetValue(clientId, out var stats))
            {
                stats = new PlayerStats();
                _stats[clientId] = stats;
            }
            return stats;
        }
    }
}