using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.UI
{
    public class KillFeedUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text feedText;
        [SerializeField] private float lineLifetime = 5f;

        private readonly List<(string text, float expiresAt)> _lines = new List<(string, float)>();

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<PlayerDownedEvent>.Subscribe(OnPlayerDowned);
            EventBus<PlayerRevivedEvent>.Subscribe(OnPlayerRevived);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<PlayerDownedEvent>.Unsubscribe(OnPlayerDowned);
            EventBus<PlayerRevivedEvent>.Unsubscribe(OnPlayerRevived);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            string line = $"Client {e.KillerClientId} killed Client {e.VictimClientId} ({e.WeaponId})";
            _lines.Add((line, Time.time + lineLifetime));
            RefreshText();
        }

        private void OnPlayerDowned(PlayerDownedEvent e)
        {
            string line = $"Client {e.KillerClientId} downed Client {e.VictimClientId} ({e.WeaponId})";
            _lines.Add((line, Time.time + lineLifetime));
            RefreshText();
        }

        private void OnPlayerRevived(PlayerRevivedEvent e)
        {
            string line = $"Client {e.ReviverClientId} revived Client {e.VictimClientId}";
            _lines.Add((line, Time.time + lineLifetime));
            RefreshText();
        }

        private void Update()
        {
            if (_lines.RemoveAll(l => l.expiresAt <= Time.time) > 0)
            {
                RefreshText();
            }
        }

        private void RefreshText()
        {
            var sb = new StringBuilder();
            foreach (var line in _lines)
            {
                sb.AppendLine(line.text);
            }
            feedText.text = sb.ToString();
        }
    }
}