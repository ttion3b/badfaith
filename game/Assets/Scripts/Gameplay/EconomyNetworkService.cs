using BadFaith.Core;
using BadFaith.Core.Economy;
using FishNet.Component.Spawning;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Pont réseau du grand livre (docs/gdd/02-economie.md). L'EconomyLedger
    /// (Core) est LA source de vérité, côté hôte uniquement. Le pot commun et
    /// le quota sont publics ; les poches perso sont mirrorées vers chaque
    /// montre (SyncVar OwnerOnly).
    /// </summary>
    public class EconomyNetworkService : NetworkBehaviour
    {
        public static EconomyNetworkService Instance { get; private set; }

        [Header("Réglages de playtest")]
        [Tooltip("Quota ajouté au pot commun requis par joueur connecté (10 000 au MVP).")]
        [SerializeField] private int _quotaPerPlayer = 3000;

        private GameRules _rules;
        private EconomyLedger _ledger;
        private readonly SyncVar<int> _pot = new SyncVar<int>();
        private readonly SyncVar<int> _quota = new SyncVar<int>();
        private readonly System.Collections.Generic.Dictionary<int, PlayerWatch> _watches = new System.Collections.Generic.Dictionary<int, PlayerWatch>();

        public int Pot => _pot.Value;
        public int Quota => _quota.Value;
        public bool QuotaReached => _ledger != null && _ledger.QuotaReached;

        private void Awake()
        {
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _rules = new GameRules { QuotaPerPlayer = _quotaPerPlayer };
            _ledger = new EconomyLedger(_rules, System.Linq.Enumerable.Empty<int>());

            var spawner = FindAnyObjectByType<PlayerSpawner>();
            if (spawner != null)
                spawner.OnSpawned += ServerOnPlayerSpawned;
        }

        private void ServerOnPlayerSpawned(NetworkObject nob)
        {
            _ledger.RegisterPlayer(nob.OwnerId);
            _quota.Value = _ledger.Quota;
            var watch = nob.GetComponent<PlayerWatch>();
            if (watch != null)
                _watches[nob.OwnerId] = watch;
            MirrorPocket(nob.OwnerId);
        }

        /// <summary>Serveur : gain direct en poche (récompense de Pacte, prime, bonus).</summary>
        public void ServerCredit(int playerId, int amount)
        {
            _ledger.Credit(playerId, amount);
            MirrorPocket(playerId);
        }

        /// <summary>Serveur : dépôt au Terminal.</summary>
        public void ServerDeposit(int playerId, int amount, DepositDestination destination)
        {
            _ledger.Deposit(playerId, amount, destination);
            _pot.Value = _ledger.CommonPot;
            MirrorPocket(playerId);
        }

        /// <summary>Serveur : la règle cruelle — la poche du mort part au pot.</summary>
        public void ServerOnPlayerDeath(int playerId)
        {
            _ledger.OnPlayerDeath(playerId);
            _pot.Value = _ledger.CommonPot;
            foreach (var id in _watches.Keys)
                MirrorPocket(id);
        }

        private void MirrorPocket(int playerId)
        {
            if (_watches.TryGetValue(playerId, out var watch) && watch != null)
                watch.ServerSetPocket(_ledger.PocketOf(playerId));
        }

        private void OnGUI()
        {
            if (_quota.Value <= 0)
                return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width / 2f - 130, 8, 260, 30), $"POT COMMUN : {_pot.Value} $ / {_quota.Value} $", style);
        }
    }
}
