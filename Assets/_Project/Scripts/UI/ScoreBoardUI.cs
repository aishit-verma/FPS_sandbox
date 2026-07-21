using System.Text;
using TMPro;
using UnityEngine;
using PolyFrontlines.Gameplay.Objectives;

namespace PolyFrontlines.Gameplay.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private void Update()
        {
            bool show = Input.GetKey(toggleKey);
            panel.SetActive(show);

            if (show)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var sb = new StringBuilder();

            if (TicketManager.Instance != null)
            {
                sb.AppendLine($"Team 0 Tickets: {TicketManager.Instance.Team0Tickets}");
                sb.AppendLine($"Team 1 Tickets: {TicketManager.Instance.Team1Tickets}");
                sb.AppendLine();
            }

            foreach (var kvp in ScoreboardTracker.AllStats)
            {
                sb.AppendLine($"Client {kvp.Key}: {kvp.Value.Kills}K / {kvp.Value.Deaths}D / {kvp.Value.Revives}R");
            }

            contentText.text = sb.ToString();
        }
    }
}