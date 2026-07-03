using System.Collections.Generic;
using System.Linq;

namespace BadFaith.Core.Pactes
{
    public enum PacteOfferState { Pending, Accepted, Expired, Declined }

    /// <summary>Une offre concrète envoyée à un joueur (instance d'un PacteDefinition).</summary>
    public sealed class PacteOffer
    {
        public int OfferId;
        public int PacteId;
        public int PlayerId;
        public float OfferedAt;
        public float ExpiresAt;
        public PacteOfferState State = PacteOfferState.Pending;
        public float AcceptedAt = -1f;
        public float ConsequenceAt = -1f;   // OfferedAt + délai tiré dans [PacteDelayMin, PacteDelayMax]
        public int TargetPlayerId = -1;     // choisi par l'acheteur si Targeting == Player
        public int TargetZoneId = -1;       // choisi par l'acheteur si Targeting == Zone
    }

    /// <summary>
    /// Côté hôte uniquement. Applique les règles de distribution du GDD
    /// (docs/gdd/03-pactes.md) : intervalle par joueur, unicité simultanée,
    /// pitié anti-ciblage, expiration.
    /// </summary>
    public sealed class PacteService
    {
        private readonly GameRules _rules;
        private readonly IGameRandom _rng;
        private readonly List<PacteOffer> _offers = new List<PacteOffer>();
        private readonly Dictionary<int, float> _nextOfferTime = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _consecutiveTargetCount = new Dictionary<int, int>();
        private int _lastTargetedPlayer = -1;
        private int _nextOfferId = 1;

        public PacteService(GameRules rules, IGameRandom rng) { _rules = rules; _rng = rng; }

        public IReadOnlyList<PacteOffer> Offers => _offers;

        public void RegisterPlayer(int playerId, float now)
        {
            // Première offre décalée aléatoirement pour désynchroniser les montres.
            _nextOfferTime[playerId] = now + _rng.Range(60f, _rules.PacteOfferInterval);
        }

        /// <summary>À appeler chaque tick hôte. Retourne les nouvelles offres à pousser sur les montres.</summary>
        public List<PacteOffer> Tick(float now, IReadOnlyList<int> alivePlayers)
        {
            var newOffers = new List<PacteOffer>();
            foreach (var offer in _offers)
                if (offer.State == PacteOfferState.Pending && now >= offer.ExpiresAt)
                    offer.State = PacteOfferState.Expired;

            foreach (var pid in alivePlayers)
            {
                if (!_nextOfferTime.TryGetValue(pid, out var t) || now < t) continue;
                if (_offers.Any(o => o.PlayerId == pid && o.State == PacteOfferState.Pending)) continue;

                var def = Draw(pid);
                if (def == null) continue;

                var offer = new PacteOffer
                {
                    OfferId = _nextOfferId++,
                    PacteId = def.Id,
                    PlayerId = pid,
                    OfferedAt = now,
                    ExpiresAt = now + _rules.PacteOfferExpirySeconds,
                };
                _offers.Add(offer);
                newOffers.Add(offer);
                _nextOfferTime[pid] = now + _rules.PacteOfferInterval * _rng.Range(0.8f, 1.2f);
            }
            return newOffers;
        }

        /// <summary>Tirage pondéré, en excluant les Pactes déjà en attente chez un autre joueur (unicité de l'anonymat).</summary>
        private PacteDefinition Draw(int forPlayerId)
        {
            var pendingIds = _offers.Where(o => o.State == PacteOfferState.Pending).Select(o => o.PacteId).ToHashSet();
            var pool = PacteDefinition.Catalog.Where(d => !pendingIds.Contains(d.Id)).ToList();
            if (pool.Count == 0) return null;

            float total = pool.Sum(d => d.DrawWeight);
            float roll = _rng.NextFloat() * total;
            foreach (var d in pool)
            {
                roll -= d.DrawWeight;
                if (roll <= 0f) return d;
            }
            return pool[pool.Count - 1];
        }

        /// <summary>Le joueur accepte sur sa montre. Retourne false si l'offre n'est plus valide ou si la cible viole la pitié.</summary>
        public bool Accept(int offerId, float now, int targetPlayerId = -1, int targetZoneId = -1)
        {
            var offer = _offers.FirstOrDefault(o => o.OfferId == offerId);
            if (offer == null || offer.State != PacteOfferState.Pending || now >= offer.ExpiresAt) return false;

            var def = PacteDefinition.Catalog.First(d => d.Id == offer.PacteId);
            if (def.Targeting == PacteTargeting.Player)
            {
                if (targetPlayerId < 0 || targetPlayerId == offer.PlayerId) return false;
                // Pitié : pas plus de MaxConsecutiveTargeting fois de suite sur la même tête.
                if (targetPlayerId == _lastTargetedPlayer &&
                    _consecutiveTargetCount.GetValueOrDefault(targetPlayerId) >= _rules.MaxConsecutiveTargeting)
                    return false;
                _consecutiveTargetCount[targetPlayerId] =
                    targetPlayerId == _lastTargetedPlayer ? _consecutiveTargetCount.GetValueOrDefault(targetPlayerId) + 1 : 1;
                _lastTargetedPlayer = targetPlayerId;
                offer.TargetPlayerId = targetPlayerId;
            }
            else if (def.Targeting == PacteTargeting.Zone)
            {
                if (targetZoneId < 0) return false;
                offer.TargetZoneId = targetZoneId;
            }

            offer.State = PacteOfferState.Accepted;
            offer.AcceptedAt = now;
            if (def.Consequence != null)
                offer.ConsequenceAt = now + _rng.Range(_rules.PacteDelayMin, _rules.PacteDelayMax);
            return true;
        }

        /// <summary>Pactes acceptés dont la conséquence est due à cet instant (à transformer en HazardEvent).</summary>
        public List<PacteOffer> DueConsequences(float now)
        {
            var due = _offers.Where(o => o.State == PacteOfferState.Accepted && o.ConsequenceAt > 0f && now >= o.ConsequenceAt).ToList();
            foreach (var o in due) o.ConsequenceAt = -1f; // consommé
            return due;
        }
    }
}
