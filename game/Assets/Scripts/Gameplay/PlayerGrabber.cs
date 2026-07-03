using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Ramasser (E), lâcher (E), lancer (clic gauche), déposer au Terminal
    /// (G : pot commun, H : poche perso). Le propriétaire vise, le serveur
    /// valide et exécute — autorité hôte du GDD.
    /// </summary>
    public class PlayerGrabber : NetworkBehaviour
    {
        [SerializeField] private float _grabRange = 3.5f;
        [SerializeField] private float _throwForce = 9f;
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private Transform _holdAnchor;

        /// <summary>Objet actuellement porté (référence côté serveur uniquement).</summary>
        private NetworkGrabbable _serverHeld;
        /// <summary>Public : porter un objet se voit (et pilote les prompts du porteur).</summary>
        private readonly SyncVar<bool> _isHolding = new SyncVar<bool>();

        private bool NearTerminal =>
            DepositTerminal.Instance != null &&
            Vector3.Distance(transform.position, DepositTerminal.Instance.transform.position) <= DepositTerminal.Instance.DepositRange;

        /// <summary>Serveur : lâche l'objet porté (mort, fin de manche).</summary>
        public void ServerDropHeld()
        {
            if (_serverHeld == null)
                return;
            _serverHeld.ServerRelease(Vector3.zero);
            _serverHeld = null;
            _isHolding.Value = false;
        }

        private void Update()
        {
            if (!IsOwner || Cursor.lockState != CursorLockMode.Locked)
                return;

            var health = GetComponent<PlayerHealth>();
            if (health != null && health.IsDead)
                return;

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null)
                return;

            if (kb.eKey.wasPressedThisFrame)
                RpcToggleGrab(_cameraHolder.position, _cameraHolder.forward);

            if (mouse.leftButton.wasPressedThisFrame)
                RpcThrow(_cameraHolder.forward);

            if (_isHolding.Value && NearTerminal)
            {
                if (kb.gKey.wasPressedThisFrame)
                    RpcDeposit(true);
                if (kb.hKey.wasPressedThisFrame)
                    RpcDeposit(false);
            }

            if (kb.bKey.wasPressedThisFrame && RoundManager.Instance != null)
                RpcPressRedButton();
        }

        [ServerRpc]
        private void RpcPressRedButton()
        {
            if (RoundManager.Instance != null)
                RoundManager.Instance.ServerTryRedButton(OwnerId, transform.position);
        }

        private void OnGUI()
        {
            if (!IsOwner || !_isHolding.Value || !NearTerminal)
                return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width / 2f - 170, Screen.height - 70, 340, 46),
                "DEPOSER : G -> POT COMMUN  ·  H -> POCHE PERSO\n(le Terminal affiche ton depot a tous)", style);
        }

        [ServerRpc]
        private void RpcDeposit(bool toCommonPot)
        {
            if (_serverHeld == null || DepositTerminal.Instance == null)
                return;
            if (Vector3.Distance(transform.position, DepositTerminal.Instance.transform.position) > DepositTerminal.Instance.DepositRange + 1f)
                return;
            var loot = _serverHeld.GetComponent<LootItem>();
            if (loot == null)
                return;

            var held = _serverHeld;
            _serverHeld = null;
            _isHolding.Value = false;
            DepositTerminal.Instance.ServerDeposit(
                OwnerId,
                $"Joueur {OwnerId}",
                loot,
                toCommonPot ? BadFaith.Core.Economy.DepositDestination.CommonPot : BadFaith.Core.Economy.DepositDestination.PersonalPocket);
            held.NetworkObject.Despawn();
        }

        [ServerRpc]
        private void RpcToggleGrab(Vector3 eyePosition, Vector3 eyeForward)
        {
            if (_serverHeld != null)
            {
                _serverHeld.ServerRelease(Vector3.zero);
                _serverHeld = null;
                _isHolding.Value = false;
                return;
            }

            // Validation serveur : raycast depuis les yeux annoncés, portée bornée.
            if (!Physics.Raycast(eyePosition, eyeForward, out RaycastHit hit, _grabRange))
                return;

            NetworkGrabbable grabbable = hit.collider.GetComponentInParent<NetworkGrabbable>();
            if (grabbable == null || grabbable.IsHeld)
                return;

            grabbable.ServerGrab(_holdAnchor);
            _serverHeld = grabbable;
            _isHolding.Value = true;
        }

        [ServerRpc]
        private void RpcThrow(Vector3 eyeForward)
        {
            if (_serverHeld == null)
                return;

            _serverHeld.ServerRelease(eyeForward.normalized * _throwForce);
            _serverHeld = null;
            _isHolding.Value = false;
        }
    }
}
