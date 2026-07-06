using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BadFaith.Core;
using BadFaith.Core.Hazards;
using BadFaith.Core.Pactes;
using BadFaith.Core.Tribunal;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Le Tribunal (docs/gdd/07-tribunal.md), version boîte grise : reveal
    /// incident par incident, votes de TOUS les joueurs (morts inclus),
    /// scoring de la mauvaise foi via le Core. Le receipt vidéo GoPro viendra
    /// remplacer le reveal texte en Phase 2/3 — la structure est la même.
    /// </summary>
    public class TribunalNetworkService : NetworkBehaviour
    {
        public static TribunalNetworkService Instance { get; private set; }

        [Header("Réglages de playtest")]
        [SerializeField] private float _voteSeconds = 15f;
        [SerializeField] private float _revealSeconds = 7f;

        private readonly SyncVar<string> _incidentText = new SyncVar<string>();
        private readonly SyncVar<string> _verdictText = new SyncVar<string>();
        private readonly SyncVar<int> _secondsLeft = new SyncVar<int>();
        private readonly SyncVar<bool> _voteOpen = new SyncVar<bool>();
        private readonly SyncVar<string> _rosterCsv = new SyncVar<string>();

        private readonly Dictionary<int, int> _votes = new Dictionary<int, int>();
        private List<int> _roster = new List<int>();
        private PlayerWatch _localWatch;
        private int _localVote = int.MinValue;

        public bool ServerHasIncidents =>
            PacteNetworkService.Instance != null && PacteNetworkService.Instance.ServerLog.Count > 0;

        private void Awake()
        {
            Instance = this;
        }

        // ==================== SERVEUR ====================

        public void ServerRunTribunal(Action onDone)
        {
            StartCoroutine(ServerTribunalRoutine(onDone));
        }

        private IEnumerator ServerTribunalRoutine(Action onDone)
        {
            var rules = new GameRules();
            var incidents = TribunalScoring.SelectIncidents(PacteNetworkService.Instance.ServerLog, rules);
            _roster = EconomyNetworkService.Instance.ServerSnapshot().Select(s => s.playerId).OrderBy(id => id).ToList();
            _rosterCsv.Value = string.Join(",", _roster);

            _incidentText.Value = "LE TRIBUNAL\n\nLa Direction va rejouer les incidents de la manche.\nVotez avec les touches chiffres. Mentez bien.";
            yield return new WaitForSeconds(4f);

            for (int i = 0; i < incidents.Count; i++)
            {
                var evt = incidents[i];
                _votes.Clear();
                _verdictText.Value = string.Empty;
                _incidentText.Value = $"INCIDENT {i + 1}/{incidents.Count} — à {FormatTime(evt.GameTime)} : {Describe(evt.Type)}.\nAccident… ou Pacte ? QUI ACCUSES-TU ?";

                _voteOpen.Value = true;
                float end = Time.time + _voteSeconds;
                while (Time.time < end && _votes.Count < _roster.Count)
                {
                    _secondsLeft.Value = Mathf.CeilToInt(end - Time.time);
                    yield return null;
                }
                _voteOpen.Value = false;

                var incident = new TribunalIncident
                {
                    Hazard = evt,
                    PacteReward = evt.SourcePacteId > 0
                        ? PacteDefinition.Catalog.First(d => d.Id == evt.SourcePacteId).Reward
                        : 0,
                    Votes = new Dictionary<int, int>(_votes),
                };
                var verdict = TribunalScoring.Resolve(incident, rules, EconomyNetworkService.Instance.ServerLedger);
                EconomyNetworkService.Instance.ServerMirrorAllPockets();
                _verdictText.Value = BuildVerdictText(verdict, incident);

                yield return new WaitForSeconds(_revealSeconds);
            }

            _incidentText.Value = string.Empty;
            _verdictText.Value = string.Empty;
            onDone?.Invoke();
        }

        public void ServerCastVote(int voterId, int suspectId)
        {
            if (!_voteOpen.Value || !_roster.Contains(voterId))
                return;
            if (suspectId != -1 && !_roster.Contains(suspectId))
                return;
            _votes[voterId] = suspectId;
        }

        private static string BuildVerdictText(TribunalVerdict verdict, TribunalIncident incident)
        {
            // Le tir du Juge : pas de déni possible, le tireur est nommé, point.
            if (incident.Hazard.Type == HazardType.GunShot)
            {
                string target = incident.Hazard.TargetPlayerId >= 0 ? $" sur Joueur {incident.Hazard.TargetPlayerId}" : " (dans le vide)";
                return $"VERDICT : LE COUP DE FEU{target}.\nTireur : Joueur {verdict.AuthorPlayerId}.\nLe Juge ne se cache pas. Les flairs justes sont récompensés.";
            }

            if (!verdict.WasPacte)
                return "VERDICT : ACCIDENT NATUREL.\nLa Direction décline toute responsabilité.\nLes accusateurs à tort paient l'amende ; les flairs justes sont récompensés.";

            string author = $"Joueur {verdict.AuthorPlayerId}";
            return verdict.AuthorExposed
                ? $"VERDICT : PACTE.\nCoupable : {author} — DÉMASQUÉ !\nIl rembourse {incident.PacteReward} $ aux votants justes."
                : $"VERDICT : PACTE.\nCoupable : {author} — le groupe n'a rien vu.\nPRIME DE MAUVAISE FOI : +{verdict.BadFaithBonus} $.";
        }

        private static string Describe(HazardType type) => type switch
        {
            HazardType.Blackout => "les lumières ont été coupées",
            HazardType.GasLeak => "une fuite de gaz s'est déclenchée",
            HazardType.DoorJam => "une porte s'est verrouillée",
            HazardType.RadioJam => "une radio a été brouillée",
            HazardType.ElectrifiedFloor => "un sol s'est électrifié",
            HazardType.GunShot => "un coup de feu a claqué",
            _ => "un incident s'est produit",
        };

        private static string FormatTime(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

        // ==================== CLIENT ====================

        private void Update()
        {
            if (!_voteOpen.Value)
                return;

            if (_localWatch == null)
                _localWatch = FindObjectsByType<PlayerWatch>(FindObjectsSortMode.None).FirstOrDefault(w => w.IsOwner);
            if (_localWatch == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            var roster = ParseRoster();
            // 0 = accident naturel ; 1..n = joueurs du roster.
            if (kb.digit0Key.wasPressedThisFrame)
                CastLocalVote(-1);
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
            for (int i = 0; i < digits.Length && i < roster.Count; i++)
            {
                if (kb[digits[i]].wasPressedThisFrame)
                    CastLocalVote(roster[i]);
            }
        }

        private void CastLocalVote(int suspectId)
        {
            _localVote = suspectId;
            _localWatch.SendTribunalVote(suspectId);
        }

        private List<int> ParseRoster() =>
            string.IsNullOrEmpty(_rosterCsv.Value)
                ? new List<int>()
                : _rosterCsv.Value.Split(',').Select(int.Parse).ToList();

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_incidentText.Value) && string.IsNullOrEmpty(_verdictText.Value))
                return;

            var style = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Box(new Rect(Screen.width / 2f - 300, 90, 600, 110), _incidentText.Value, style);

            if (_voteOpen.Value)
            {
                var roster = ParseRoster();
                var lines = new List<string> { $"VOTE ({_secondsLeft.Value}s) — 0 : ACCIDENT NATUREL" };
                for (int i = 0; i < roster.Count; i++)
                    lines.Add($"{i + 1} : Joueur {roster[i]}");
                string myVote = _localVote == int.MinValue ? "aucun" : _localVote == -1 ? "ACCIDENT" : $"Joueur {_localVote}";
                lines.Add($"Ton vote : {myVote}");
                var voteStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
                GUI.Box(new Rect(Screen.width / 2f - 160, 210, 320, 24 + lines.Count * 20), string.Join("\n", lines), voteStyle);
            }
            else
            {
                _localVote = int.MinValue;
            }

            if (!string.IsNullOrEmpty(_verdictText.Value))
            {
                var verdictStyle = new GUIStyle(GUI.skin.box) { fontSize = 19, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                GUI.Box(new Rect(Screen.width / 2f - 300, 230, 600, 110), _verdictText.Value, verdictStyle);
            }
        }
    }
}
