using Unity.Netcode;
using UnityEngine;
using PolyFrontlines.Utils.StateMachine;
using PolyFrontlines.Gameplay.Weapons;
using PolyFrontlines.Networking.Movement;
using PolyFrontlines.Gameplay.Team;
using PolyFrontlines.Gameplay.Objectives;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.Player.LifeStates
{
    public class PlayerLifeState : NetworkBehaviour
    {
        public enum LifeStateId { Alive, Downed, Dead }

        [SerializeField] private float bleedOutTime = 20f;
        [SerializeField] private float reviveHealth = 50f;
        [SerializeField] private float respawnDelay = 5f;

        public float BleedOutTime => bleedOutTime;
        public float RespawnDelay => respawnDelay;

        private readonly NetworkVariable<LifeStateId> _visibleState =
            new NetworkVariable<LifeStateId>(LifeStateId.Alive, writePerm: NetworkVariableWritePermission.Server);

        public LifeStateId CurrentVisibleState => _visibleState.Value;

        private StateMachine _stateMachine;
        private Health.Health _health;
        private PlayerMovement _movement;
        private WeaponHitscan _weapon;
        private PlayerTeam _team;

        private ulong _lastKillerClientId;
        private string _lastWeaponId;

        private void Awake()
        {
            _health = GetComponent<Health.Health>();
            _movement = GetComponent<PlayerMovement>();
            _weapon = GetComponent<WeaponHitscan>();
            _team = GetComponent<PlayerTeam>();

            _stateMachine = new StateMachine();
            _stateMachine.RegisterState(typeof(AliveState), new AliveState(this));
            _stateMachine.RegisterState(typeof(DownedState), new DownedState(this));
            _stateMachine.RegisterState(typeof(DeadState), new DeadState(this));
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _health.OnDepleted += OnHealthDepleted;
                _stateMachine.ChangeState(typeof(AliveState));
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _health.OnDepleted -= OnHealthDepleted;
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            _stateMachine.Tick(Time.deltaTime);
        }

        private void OnHealthDepleted(ulong killerClientId, string weaponId)
        {
            _lastKillerClientId = killerClientId;
            _lastWeaponId = weaponId;

            _stateMachine.ChangeState(typeof(DownedState));
            _visibleState.Value = LifeStateId.Downed;

            NotifyDownedClientRpc(OwnerClientId, killerClientId, weaponId);
        }

        [ClientRpc]
        private void NotifyDownedClientRpc(ulong victimClientId, ulong killerClientId, string weaponId)
        {
            EventBus<PlayerDownedEvent>.Publish(new PlayerDownedEvent(victimClientId, killerClientId, weaponId));
        }

        public void TransitionToDead()
        {
            _stateMachine.ChangeState(typeof(DeadState));
            _visibleState.Value = LifeStateId.Dead;
        }

        public void BroadcastDeath()
        {
            _health.BroadcastDeath(_lastKillerClientId, _lastWeaponId);
        }

        public bool TryRevive(ulong reviverClientId)
        {
            if (!IsServer) return false;

            if (_stateMachine.CurrentStateType != typeof(DownedState))
            {
                return false;
            }

            _health.SetHealth(reviveHealth);
            _stateMachine.ChangeState(typeof(AliveState));
            _visibleState.Value = LifeStateId.Alive;

            NotifyRevivedClientRpc(OwnerClientId, reviverClientId);
            return true;
        }

        [ClientRpc]
        private void NotifyRevivedClientRpc(ulong victimClientId, ulong reviverClientId)
        {
            EventBus<PlayerRevivedEvent>.Publish(new PlayerRevivedEvent(victimClientId, reviverClientId));
        }

        public void SetGameplayEnabled(bool enabled)
        {
            if (_movement != null) _movement.enabled = enabled;
            if (_weapon != null) _weapon.enabled = enabled;
        }

        public void Respawn()
        {
            if (!IsServer) return;

            int teamId = _team != null ? _team.TeamId : -1;
            Vector3 spawnPos = RespawnManager.GetSpawnPosition(teamId);

            _movement.Teleport(spawnPos);
            _health.FullHeal();

            _stateMachine.ChangeState(typeof(AliveState));
            _visibleState.Value = LifeStateId.Alive;
        }
    }
}