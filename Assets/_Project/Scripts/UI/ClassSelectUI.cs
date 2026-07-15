using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace PolyFrontlines.Gameplay.Player
{
    public class ClassSelectUI : NetworkBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button assaultButton;
        [SerializeField] private Button medicButton;
        [SerializeField] private Button supportButton;

        private PlayerLoadout _loadout;
        private PlayerLook _look;

        private void Awake()
        {
            _loadout = GetComponent<PlayerLoadout>();
            _look = GetComponent<PlayerLook>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                panel.SetActive(false);
                enabled = false;
                return;
            }

            panel.SetActive(true);
            assaultButton.onClick.AddListener(() => Choose(0));
            medicButton.onClick.AddListener(() => Choose(1));
            supportButton.onClick.AddListener(() => Choose(2));
        }

        private void Choose(int classIndex)
        {
            _loadout.SelectClass(classIndex);
            _look.LockCursor();
            panel.SetActive(false);
        }
    }
}