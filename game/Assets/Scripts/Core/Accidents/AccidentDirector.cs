using System.Collections.Generic;
using System.Linq;
using BadFaith.Core.Hazards;

namespace BadFaith.Core.Accidents
{
    /// <summary>
    /// Le Directeur d'Accidents (docs/gdd/04-deni-plausible.md), côté hôte.
    /// Maintient le taux de base d'événements NATURELS qui rend toute
    /// accusation contestable. Trois règles : indistinguabilité (il émet les
    /// mêmes HazardEvent que les Pactes), lissage anti-cluster, génération
    /// d'ambiguïté active.
    /// </summary>
    public sealed class AccidentDirector
    {
        // Espérance d'occurrences naturelles par manche de 12 min, par type (valeurs GDD).
        private static readonly Dictionary<HazardType, float> BaseRatePerRound = new Dictionary<HazardType, float>
        {
            { HazardType.Blackout, 1.0f },
            { HazardType.DoorJam, 2.0f },
            { HazardType.GasLeak, 0.8f },
            { HazardType.ElectrifiedFloor, 0.6f },
            { HazardType.RadioJam, 1.2f },
            { HazardType.TerminalDisplayFault, 0.8f },
        };

        private readonly GameRules _rules;
        private readonly IGameRandom _rng;
        private readonly Dictionary<HazardType, float> _suppressedUntil = new Dictionary<HazardType, float>();
        private float _ambiguityBoostUntil = -1f;
        private HazardType? _ambiguityBoostType;

        public AccidentDirector(GameRules rules, IGameRandom rng) { _rules = rules; _rng = rng; }

        /// <summary>Appelé quand un Pacte vient de déclencher un hazard : supprime les naturels du même type (anti-cluster).</summary>
        public void OnPacteHazardFired(HazardType type, float now)
        {
            _suppressedUntil[type] = now + _rules.AntiClusterSuppressionSeconds;
        }

        /// <summary>Appelé à l'ACCEPTATION d'un Pacte : booste un naturel d'un AUTRE type pour créer une seconde explication possible.</summary>
        public void OnPacteAccepted(HazardType? pacteConsequence, float now)
        {
            var candidates = BaseRatePerRound.Keys.Where(t => t != pacteConsequence).ToList();
            _ambiguityBoostType = candidates[_rng.NextInt(0, candidates.Count)];
            _ambiguityBoostUntil = now + _rules.AmbiguityBoostWindowSeconds;
        }

        /// <summary>
        /// Tick hôte (dt en secondes). Tire les accidents naturels de cette frame.
        /// Processus de Poisson discretisé : p = taux_par_seconde * dt.
        /// </summary>
        public List<HazardEvent> Tick(float now, float dt, int zoneCount, IReadOnlyList<int> alivePlayers)
        {
            var events = new List<HazardEvent>();
            foreach (var kv in BaseRatePerRound)
            {
                var type = kv.Key;
                if (_suppressedUntil.TryGetValue(type, out var until) && now < until) continue;

                float perSecond = kv.Value / _rules.ExpeditionSeconds * _rules.NaturalAccidentRate;
                if (_ambiguityBoostType == type && now < _ambiguityBoostUntil)
                    perSecond *= _rules.AmbiguityBoostMultiplier;

                if (_rng.NextFloat() < perSecond * dt)
                {
                    events.Add(new HazardEvent
                    {
                        Type = type,
                        GameTime = now,
                        ZoneId = _rng.NextInt(0, zoneCount),
                        TargetPlayerId = type == HazardType.RadioJam && alivePlayers.Count > 0
                            ? alivePlayers[_rng.NextInt(0, alivePlayers.Count)] : -1,
                        DurationSeconds = _rng.Range(30f, 60f),
                        Origin = HazardOrigin.Natural,
                    });
                    // Un accident naturel supprime aussi ses propres répétitions immédiates.
                    _suppressedUntil[type] = now + _rules.AntiClusterSuppressionSeconds;
                }
            }
            return events;
        }
    }
}
