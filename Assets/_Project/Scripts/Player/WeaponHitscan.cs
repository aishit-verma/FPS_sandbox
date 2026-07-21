using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Data;
using PolyFrontlines.Networking.Movement;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Weapons
{
    public class WeaponHitscan : NetworkBehaviour
    {
        [SerializeField] private WeaponDefinitionSO weaponDefinition; // fallback/default for standalone testing
        [SerializeField] private Camera ownerCamera;

        private const float MaxRaycastDistance = 300f;

        private readonly NetworkVariable<int> _magazineAmmo =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _reserveAmmo =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isReloading =
            new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);

        public int MagazineAmmo => _magazineAmmo.Value;
        public int ReserveAmmo => _reserveAmmo.Value;

        private float _nextFireTime;
        private float _reloadCompleteAt; // server-only timing
        private PlayerHitboxHistory _ownHitboxHistory;

        private void Awake()
        {
            _ownHitboxHistory = GetComponent<PlayerHitboxHistory>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _magazineAmmo.OnValueChanged += OnAmmoChanged;
                _reserveAmmo.OnValueChanged += OnAmmoChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _magazineAmmo.OnValueChanged -= OnAmmoChanged;
                _reserveAmmo.OnValueChanged -= OnAmmoChanged;
            }
        }

        private void OnAmmoChanged(int previous, int current)
        {
            EventBus<AmmoChangedEvent>.Publish(new AmmoChangedEvent(_magazineAmmo.Value, _reserveAmmo.Value));
        }

        public void Equip(WeaponDefinitionSO definition)
        {
            weaponDefinition = definition;
            if (IsServer)
            {
                _magazineAmmo.Value = definition.magazineSize;
                _reserveAmmo.Value = definition.maxAmmo;
                _isReloading.Value = false;
            }
        }

        public void Resupply()
        {
            if (!IsServer) return;
            _reserveAmmo.Value = weaponDefinition.maxAmmo;
        }

        private void Update()
        {
            if (IsServer)
            {
                HandleReloadCompletion();
            }

            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.R) && !_isReloading.Value
                && _magazineAmmo.Value < weaponDefinition.magazineSize && _reserveAmmo.Value > 0)
            {
                ReloadServerRpc();
            }

            if (Input.GetButton("Fire1") && Time.time >= _nextFireTime
                && !_isReloading.Value && _magazineAmmo.Value > 0)
            {
                _nextFireTime = Time.time + weaponDefinition.fireRate;

                Vector3 origin = ownerCamera.transform.position;
                Vector3 direction = ownerCamera.transform.forward;

                FireServerRpc(NetworkTick.Current, origin, direction);
            }
        }

        [ServerRpc]
        private void ReloadServerRpc()
        {
            if (_isReloading.Value) return;
            if (_magazineAmmo.Value >= weaponDefinition.magazineSize) return;
            if (_reserveAmmo.Value <= 0) return;

            _isReloading.Value = true;
            _reloadCompleteAt = Time.time + weaponDefinition.reloadTime;
        }

        private void HandleReloadCompletion()
        {
            if (!_isReloading.Value) return;
            if (Time.time < _reloadCompleteAt) return;

            int needed = weaponDefinition.magazineSize - _magazineAmmo.Value;
            int taken = Mathf.Min(needed, _reserveAmmo.Value);

            _magazineAmmo.Value += taken;
            _reserveAmmo.Value -= taken;
            _isReloading.Value = false;
        }

        [ServerRpc]
        private void FireServerRpc(int firedAtTick, Vector3 origin, Vector3 direction)
        {
            if (_isReloading.Value || _magazineAmmo.Value <= 0) return;
            _magazineAmmo.Value--;

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
                if (health != null && !Team.TeamUtils.AreSameTeam(this, health))
                {
                    float damage = CalculateDamage(hit.distance);
                    health.ApplyDamage(damage, OwnerClientId, weaponDefinition.weaponName);
                }

                var crate = hit.collider.GetComponentInParent<Support.AmmoCrate>();
                if (crate != null)
                {
                    crate.ApplyDamage(CalculateDamage(hit.distance), Team.TeamUtils.GetTeamId(this));
                }
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