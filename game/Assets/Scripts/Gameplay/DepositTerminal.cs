using System.Collections;
using BadFaith.Core.Economy;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Le Terminal central (docs/gdd/02-economie.md). Le dépôt est un geste
    /// PUBLIC : le montant et la destination s'affichent sur l'écran pendant
    /// 10 s — espionner les dépôts est un gameplay.
    /// </summary>
    public class DepositTerminal : NetworkBehaviour
    {
        public static DepositTerminal Instance { get; private set; }

        [SerializeField] private float _depositRange = 4f;

        private readonly SyncVar<string> _display = new SyncVar<string>();
        private Coroutine _clearRoutine;
        private TextMesh[] _screens;

        public float DepositRange => _depositRange;

        private void Awake()
        {
            Instance = this;
            _screens = GetComponentsInChildren<TextMesh>();
        }

        /// <summary>Serveur : dépôt validé (distance + objet tenu vérifiés par l'appelant côté serveur).</summary>
        public void ServerDeposit(int playerId, string playerName, LootItem loot, DepositDestination destination)
        {
            EconomyNetworkService.Instance.ServerDeposit(playerId, loot.Value, destination);

            // L'affichage public — le cœur du dilemme. (Le Pacte "Dépôt masqué" le videra plus tard.)
            _display.Value = destination == DepositDestination.CommonPot
                ? $"+{loot.Value} $ -> POT COMMUN\n({playerName})"
                : $"+{loot.Value} $ -> POCHE PERSO\n({playerName})";
            if (_clearRoutine != null)
                StopCoroutine(_clearRoutine);
            _clearRoutine = StartCoroutine(ClearDisplayAfter(10f));
        }

        private IEnumerator ClearDisplayAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _display.Value = string.Empty;
            _clearRoutine = null;
        }

        private void Update()
        {
            if (_screens == null || _screens.Length == 0)
                return;
            string header = EconomyNetworkService.Instance != null
                ? $"TERMINAL\nPOT : {EconomyNetworkService.Instance.Pot} $ / {EconomyNetworkService.Instance.Quota} $"
                : "TERMINAL";
            string text = string.IsNullOrEmpty(_display.Value) ? header : $"{header}\n\n{_display.Value}";
            foreach (var screen in _screens)
                screen.text = text;
        }
    }
}
