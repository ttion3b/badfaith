using System.Collections.Generic;
using System.Linq;
using BadFaith.Core.Economy;
using BadFaith.Core.Hazards;

namespace BadFaith.Core.Tribunal
{
    /// <summary>Un incident sélectionné pour le Tribunal, avec les votes de la table.</summary>
    public sealed class TribunalIncident
    {
        public HazardEvent Hazard;
        public int PacteReward;                       // gain que le coupable avait touché (0 si naturel)
        /// <summary>playerId votant → playerId accusé, ou -1 pour "accident naturel".</summary>
        public Dictionary<int, int> Votes = new Dictionary<int, int>();
    }

    public sealed class TribunalVerdict
    {
        public bool WasPacte;
        public int AuthorPlayerId = -1;
        public bool AuthorExposed;                    // désigné majoritairement
        public int BadFaithBonus;                     // prime de mensonge réussi
        public Dictionary<int, int> Deltas = new Dictionary<int, int>(); // playerId → variation de poche
    }

    /// <summary>
    /// Le scoring du Tribunal (docs/gdd/07-tribunal.md) : le mensonge est scoré.
    /// Pur calcul — le séquencement/réveal est fait par la couche Unity.
    /// </summary>
    public static class TribunalScoring
    {
        public static TribunalVerdict Resolve(TribunalIncident incident, GameRules rules, EconomyLedger ledger)
        {
            var v = new TribunalVerdict
            {
                WasPacte = incident.Hazard.Origin == HazardOrigin.Pacte,
                AuthorPlayerId = incident.Hazard.AuthorPlayerId,
            };

            void Add(int player, int delta)
            {
                v.Deltas[player] = v.Deltas.GetValueOrDefault(player) + delta;
                if (delta >= 0) ledger.Credit(player, delta); else ledger.Debit(player, -delta);
            }

            if (v.WasPacte)
            {
                var accusers = incident.Votes.Where(kv => kv.Value == v.AuthorPlayerId).Select(kv => kv.Key).ToList();
                v.AuthorExposed = accusers.Count * 2 > incident.Votes.Count; // majorité stricte

                if (v.AuthorExposed)
                {
                    // Démasqué : rembourse son gain, réparti entre les votants justes.
                    int refunded = ledger.Debit(v.AuthorPlayerId, incident.PacteReward);
                    v.Deltas[v.AuthorPlayerId] = v.Deltas.GetValueOrDefault(v.AuthorPlayerId) - refunded;
                    if (accusers.Count > 0)
                    {
                        int share = refunded / accusers.Count;
                        foreach (var a in accusers) Add(a, share);
                    }
                }
                else
                {
                    // Prime de mauvaise foi : mentir avec succès rapporte.
                    v.BadFaithBonus = (int)(incident.PacteReward * rules.BadFaithBonusRatio);
                    Add(v.AuthorPlayerId, v.BadFaithBonus);
                }

                // Flair : tous ceux qui ont visé juste (même sans majorité).
                foreach (var a in accusers) Add(a, rules.CorrectVerdictReward);
            }
            else
            {
                // Accident naturel : amende pour chaque accusation à tort, flair pour ceux qui ont voté "accident".
                foreach (var kv in incident.Votes)
                {
                    if (kv.Value == -1) Add(kv.Key, rules.CorrectVerdictReward);
                    else Add(kv.Key, -rules.FalseAccusationFine);
                }
            }
            return v;
        }

        /// <summary>
        /// Sélection des incidents par score de drama (docs/gdd/07-tribunal.md) :
        /// Pacte > mort > accident avec dégâts. Le tir du Juge et le bouton rouge
        /// sont injectés d'office par l'appelant.
        /// </summary>
        public static List<HazardEvent> SelectIncidents(IEnumerable<HazardEvent> all, GameRules rules)
        {
            return all
                .OrderByDescending(h => h.Origin == HazardOrigin.Pacte ? 2 : 1)
                .ThenByDescending(h => h.GameTime)
                .Take(rules.TribunalMaxIncidents)
                .OrderBy(h => h.GameTime)
                .ToList();
        }
    }
}
