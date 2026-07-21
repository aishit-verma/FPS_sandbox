using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Team
{
    public class TeamManager : MonoBehaviour
    {
        private readonly int[] _teamPlayerCounts = new int[2];
        private readonly Dictionary<ulong, int> _clientTeams = new Dictionary<ulong, int>();

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            int assignedTeam = _teamPlayerCounts[0] <= _teamPlayerCounts[1] ? 0 : 1;
            _teamPlayerCounts[assignedTeam]++;
            _clientTeams[clientId] = assignedTeam;

            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerTeam = playerObject.GetComponent<PlayerTeam>();
            playerTeam.SetTeam(assignedTeam);

            Debug.Log($"[TEAM] client {clientId} assigned to team {assignedTeam}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (!_clientTeams.TryGetValue(clientId, out int team)) return;

            _teamPlayerCounts[team]--;
            _clientTeams.Remove(clientId);
        }
    }
}