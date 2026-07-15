using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Gameplay.Player.LifeStates;

namespace PolyFrontlines.Gameplay.Player
{
    public class ReviveInteractor : NetworkBehaviour
    {
        [SerializeField] private Camera ownerCamera;
        [SerializeField] private float interactRange = 3f;

        private PlayerLifeState _ownLifeState;

        private void Awake()
        {
            _ownLifeState = GetComponent<PlayerLifeState>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                RequestReviveServerRpc(ownerCamera.transform.position, ownerCamera.transform.forward);
            }
        }

        [ServerRpc]
        private void RequestReviveServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, interactRange)) return;

            var targetLifeState = hit.collider.GetComponentInParent<PlayerLifeState>();
            if (targetLifeState == null || targetLifeState == _ownLifeState) return;

            targetLifeState.TryRevive(OwnerClientId);
        }
    }
}