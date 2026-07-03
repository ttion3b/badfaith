using System.Collections.Generic;
using System.Linq;
using BadFaith.Core;
using BadFaith.Core.Accidents;
using BadFaith.Core.Hazards;
using BadFaith.Core.Pactes;
using FishNet.Component.Spawning;
using FishNet.Object;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Pont réseau (côté hôte uniquement) entre FishNet et la logique de domaine :
    /// PacteService (distribution des offres) + AccidentDirector (déni plausible).
    /// Les clients ne voient passer que des RPC — jamais l'origine des événements.
    /// </summary>
    public class PacteNetworkService : NetworkBehaviour
    {
        public static PacteNetworkService Instance { get; private set; }

        [Header("Réglages de playtest (voir docs/gdd/04-deni-plausible.md)")]
        [SerializeField] private float _offerIntervalSeconds = 45f;
        [SerializeField] private float _naturalAccidentRate = 2.5f;

        private GameRules _rules;
        private PacteService _pactes;
        private AccidentDirector _accidents;
        private HazardExecutor _executor;
        private readonly Dictionary<int, PlayerWatch> _watches = new Dictionary<int, PlayerWatch>();
        /// <summary>Journal complet de la manche — la matière première du futur Tribunal.</summary>
        private readonly List<HazardEvent> _log = new List<HazardEvent>();

        private void Awake()
        {
            Instance = this;
            _executor = GetComponent<HazardExecutor>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rules = new GameRules
            {
                // Cadence resserrée pour le playtest boîte grise (2-3 min au MVP).
                PacteOfferInterval = _offerIntervalSeconds,
                NaturalAccidentRate = _naturalAccidentRate,
            };
            var rng = new SeededGameRandom(System.Environment.TickCount);

            // Seuls les Pactes à conséquence implémentée en boîte grise sont distribués.
            var catalog = PacteDefinition.Catalog
                .Where(d => d.IsGift || d.Consequence == HazardType.Blackout || d.Consequence == HazardType.GasLeak)
                .ToList();
            _pactes = new PacteService(_rules, rng, catalog);
            _accidents = new AccidentDirector(_rules, rng);

            var spawner = FindAnyObjectByType<PlayerSpawner>();
            if (spawner != null)
                spawner.OnSpawned += ServerOnPlayerSpawned;
        }

        private void ServerOnPlayerSpawned(NetworkObject nob)
        {
            var watch = nob.GetComponent<PlayerWatch>();
            if (watch == null)
                return;
            _watches[nob.OwnerId] = watch;
            _pactes.RegisterPlayer(nob.OwnerId, Time.time);
        }

        private void Update()
        {
            if (!IsServerInitialized || _pactes == null)
                return;

            float now = Time.time;

            // Purge des déconnectés ; les morts ne reçoivent plus d'offres.
            foreach (var gone in _watches.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList())
                _watches.Remove(gone);
            var players = _watches
                .Where(kv =>
                {
                    var health = kv.Value.GetComponent<PlayerHealth>();
                    return health == null || !health.IsDead;
                })
                .Select(kv => kv.Key)
                .ToList();

            // 1. Nouvelles offres → montres.
            foreach (var offer in _pactes.Tick(now, players))
            {
                var def = PacteDefinition.Catalog.First(d => d.Id == offer.PacteId);
                if (_watches.TryGetValue(offer.PlayerId, out var watch))
                    watch.ServerSendOffer(offer.OfferId, def.NameFr, def.Reward, _rules.PacteOfferExpirySeconds);
            }

            // 2. Conséquences de Pactes arrivées à échéance.
            foreach (var due in _pactes.DueConsequences(now))
            {
                var def = PacteDefinition.Catalog.First(d => d.Id == due.PacteId);
                if (def.Consequence == null)
                    continue;
                var evt = new HazardEvent
                {
                    Type = def.Consequence.Value,
                    GameTime = now,
                    ZoneId = due.TargetZoneId,
                    TargetPlayerId = due.TargetPlayerId,
                    DurationSeconds = def.ConsequenceDurationSeconds,
                    Origin = HazardOrigin.Pacte,
                    AuthorPlayerId = due.PlayerId,
                    SourcePacteId = due.PacteId,
                };
                _log.Add(evt);
                _executor.ServerExecute(evt);
                _accidents.OnPacteHazardFired(evt.Type, now);
            }

            // 3. Accidents naturels — le carburant du déni plausible.
            foreach (var evt in _accidents.Tick(now, Time.deltaTime, zoneCount: 1, players))
            {
                _log.Add(evt);
                _executor.ServerExecute(evt);
            }
        }

        /// <summary>Appelé par la montre (ServerRpc) quand son porteur accepte une offre.</summary>
        public bool ServerTryAccept(PlayerWatch watch, int offerId)
        {
            var offer = _pactes.Offers.FirstOrDefault(o => o.OfferId == offerId);
            if (offer == null || offer.PlayerId != watch.OwnerId)
                return false;

            var def = PacteDefinition.Catalog.First(d => d.Id == offer.PacteId);
            // Boîte grise mono-zone : les Pactes de zone ciblent l'arène entière (zone 0).
            int zone = def.Targeting == PacteTargeting.Zone ? 0 : -1;
            if (!_pactes.Accept(offerId, Time.time, targetZoneId: zone))
                return false;

            EconomyNetworkService.Instance.ServerCredit(watch.OwnerId, def.Reward);
            if (def.Consequence != null)
                _accidents.OnPacteAccepted(def.Consequence, Time.time);
            return true;
        }
    }
}
