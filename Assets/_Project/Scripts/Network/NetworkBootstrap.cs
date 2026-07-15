using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Networking.Connection
{
    // Temporary dev-only UI for testing. Will be replaced by the real
    // frontend/menu flow later.
    public class NetworkBootstrap : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));

            var nm = NetworkManager.Singleton;

            if (nm == null)
            {
                GUILayout.Label("No NetworkManager in scene");
                GUILayout.EndArea();
                return;
            }

            if (!nm.IsClient && !nm.IsServer)
            {
                if (GUILayout.Button("Start Host"))
                    nm.StartHost();

                if (GUILayout.Button("Start Client"))
                    nm.StartClient();

                if (GUILayout.Button("Start Server"))
                    nm.StartServer();
            }
            else
            {
                GUILayout.Label(GetStatusLabel(nm));

                if (GUILayout.Button("Disconnect"))
                    nm.Shutdown();
            }

            GUILayout.EndArea();
        }

        private string GetStatusLabel(NetworkManager nm)
        {
            if (nm.IsHost) return "Host";
            if (nm.IsServer) return "Server";
            return "Client";
        }
    }
}