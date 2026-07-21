using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Data;
using PolyFrontlines.Gameplay.Weapons;
using PolyFrontlines.Gameplay.Health;
using PolyFrontlines.Networking.Movement;

namespace PolyFrontlines.Gameplay.Player
{
    public class PlayerLoadout : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _selectedClass =
            new NetworkVariable<int>(-1, writePerm: NetworkVariableWritePermission.Server);

        // Resolvable on any machine (index syncs, catalog is local everywhere),
        // so the server can validate ability permissions too.
        public ClassDefinitionSO CurrentClass => ClassCatalog.Instance.Get(_selectedClass.Value);

        private WeaponHitscan _weapon;
        private Health.Health _health;
        private PlayerMovement _movement;

        private void Awake()
        {
            _weapon = GetComponent<WeaponHitscan>();
            _health = GetComponent<Health.Health>();
            _movement = GetComponent<PlayerMovement>();
        }

        [SerializeField] private TMPro.TMP_Text classLabel;

        public override void OnNetworkSpawn()
        {
            _selectedClass.OnValueChanged += (_, newValue) => ApplyLoadout(newValue);

            if (_selectedClass.Value >= 0)
            {
                ApplyLoadout(_selectedClass.Value);
            }
        }

        public void SelectClass(int classIndex)
        {
            RequestClassServerRpc(classIndex);
        }

        [ServerRpc]
        private void RequestClassServerRpc(int classIndex)
        {
            if (ClassCatalog.Instance.Get(classIndex) == null) return;
            _selectedClass.Value = classIndex;
        }

        private void ApplyLoadout(int classIndex)
        {
            var classDefinition = ClassCatalog.Instance.Get(classIndex);
            if (classDefinition == null) return;

            _weapon.Equip(classDefinition.primaryWeapon);
            _health.SetMaxHealth(classDefinition.maxHealth);
            _movement.SetMoveSpeed(classDefinition.moveSpeed);

            if (IsOwner && classLabel != null)
            {
                classLabel.text = classDefinition.className;
            }
        }
    }
}