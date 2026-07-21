using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Gameplay.Team;

namespace PolyFrontlines.Gameplay.Support
{
    public class AmmoCrateThrower : NetworkBehaviour
    {
        [SerializeField] private GameObject ammoCratePrefab;
        [SerializeField] private float cooldown = 90f;

        private float _nextDropTime;
        private Player.PlayerLoadout _loadout;

        private void Awake()
        {
            _loadout = GetComponent<Player.PlayerLoadout>();
        }

        private bool CanDropCrate => _loadout.CurrentClass != null && _loadout.CurrentClass.canDropAmmoCrate;

        private void Update()
        {
            if (!IsOwner) return;
            if (!CanDropCrate) return;

            if (Input.GetKeyDown(KeyCode.H) && Time.time >= _nextDropTime)
            {
                _nextDropTime = Time.time + cooldown;
                DropServerRpc(transform.position + transform.forward * 1.5f);
            }
        }

        [ServerRpc]
        private void DropServerRpc(Vector3 position)
        {
            if (!CanDropCrate) return;

            GameObject crate = Instantiate(ammoCratePrefab, position, Quaternion.identity);
            crate.GetComponent<NetworkObject>().Spawn();
            crate.GetComponent<AmmoCrate>().SetOwnerTeam(TeamUtils.GetTeamId(this));
        }
    }
}