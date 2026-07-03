using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// La Montre (docs/gdd/03-pactes.md), version boîte grise.
    /// - L'offre arrive : vibration audible a ~2,5 m par tout le monde.
    /// - Consulter = TENIR Tab : geste PUBLIC (l'indicateur de poignet s'allume
    ///   chez tous les joueurs) — c'est le tell social du GDD.
    /// - F pendant la consultation = accepter. La poche est secrete (OwnerOnly).
    /// </summary>
    public class PlayerWatch : NetworkBehaviour
    {
        [SerializeField] private Renderer _wristIndicator;

        private readonly SyncVar<int> _pocket = new SyncVar<int>(new SyncTypeSettings(ReadPermission.OwnerOnly));
        private readonly SyncVar<bool> _consulting = new SyncVar<bool>();

        // --- Offre courante, cote client proprietaire uniquement ---
        private int _offerId = -1;
        private string _offerTitle;
        private int _offerReward;
        private float _offerExpiresAt;

        private AudioSource _buzzSource;

        public int Pocket => _pocket.Value;

        private void Awake()
        {
            _buzzSource = gameObject.AddComponent<AudioSource>();
            _buzzSource.playOnAwake = false;
            _buzzSource.spatialBlend = 1f;
            _buzzSource.minDistance = 0.5f;
            _buzzSource.maxDistance = 2.5f;
            _buzzSource.rolloffMode = AudioRolloffMode.Linear;
            _buzzSource.clip = CreateBuzzClip();
        }

        /// <summary>Double impulsion sinusoïdale générée à la volée — pas d'asset audio en boîte grise.</summary>
        private static AudioClip CreateBuzzClip()
        {
            const int rate = 44100;
            const float duration = 0.5f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                bool pulse = t < 0.15f || (t > 0.25f && t < 0.4f);
                data[i] = pulse ? Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.5f : 0f;
            }
            var clip = AudioClip.Create("WatchBuzz", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ==================== SERVEUR ====================

        /// <summary>Serveur : crédite/débite la poche perso.</summary>
        public void ServerCredit(int amount) => _pocket.Value += amount;

        /// <summary>Serveur : pousse une offre sur cette montre.</summary>
        public void ServerSendOffer(int offerId, string title, int reward, float expirySeconds)
        {
            TargetReceiveOffer(Owner, offerId, title, reward, expirySeconds);
            RpcBuzz();
        }

        [ServerRpc]
        private void RpcSetConsulting(bool value) => _consulting.Value = value;

        [ServerRpc]
        private void RpcRequestAccept(int offerId)
        {
            bool ok = PacteNetworkService.Instance != null && PacteNetworkService.Instance.ServerTryAccept(this, offerId);
            TargetAcceptResult(Owner, offerId, ok);
        }

        // ==================== CLIENT ====================

        [TargetRpc]
        private void TargetReceiveOffer(NetworkConnection conn, int offerId, string title, int reward, float expirySeconds)
        {
            _offerId = offerId;
            _offerTitle = title;
            _offerReward = reward;
            _offerExpiresAt = Time.time + expirySeconds;
        }

        [TargetRpc]
        private void TargetAcceptResult(NetworkConnection conn, int offerId, bool accepted)
        {
            if (_offerId == offerId)
                _offerId = -1; // acceptee ou refusee par le serveur : l'offre quitte l'ecran
        }

        [ObserversRpc]
        private void RpcBuzz()
        {
            // Audible par quiconque est a moins de ~2,5 m — y compris le porteur.
            if (_buzzSource != null)
                _buzzSource.Play();
        }

        private void Update()
        {
            // Indicateur de poignet : visible par TOUS (le tell social).
            if (_wristIndicator != null)
                _wristIndicator.material.color = _consulting.Value ? new Color(1f, 0.85f, 0.2f) : new Color(0.12f, 0.12f, 0.14f);

            if (!IsOwner)
                return;

            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            bool wantConsult = kb.tabKey.isPressed && Cursor.lockState == CursorLockMode.Locked;
            if (wantConsult != _consulting.Value)
                RpcSetConsulting(wantConsult);

            if (_offerId >= 0 && Time.time >= _offerExpiresAt)
                _offerId = -1;

            if (wantConsult && _offerId >= 0 && kb.fKey.wasPressedThisFrame)
                RpcRequestAccept(_offerId);
        }

        private void OnGUI()
        {
            if (!IsOwner || !_consulting.Value)
                return;

            GUILayout.BeginArea(new Rect(Screen.width - 290, Screen.height - 170, 280, 160), GUI.skin.box);
            GUILayout.Label("MONTRE — LA DIRECTION");
            GUILayout.Label($"Poche perso : {_pocket.Value} $");
            GUILayout.Space(6);
            if (_offerId >= 0)
            {
                float remaining = Mathf.Max(0f, _offerExpiresAt - Time.time);
                GUILayout.Label($"OFFRE : {_offerTitle}");
                GUILayout.Label($"Gain : +{_offerReward} $   ({remaining:0} s)");
                GUILayout.Label(">> F : ACCEPTER <<");
            }
            else
            {
                GUILayout.Label("Aucune offre en cours.");
            }
            GUILayout.EndArea();
        }
    }
}
