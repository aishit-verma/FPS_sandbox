using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Gameplay.Player.LifeStates;
using PolyFrontlines.Gameplay.Team;

namespace PolyFrontlines.Gameplay.Player
{
    public class ReviveInteractor : NetworkBehaviour
    {
        [SerializeField] private Camera ownerCamera;
        [SerializeField] private float interactRange = 3f;

        private PlayerLifeState _ownLifeState;
        private PlayerLoadout _loadout;

        private void Awake()
        {
            _ownLifeState = GetComponent<PlayerLifeState>();
            _loadout = GetComponent<PlayerLoadout>();
        }

        private bool CanRevive => _loadout.CurrentClass != null && _loadout.CurrentClass.canRevive;

        private void Update()
        {
            if (!IsOwner) return;
            if (!CanRevive) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                RequestReviveServerRpc(ownerCamera.transform.position, ownerCamera.transform.forward);
            }
        }

        [ServerRpc]
        private void RequestReviveServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!CanRevive) return;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, interactRange)) return;

            var targetLifeState = hit.collider.GetComponentInParent<PlayerLifeState>();
            if (targetLifeState == null || targetLifeState == _ownLifeState) return;
            if (!TeamUtils.AreSameTeam(this, targetLifeState)) return;

            targetLifeState.TryRevive(OwnerClientId);
        }
    }
}