using Unity.Netcode;
using PolyFrontlines.Core;
using PolyFrontlines.Gameplay.Player.LifeStates;

namespace PolyFrontlines.Gameplay.Team
{
    public class PlayerTeam : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _teamId =
            new NetworkVariable<int>(-1, writePerm: NetworkVariableWritePermission.Server);

        public int TeamId => _teamId.Value;

        public void SetTeam(int teamId)
        {
            if (!IsServer) return;
            _teamId.Value = teamId;

            // First moment we actually know the team — place the player
            // at a proper spawn now, instead of wherever they auto-spawned.
            var lifeState = GetComponent<PlayerLifeState>();
            if (lifeState != null)
            {
                lifeState.Respawn();
            }
        }

        public override void OnNetworkSpawn()
        {
            _teamId.OnValueChanged += OnTeamChanged;
        }

        public override void OnNetworkDespawn()
        {
            _teamId.OnValueChanged -= OnTeamChanged;
        }

        private void OnTeamChanged(int previous, int current)
        {
            EventBus<TeamAssignedEvent>.Publish(new TeamAssignedEvent(OwnerClientId, current));
        }
    }
}