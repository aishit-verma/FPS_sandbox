using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Gameplay.Weapons;

namespace PolyFrontlines.Gameplay.Support
{
    [RequireComponent(typeof(NetworkObject))]
    public class AmmoCrate : NetworkBehaviour
    {
        [SerializeField] private int maxCharges = 3;
        [SerializeField] private float lifetime = 60f;

        private int _remainingCharges;
        private float _spawnedAt;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _remainingCharges = maxCharges;
            _spawnedAt = Time.time;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time - _spawnedAt >= lifetime)
            {
                NetworkObject.Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _remainingCharges <= 0) return;

            var weapon = other.GetComponentInParent<WeaponHitscan>();
            var grenades = other.GetComponentInParent<GrenadeThrower>();
            if (weapon == null && grenades == null) return;

            weapon?.Resupply();
            grenades?.Resupply();
            _remainingCharges--;

            if (_remainingCharges <= 0)
            {
                NetworkObject.Despawn();
            }
        }
    }
}