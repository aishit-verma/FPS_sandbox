using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Health
{
    public class Health : NetworkBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private readonly NetworkVariable<float> _currentHealth =
            new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);

        public float CurrentHealth => _currentHealth.Value;
        public bool IsDead => _currentHealth.Value <= 0f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _currentHealth.Value = maxHealth;
            }
        }

        public void ApplyDamage(float amount)
        {
            if (!IsServer) return;
            if (IsDead) return;

            _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

            if (_currentHealth.Value <= 0f)
            {
                Debug.Log($"{gameObject.name} died.");
            }
        }
    }
}