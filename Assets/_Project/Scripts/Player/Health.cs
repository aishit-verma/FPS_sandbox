using System;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Health
{
    public class Health : NetworkBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private readonly NetworkVariable<float> _currentHealth =
            new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);

        public float CurrentHealth => _currentHealth.Value;
        public bool IsDead => _currentHealth.Value <= 0f;

        // Raised when health reaches 0. Does NOT mean "dead" anymore —
        // PlayerLifeState decides what happens next (usually: go Downed).
        public event Action<ulong, string> OnDepleted;

        public void SetMaxHealth(float newMax)
        {
            maxHealth = newMax;
            if (IsServer)
            {
                _currentHealth.Value = maxHealth;
            }
        }

        public void SetHealth(float value)
        {
            if (!IsServer) return;
            _currentHealth.Value = Mathf.Clamp(value, 0f, maxHealth);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _currentHealth.Value = maxHealth;
            }
        }

        public void ApplyDamage(float amount, ulong killerClientId, string weaponId)
        {
            if (!IsServer) return;
            if (IsDead) return;

            _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

            if (_currentHealth.Value <= 0f)
            {
                OnDepleted?.Invoke(killerClientId, weaponId);
            }
        }

        public void BroadcastDeath(ulong killerClientId, string weaponId)
        {
            if (!IsServer) return;
            NotifyDeathClientRpc(OwnerClientId, killerClientId, weaponId);
        }

        [ClientRpc]
        private void NotifyDeathClientRpc(ulong victimClientId, ulong killerClientId, string weaponId)
        {
            EventBus<PlayerDiedEvent>.Publish(new PlayerDiedEvent(victimClientId, killerClientId, weaponId));
        }
    }
}