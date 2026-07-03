using FishNet;
using UnityEngine;

namespace BadFaith.UI
{
    /// <summary>
    /// HUD provisoire J0 : Heberger / Rejoindre en LAN (Tugboat).
    /// Sera remplace par le lobby Steam en fin de Phase 0.
    /// </summary>
    public class NetworkLauncherHUD : MonoBehaviour
    {
        private string _address = "localhost";

        private void OnGUI()
        {
            bool serverStarted = InstanceFinder.IsServerStarted;
            bool clientStarted = InstanceFinder.IsClientStarted;
            if (serverStarted || clientStarted)
            {
                GUILayout.BeginArea(new Rect(10, 10, 260, 60));
                GUILayout.Label(serverStarted ? $"HOTE - clients : {InstanceFinder.ServerManager.Clients.Count}" : "CLIENT connecte");
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 260, 140), GUI.skin.box);
            GUILayout.Label("MAUVAISE FOI - test reseau J0");

            if (GUILayout.Button("HEBERGER (hote + joueur)", GUILayout.Height(32)))
            {
                InstanceFinder.ServerManager.StartConnection();
                InstanceFinder.ClientManager.StartConnection();
            }

            GUILayout.BeginHorizontal();
            _address = GUILayout.TextField(_address, GUILayout.Height(28));
            if (GUILayout.Button("REJOINDRE", GUILayout.Width(90), GUILayout.Height(28)))
                InstanceFinder.ClientManager.StartConnection(_address);
            GUILayout.EndHorizontal();

            GUILayout.Label("E : prendre/poser - Clic : lancer\nZQSD/WASD : bouger - Echap : souris");
            GUILayout.EndArea();
        }
    }
}
