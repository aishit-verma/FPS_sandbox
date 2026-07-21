using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Gameplay.Weapons;
using PolyFrontlines.Gameplay.Team;

namespace PolyFrontlines.Gameplay.Support
{
    [RequireComponent(typeof(NetworkObject))]
    public class AmmoCrate : NetworkBehaviour
    {
        [SerializeField] private int maxCharges = 3;
        [SerializeField] private float lifetime = 60f;
        [SerializeField] private float maxCrateHealth = 60f;

        private int _remainingCharges;
        private float _spawnedAt;
        private float _crateHealth;
        private int _ownerTeamId = -1;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _remainingCharges = maxCharges;
            _spawnedAt = Time.time;
            _crateHealth = maxCrateHealth;
        }

        public void SetOwnerTeam(int teamId)
        {
            if (!IsServer) return;
            _ownerTeamId = teamId;
        }

        // Called by weapons when their raycast hits the crate.
        public void ApplyDamage(float amount, int attackerTeamId)
        {
            if (!IsServer) return;
            if (attackerTeamId == _ownerTeamId) return; // friendly fire off for crates too

            _crateHealth -= amount;
            if (_crateHealth <= 0f)
            {
                NetworkObject.Despawn();
            }
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
            if (TeamUtils.GetTeamId(other) != _ownerTeamId) return; // teammates only

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