using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Santé et mort. À la mort, la règle cruelle s'applique (la poche part au
    /// pot commun) et le corps reste sur place, teinté et couché — pas de
    /// despawn : l'identité du joueur survit pour le Tribunal.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        private readonly SyncVar<int> _health = new SyncVar<int>(100);
        private readonly SyncVar<bool> _dead = new SyncVar<bool>();

        public bool IsDead => _dead.Value;
        public int Health => _health.Value;

        /// <summary>Serveur : inflige des dégâts (gaz, électricité, plus tard le Juge).</summary>
        public void ServerDamage(int amount)
        {
            if (_dead.Value)
                return;
            _health.Value = Mathf.Max(0, _health.Value - amount);
            if (_health.Value <= 0)
                ServerKill();
        }

        /// <summary>Serveur : mort immédiate.</summary>
        public void ServerKill()
        {
            if (_dead.Value)
                return;
            _dead.Value = true;

            var grabber = GetComponent<PlayerGrabber>();
            if (grabber != null)
                grabber.ServerDropHeld();

            // La règle cruelle : ta poche part au pot — ta mort aide les autres.
            if (EconomyNetworkService.Instance != null)
                EconomyNetworkService.Instance.ServerOnPlayerDeath(OwnerId);
            if (RoundManager.Instance != null)
                RoundManager.Instance.ServerOnPlayerDied(OwnerId);

            RpcOnDeath();
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcOnDeath()
        {
            // Corps couché et assombri, visible par tous.
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material.color = new Color(0.25f, 0.05f, 0.05f);
            var cc = GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;
        }

        private void OnGUI()
        {
            if (!IsOwner)
                return;

            if (_dead.Value)
            {
                var deadStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height / 2f - 40, 440, 60),
                    "TU ES MORT\nTa poche perso est partie au pot commun.", deadStyle);
                return;
            }

            var style = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(10, Screen.height - 36, 130, 26), $"SANTE : {_health.Value}", style);
        }
    }
}
