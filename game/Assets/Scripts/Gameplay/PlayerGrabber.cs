using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Ramasser (E), lâcher (E), lancer (clic gauche). Le propriétaire vise,
    /// le serveur valide et exécute — première brique de l'autorité hôte du GDD.
    /// </summary>
    public class PlayerGrabber : NetworkBehaviour
    {
        [SerializeField] private float _grabRange = 3.5f;
        [SerializeField] private float _throwForce = 9f;
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private Transform _holdAnchor;

        /// <summary>Objet actuellement porté (référence côté serveur uniquement).</summary>
        private NetworkGrabbable _serverHeld;

        private void Update()
        {
            if (!IsOwner || Cursor.lockState != CursorLockMode.Locked)
                return;

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null)
                return;

            if (kb.eKey.wasPressedThisFrame)
                RpcToggleGrab(_cameraHolder.position, _cameraHolder.forward);

            if (mouse.leftButton.wasPressedThisFrame)
                RpcThrow(_cameraHolder.forward);
        }

        [ServerRpc]
        private void RpcToggleGrab(Vector3 eyePosition, Vector3 eyeForward)
        {
            if (_serverHeld != null)
            {
                _serverHeld.ServerRelease(Vector3.zero);
                _serverHeld = null;
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
        }

        [ServerRpc]
        private void RpcThrow(Vector3 eyeForward)
        {
            if (_serverHeld == null)
                return;

            _serverHeld.ServerRelease(eyeForward.normalized * _throwForce);
            _serverHeld = null;
        }
    }
}
