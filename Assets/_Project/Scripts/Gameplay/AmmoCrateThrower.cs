using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Support
{
    public class AmmoCrateThrower : NetworkBehaviour
    {
        [SerializeField] private GameObject ammoCratePrefab;
        [SerializeField] private float cooldown = 90f;

        private float _nextDropTime;

        private void Update()
        {
            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.H) && Time.time >= _nextDropTime)
            {
                _nextDropTime = Time.time + cooldown;
                DropServerRpc(transform.position + transform.forward * 1.5f);
            }
        }

        [ServerRpc]
        private void DropServerRpc(Vector3 position)
        {
            GameObject crate = Instantiate(ammoCratePrefab, position, Quaternion.identity);
            crate.GetComponent<NetworkObject>().Spawn();
        }
    }
}