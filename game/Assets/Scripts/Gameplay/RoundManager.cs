using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Component.Spawning;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    public enum RoundPhase : byte { Expedition, Extraction, Tribunal, Ended }

    /// <summary>
    /// La manche (docs/gdd/01-core-loop.md + 06-extraction.md) :
    /// Expédition (timer) → quota atteint → la capsule arrive → sièges N-2,
    /// bouton rouge (départ anticipé), surcharge = refus de décoller →
    /// fin de manche, le survivant extrait le plus riche gagne.
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance { get; private set; }

        /// <summary>Doit correspondre à la position de la capsule dans J0SceneBuilder.</summary>
        public static readonly Vector3 CapsuleCenter = new Vector3(0f, 1f, 17.5f);
        public const float CapsuleRadius = 3f;

        [Header("Réglages de playtest")]
        [SerializeField] private float _expeditionSeconds = 300f;
        [SerializeField] private float _extractionWindowSeconds = 120f;
        [SerializeField] private float _redButtonUnlockSeconds = 30f;
        [SerializeField] private float _launchCountdownSeconds = 10f;

        private readonly SyncVar<RoundPhase> _phase = new SyncVar<RoundPhase>();
        private readonly SyncVar<int> _roundNumber = new SyncVar<int>(1);
        private readonly SyncVar<int> _secondsLeft = new SyncVar<int>();
        private readonly SyncVar<int> _seats = new SyncVar<int>();
        private readonly SyncVar<int> _aboard = new SyncVar<int>();
        private readonly SyncVar<float> _launchCountdown = new SyncVar<float>(-1f);
        private readonly SyncVar<string> _announcement = new SyncVar<string>();
        private readonly SyncVar<string> _endResults = new SyncVar<string>();

        private readonly HashSet<int> _players = new HashSet<int>();
        private readonly HashSet<int> _deadPlayers = new HashSet<int>();
        private float _phaseEndTime;
        private float _extractionStartTime;
        private Coroutine _countdownRoutine;
        private GameObject _barrier;
        private List<int> _finalExtracted = new List<int>();
        private string _endReason = string.Empty;
        /// <summary>Cumul de session : les extraits encaissent leur poche à chaque manche.</summary>
        private readonly Dictionary<int, int> _sessionBank = new Dictionary<int, int>();

        public RoundPhase Phase => _phase.Value;

        private void Awake()
        {
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _phase.Value = RoundPhase.Expedition;
            _phaseEndTime = Time.time + _expeditionSeconds;

            var spawner = FindAnyObjectByType<PlayerSpawner>();
            if (spawner != null)
                spawner.OnSpawned += nob =>
                {
                    _players.Add(nob.OwnerId);
                    _seats.Value = Mathf.Max(1, _players.Count - 2);
                };
        }

        public void ServerOnPlayerDied(int playerId)
        {
            _deadPlayers.Add(playerId);
            if (_phase.Value != RoundPhase.Ended && _players.Count > 0 && _deadPlayers.Count >= _players.Count)
                ServerEndRound(new List<int>(), "PERSONNE N'EST SORTI VIVANT.");
        }

        private void Update()
        {
            UpdateBarrierVisual();

            if (!IsServerInitialized)
                return;

            // Manche suivante : l'hôte presse R sur l'écran de fin.
            if (_phase.Value == RoundPhase.Ended)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame)
                    ServerStartNewRound();
                return;
            }

            _secondsLeft.Value = Mathf.Max(0, Mathf.CeilToInt(_phaseEndTime - Time.time));

            if (_phase.Value == RoundPhase.Expedition)
            {
                bool quotaOk = EconomyNetworkService.Instance != null && EconomyNetworkService.Instance.QuotaReached;
                if (quotaOk)
                {
                    ServerStartExtraction("QUOTA ATTEINT — LA CAPSULE ARRIVE.");
                }
                else if (Time.time >= _phaseEndTime)
                {
                    // Pas de capsule sans quota : tout le monde meurt.
                    ServerEndRound(new List<int>(), "QUOTA NON ATTEINT. LA DIRECTION VOUS ABANDONNE.");
                }
            }
            else if (_phase.Value == RoundPhase.Extraction)
            {
                _aboard.Value = ServerPlayersAboard().Count;
                if (_launchCountdown.Value < 0f && Time.time >= _phaseEndTime)
                    ServerTryLaunch(auto: true);
            }
        }

        private void ServerStartExtraction(string message)
        {
            _phase.Value = RoundPhase.Extraction;
            _extractionStartTime = Time.time;
            _phaseEndTime = Time.time + _extractionWindowSeconds;
            ServerAnnounce(message, 6f);
        }

        /// <summary>Serveur : un joueur à bord presse le bouton rouge.</summary>
        public void ServerTryRedButton(int playerId, Vector3 playerPosition)
        {
            if (_phase.Value != RoundPhase.Extraction || _launchCountdown.Value >= 0f)
                return;
            if (Time.time < _extractionStartTime + _redButtonUnlockSeconds)
            {
                ServerAnnounce("BOUTON ROUGE VERROUILLÉ ENCORE QUELQUES SECONDES.", 3f);
                return;
            }
            if (Vector3.Distance(playerPosition, CapsuleCenter) > CapsuleRadius)
                return;

            _countdownRoutine = StartCoroutine(ServerCountdownRoutine());
        }

        private IEnumerator ServerCountdownRoutine()
        {
            float remaining = _launchCountdownSeconds;
            ServerAnnounce("DÉPART ANTICIPÉ ENCLENCHÉ.", 3f);
            while (remaining > 0f)
            {
                _launchCountdown.Value = remaining;
                yield return new WaitForSeconds(0.25f);
                remaining -= 0.25f;
            }
            _launchCountdown.Value = -1f;
            _countdownRoutine = null;
            ServerTryLaunch(auto: false);
        }

        private void ServerTryLaunch(bool auto)
        {
            var aboard = ServerPlayersAboard();
            if (aboard.Count > _seats.Value)
            {
                if (auto)
                {
                    // Fin de fenêtre en surcharge : la capsule part à vide. Cruel, documenté.
                    ServerEndRound(new List<int>(), $"SURCHARGE ({aboard.Count}/{_seats.Value}). LA CAPSULE EST PARTIE À VIDE.");
                }
                else
                {
                    ServerAnnounce($"SURCHARGE : {aboard.Count} À BORD POUR {_seats.Value} SIÈGES. DÉCOLLAGE REFUSÉ.", 5f);
                }
                return;
            }

            // Ceux qui restent au sol meurent (poche → pot, pour l'histoire).
            foreach (var health in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (!health.IsDead && !aboard.Contains(health.OwnerId))
                    health.ServerKill();
            }

            string reason = aboard.Count == 0 ? "LA CAPSULE EST PARTIE À VIDE." : "EXTRACTION RÉUSSIE.";
            ServerEndRound(aboard, reason);
        }

        private List<int> ServerPlayersAboard()
        {
            var result = new List<int>();
            foreach (var health in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (!health.IsDead && Vector3.Distance(health.transform.position, CapsuleCenter) <= CapsuleRadius)
                    result.Add(health.OwnerId);
            }
            return result;
        }

        private void ServerEndRound(List<int> extracted, string reason)
        {
            if (_phase.Value == RoundPhase.Ended || _phase.Value == RoundPhase.Tribunal)
                return;
            if (_countdownRoutine != null)
                StopCoroutine(_countdownRoutine);
            _launchCountdown.Value = -1f;
            _finalExtracted = extracted;
            _endReason = reason;

            // Le Tribunal d'abord : le vainqueur se calcule APRÈS les primes de
            // mauvaise foi et les amendes (le mensonge est scoré, pilier 2).
            var tribunal = TribunalNetworkService.Instance;
            if (tribunal != null && tribunal.ServerHasIncidents)
            {
                _phase.Value = RoundPhase.Tribunal;
                ServerAnnounce(reason, 5f);
                tribunal.ServerRunTribunal(ServerShowFinalResults);
            }
            else
            {
                ServerShowFinalResults();
            }
        }

        private void ServerShowFinalResults()
        {
            _phase.Value = RoundPhase.Ended;
            int winner = EconomyNetworkService.Instance.ServerWinner(_finalExtracted);
            var lines = new List<string> { _endReason, string.Empty };
            foreach (var snapshot in EconomyNetworkService.Instance.ServerSnapshot().OrderByDescending(s => s.pocket))
            {
                // Les extraits encaissent leur poche dans la banque de session.
                if (_finalExtracted.Contains(snapshot.playerId))
                    _sessionBank[snapshot.playerId] = _sessionBank.GetValueOrDefault(snapshot.playerId) + snapshot.pocket;
                else
                    _sessionBank.TryAdd(snapshot.playerId, 0);

                string status = _finalExtracted.Contains(snapshot.playerId) ? "EXTRAIT" : _deadPlayers.Contains(snapshot.playerId) ? "MORT" : "ABANDONNÉ";
                string crown = snapshot.playerId == winner ? "  <<< VAINQUEUR" : string.Empty;
                lines.Add($"Joueur {snapshot.playerId} — {snapshot.pocket} $ — {status}{crown}");
            }
            if (winner < 0)
                lines.Add("\nPas de vainqueur. La Direction décline toute responsabilité.");

            lines.Add(string.Empty);
            lines.Add($"— CLASSEMENT DE SESSION ({_roundNumber.Value} manche{(_roundNumber.Value > 1 ? "s" : "")}) —");
            foreach (var entry in _sessionBank.OrderByDescending(kv => kv.Value))
                lines.Add($"Joueur {entry.Key} : {entry.Value} $");

            _endResults.Value = string.Join("\n", lines);
        }

        /// <summary>Serveur (hôte, touche R) : réinitialise tout en place pour la manche suivante.</summary>
        private void ServerStartNewRound()
        {
            // Tout le monde lâche ce qu'il porte.
            foreach (var grabber in FindObjectsByType<PlayerGrabber>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                grabber.ServerDropHeld();

            EconomyNetworkService.Instance.ServerResetRound();
            PacteNetworkService.Instance.ServerResetRound();
            GetComponent<LootRespawner>()?.ServerResetRound();
            if (TheJudge.Instance != null)
                TheJudge.Instance.ServerResetRound();

            // Résurrection et replacement de tous les joueurs sur le cercle de spawn.
            var healths = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < healths.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, healths.Length);
                var pos = new Vector3(Mathf.Cos(angle) * 12f, 1.2f, Mathf.Sin(angle) * 12f);
                float yaw = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z)).eulerAngles.y;
                healths[i].ServerRevive(pos, yaw);
            }

            _deadPlayers.Clear();
            _finalExtracted = new List<int>();
            _endReason = string.Empty;
            _endResults.Value = string.Empty;
            _launchCountdown.Value = -1f;

            _roundNumber.Value++;
            _phase.Value = RoundPhase.Expedition;
            _phaseEndTime = Time.time + _expeditionSeconds;
            ServerAnnounce($"MANCHE {_roundNumber.Value} — BONNE CHANCE. OU PAS.", 5f);
        }

        public void ServerAnnounce(string message, float seconds)
        {
            _announcement.Value = message;
            StartCoroutine(ClearAnnouncement(seconds));
        }

        private IEnumerator ClearAnnouncement(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _announcement.Value = string.Empty;
        }

        // ==================== VISUEL / HUD (tous les clients) ====================

        private void UpdateBarrierVisual()
        {
            if (_barrier == null)
            {
                var found = GameObject.Find("CapsuleBarrier");
                if (found != null)
                    _barrier = found;
                else
                    return;
            }
            bool closed = _phase.Value == RoundPhase.Expedition;
            if (_barrier.activeSelf != closed)
                _barrier.SetActive(closed);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };

            // Phase + timer, en haut à droite.
            string phaseText = _phase.Value switch
            {
                RoundPhase.Expedition => $"MANCHE {_roundNumber.Value} — EXPÉDITION — {_secondsLeft.Value / 60}:{_secondsLeft.Value % 60:00}",
                RoundPhase.Extraction => $"EXTRACTION — {_secondsLeft.Value}s — À BORD : {_aboard.Value}/{_seats.Value}",
                RoundPhase.Tribunal => "LE TRIBUNAL",
                _ => "MANCHE TERMINÉE",
            };
            GUI.Box(new Rect(Screen.width - 320, 8, 310, 30), phaseText, style);

            // Annonces de La Direction.
            if (!string.IsNullOrEmpty(_announcement.Value))
            {
                var annStyle = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(Screen.width / 2f - 260, 48, 520, 38), _announcement.Value, annStyle);
            }

            // Compte à rebours du bouton rouge.
            if (_launchCountdown.Value >= 0f)
            {
                var cdStyle = new GUIStyle(GUI.skin.box) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(Screen.width / 2f - 130, Screen.height / 2f - 110, 260, 60),
                    $"DÉCOLLAGE : {Mathf.CeilToInt(_launchCountdown.Value)}", cdStyle);
            }

            // Écran de fin.
            if (_phase.Value == RoundPhase.Ended && !string.IsNullOrEmpty(_endResults.Value))
            {
                string hint = FishNet.InstanceFinder.IsServerStarted
                    ? "R : MANCHE SUIVANTE"
                    : "L'hôte peut relancer une manche (R).";
                var endStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(Screen.width / 2f - 280, Screen.height / 2f - 180, 560, 360),
                    $"FIN DE MANCHE {_roundNumber.Value}\n\n{_endResults.Value}\n\n{hint}", endStyle);
            }

            // Aide bouton rouge quand on est à bord.
            if (_phase.Value == RoundPhase.Extraction && Camera.main != null &&
                Vector3.Distance(Camera.main.transform.position, CapsuleCenter) <= CapsuleRadius + 0.5f)
            {
                GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height - 110, 300, 28), "B : BOUTON ROUGE (départ anticipé)", style);
            }
        }
    }
}
