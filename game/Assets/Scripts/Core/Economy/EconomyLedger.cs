using System.Collections.Generic;
using System.Linq;

namespace BadFaith.Core.Economy
{
    public enum DepositDestination { CommonPot, PersonalPocket }

    /// <summary>
    /// Le grand livre de la manche (docs/gdd/02-economie.md), côté hôte.
    /// Pot Commun (survie collective) vs Poche Perso (victoire individuelle),
    /// et la règle cruelle : un mort verse sa poche au pot.
    /// </summary>
    public sealed class EconomyLedger
    {
        private readonly GameRules _rules;
        private readonly Dictionary<int, int> _pockets = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _insuranceBeneficiary = new Dictionary<int, int>();
        private readonly HashSet<int> _dead = new HashSet<int>();

        public int CommonPot { get; private set; }
        public int Quota { get; private set; }

        public EconomyLedger(GameRules rules, IEnumerable<int> playerIds)
        {
            _rules = rules;
            var ids = playerIds.ToList();
            Quota = _rules.QuotaPerPlayer * ids.Count;
            foreach (var id in ids) _pockets[id] = 0;
        }

        /// <summary>Enregistre un joueur arrivé en cours de lobby : le quota grandit avec l'équipe.</summary>
        public void RegisterPlayer(int playerId)
        {
            if (_pockets.ContainsKey(playerId)) return;
            _pockets[playerId] = 0;
            Quota += _rules.QuotaPerPlayer;
        }

        public int PocketOf(int playerId) => _pockets.GetValueOrDefault(playerId);
        public bool IsDead(int playerId) => _dead.Contains(playerId);
        public bool QuotaReached => CommonPot >= Quota;
        public bool QuotaInGraceRange => CommonPot >= (int)(Quota * _rules.QuotaGraceThreshold);

        /// <summary>Dépôt au Terminal. Retourne le montant affiché publiquement (masqué par le Pacte Dépôt masqué → l'appelant n'affiche rien).</summary>
        public void Deposit(int playerId, int amount, DepositDestination destination)
        {
            if (_dead.Contains(playerId) || amount <= 0) return;
            if (destination == DepositDestination.CommonPot) CommonPot += amount;
            else _pockets[playerId] = _pockets.GetValueOrDefault(playerId) + amount;
        }

        /// <summary>Gain direct en poche (récompense de Pacte, prime, bonus Tribunal).</summary>
        public void Credit(int playerId, int amount)
        {
            if (amount == 0) return;
            _pockets[playerId] = _pockets.GetValueOrDefault(playerId) + amount;
        }

        /// <summary>Débit (amendes, remboursement de Pacte démasqué). Plancher à 0 : pas de dette au MVP.</summary>
        public int Debit(int playerId, int amount)
        {
            int taken = System.Math.Min(amount, _pockets.GetValueOrDefault(playerId));
            _pockets[playerId] = _pockets.GetValueOrDefault(playerId) - taken;
            return taken;
        }

        /// <summary>
        /// La règle cruelle : la poche du mort va au Pot Commun — sauf assurance-vie,
        /// où elle va au bénéficiaire désigné secrètement.
        /// </summary>
        public void OnPlayerDeath(int playerId)
        {
            if (!_dead.Add(playerId)) return;
            int pocket = _pockets.GetValueOrDefault(playerId);
            _pockets[playerId] = 0;
            if (_insuranceBeneficiary.TryGetValue(playerId, out var beneficiary) && !_dead.Contains(beneficiary))
                _pockets[beneficiary] = _pockets.GetValueOrDefault(beneficiary) + pocket;
            else
                CommonPot += pocket;
        }

        public void SetInsuranceBeneficiary(int holder, int beneficiary)
        {
            if (holder != beneficiary) _insuranceBeneficiary[holder] = beneficiary;
        }

        /// <summary>Vainqueur : le survivant EXTRAIT le plus riche (après scoring Tribunal). Retourne -1 si aucun extrait.</summary>
        public int Winner(IReadOnlyCollection<int> extractedPlayers)
        {
            int best = -1, bestPocket = -1;
            foreach (var id in extractedPlayers.Where(p => !_dead.Contains(p)))
            {
                int pocket = _pockets.GetValueOrDefault(id);
                if (pocket > bestPocket) { best = id; bestPocket = pocket; }
            }
            return best;
        }
    }
}
