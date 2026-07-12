using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Player
{
    public class PlayerHud : NetworkBehaviour
    {
        [SerializeField] private GameObject hudCanvas;

        public override void OnNetworkSpawn()
        {
            hudCanvas.SetActive(IsOwner);
        }
    }
}