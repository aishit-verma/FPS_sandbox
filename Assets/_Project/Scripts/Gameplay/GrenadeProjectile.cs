using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Data;

namespace PolyFrontlines.Gameplay.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public class GrenadeProjectile : NetworkBehaviour
    {
        [SerializeField] private GrenadeDefinitionSO definition;

        private Rigidbody _rb;
        private float _fuseTimer;
        private bool _exploded;
        private ulong _throwerClientId;
        private int _throwerTeamId = -1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            // Only the server simulates real physics; clients just follow
            // the synced transform.
            _rb.isKinematic = !IsServer;
        }

        public void Launch(Vector3 velocity, ulong throwerClientId, int throwerTeamId)
        {
            if (!IsServer) return;
            _rb.linearVelocity = velocity;
            _throwerClientId = throwerClientId;
            _throwerTeamId = throwerTeamId;
        }

        private void Update()
        {
            if (!IsServer || _exploded) return;

            _fuseTimer += Time.deltaTime;
            if (_fuseTimer >= definition.fuseTime)
            {
                Explode();
            }
        }

        private void Explode()
        {
            _exploded = true;

            Collider[] hits = Physics.OverlapSphere(transform.position, definition.radius);

            foreach (var col in hits)
            {
                var health = col.GetComponentInParent<PolyFrontlines.Gameplay.Health.Health>();
                if (health == null) continue;
                if (Team.TeamUtils.GetTeamId(health) == _throwerTeamId) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / definition.radius));
                float damage = definition.damage * falloff;

                health.ApplyDamage(damage, _throwerClientId, "Grenade");
            }

            NetworkObject.Despawn();
        }
    }
}