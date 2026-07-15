using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Data;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Weapons
{
    public class GrenadeThrower : NetworkBehaviour
    {
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private GrenadeDefinitionSO definition;
        [SerializeField] private Camera ownerCamera;
        [SerializeField] private int maxGrenades = 2;

        private readonly NetworkVariable<int> _grenadeCount =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _grenadeCount.Value = maxGrenades;
            }

            if (IsOwner)
            {
                _grenadeCount.OnValueChanged += OnGrenadeCountChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _grenadeCount.OnValueChanged -= OnGrenadeCountChanged;
            }
        }

        private void OnGrenadeCountChanged(int previous, int current)
        {
            EventBus<GrenadeCountChangedEvent>.Publish(new GrenadeCountChangedEvent(current, maxGrenades));
        }

        public void Resupply()
        {
            if (!IsServer) return;
            _grenadeCount.Value = maxGrenades;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.G) && _grenadeCount.Value > 0)
            {
                Vector3 origin = ownerCamera.transform.position + ownerCamera.transform.forward;
                Vector3 direction = ownerCamera.transform.forward;

                ThrowServerRpc(origin, direction);
            }
        }

        [ServerRpc]
        private void ThrowServerRpc(Vector3 origin, Vector3 direction)
        {
            if (_grenadeCount.Value <= 0) return;
            _grenadeCount.Value--;

            GameObject grenadeObj = Instantiate(grenadePrefab, origin, Quaternion.identity);
            grenadeObj.GetComponent<NetworkObject>().Spawn();
            grenadeObj.GetComponent<GrenadeProjectile>().Launch(direction * definition.throwForce, OwnerClientId);
        }
    }
}