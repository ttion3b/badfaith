using FishNet.Object;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Objet physique portable/lançable. Autorité 100 % serveur (hôte) :
    /// le Rigidbody ne simule que côté serveur, les clients suivent le
    /// NetworkTransform (server-authoritative sur ce prefab).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkGrabbable : NetworkBehaviour
    {
        [SerializeField] private float _followLerp = 20f;

        private Rigidbody _rb;
        /// <summary>Point d'ancrage suivi quand porté (côté serveur uniquement).</summary>
        private Transform _serverHoldAnchor;

        public bool IsHeld => _serverHoldAnchor != null;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Pas de simulation physique locale chez les clients purs :
            // le transform est piloté par le réseau.
            if (!IsServerInitialized)
                _rb.isKinematic = true;
        }

        /// <summary>Serveur : commence à suivre l'ancre du porteur.</summary>
        public void ServerGrab(Transform holdAnchor)
        {
            _serverHoldAnchor = holdAnchor;
            _rb.isKinematic = true;
        }

        /// <summary>Serveur : lâche, avec impulsion optionnelle.</summary>
        public void ServerRelease(Vector3 impulse)
        {
            _serverHoldAnchor = null;
            _rb.isKinematic = false;
            if (impulse != Vector3.zero)
                _rb.AddForce(impulse, ForceMode.Impulse);
        }

        private void Update()
        {
            if (!IsServerInitialized || _serverHoldAnchor == null)
                return;

            transform.position = Vector3.Lerp(transform.position, _serverHoldAnchor.position, _followLerp * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _serverHoldAnchor.rotation, _followLerp * Time.deltaTime);
        }
    }
}
