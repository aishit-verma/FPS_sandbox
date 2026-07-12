using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Data;
using PolyFrontlines.Networking.Movement;

namespace PolyFrontlines.Gameplay.Weapons
{
    public class WeaponHitscan : NetworkBehaviour
    {
        [SerializeField] private WeaponDefinitionSO weaponDefinition;
        [SerializeField] private Camera ownerCamera;

        private const float MaxRaycastDistance = 300f;

        private float _nextFireTime;
        private PlayerHitboxHistory _ownHitboxHistory;

        private void Awake()
        {
            _ownHitboxHistory = GetComponent<PlayerHitboxHistory>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + weaponDefinition.fireRate;

                Vector3 origin = ownerCamera.transform.position;
                Vector3 direction = ownerCamera.transform.forward;

                FireServerRpc(NetworkTick.Current, origin, direction);
            }
        }

        [ServerRpc]
        private void FireServerRpc(int firedAtTick, Vector3 origin, Vector3 direction)
        {
            var rewound = new List<(Transform t, Vector3 realPosition)>();

            foreach (var target in PlayerHitboxHistory.AllTargets)
            {
                if (target == _ownHitboxHistory) continue;

                if (target.TryGetPositionAtTick(firedAtTick, out Vector3 historicalPosition))
                {
                    rewound.Add((target.transform, target.transform.position));
                    target.transform.position = historicalPosition;
                }
            }

            Physics.SyncTransforms();

            bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, MaxRaycastDistance);

            foreach (var (t, realPosition) in rewound)
            {
                t.position = realPosition;
            }
            Physics.SyncTransforms();

            if (didHit)
            {
                var health = hit.collider.GetComponentInParent<PolyFrontlines.Gameplay.Health.Health>();
                if (health != null)
                {
                    float damage = CalculateDamage(hit.distance);
                    health.ApplyDamage(damage);
                    Debug.Log($"[HIT] target={hit.collider.name} distance={hit.distance:F1}m damage={damage} rewoundTick={firedAtTick}");
                }
            }
            else
            {
                Debug.Log("[MISS]");
            }
        }

        private float CalculateDamage(float distance)
        {
            if (distance <= weaponDefinition.rangeNear) return weaponDefinition.damageNear;
            if (distance <= weaponDefinition.rangeMid) return weaponDefinition.damageMid;
            return weaponDefinition.damageFar;
        }
    }
}