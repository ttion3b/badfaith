namespace BadFaith.Core
{
    /// <summary>
    /// Toutes les constantes d'équilibrage du MVP en un seul endroit.
    /// Les 4 variables prioritaires de playtest (docs/gdd/04-deni-plausible.md)
    /// sont regroupées en tête et exposées dans le menu de playtest.
    /// </summary>
    public class GameRules
    {
        // ---- Les 4 variables de tuning prioritaires ----
        /// <summary>Multiplicateur global du taux d'accidents naturels (1.0 = valeurs de base du GDD).</summary>
        public float NaturalAccidentRate = 1.0f;
        /// <summary>Délai min/max (secondes) entre acceptation d'un Pacte et sa conséquence.</summary>
        public float PacteDelayMin = 20f, PacteDelayMax = 90f;
        /// <summary>Portée (mètres) à laquelle la vibration de montre est audible par les autres.</summary>
        public float WatchBuzzAudibleRange = 2f;
        /// <summary>Intervalle moyen (secondes) entre deux offres de Pacte pour un même joueur.</summary>
        public float PacteOfferInterval = 150f;

        // ---- Économie (docs/gdd/02-economie.md) ----
        public int QuotaPerPlayer = 10_000;
        public float MapLootToQuotaRatio = 1.5f;
        public int FalseAccusationFine = 500;
        public int CorrectVerdictReward = 300;
        /// <summary>Bonus de mensonge réussi : fraction du gain du Pacte (0.5 = +50 %).</summary>
        public float BadFaithBonusRatio = 0.5f;

        // ---- Manche (docs/gdd/01-core-loop.md) ----
        public float InsertionSeconds = 60f;
        public float ExpeditionSeconds = 12 * 60f;
        public float ExtractionWindowSeconds = 120f;
        public float RedButtonUnlockSeconds = 30f;
        public float RedButtonCountdownSeconds = 10f;
        /// <summary>La capsule vient quand même en fin de timer si le pot atteint cette fraction du quota.</summary>
        public float QuotaGraceThreshold = 0.8f;

        // ---- Capsule (docs/gdd/06-extraction.md) : sièges = joueurs - 2 ----
        public int CapsuleSeats(int playerCount) => System.Math.Max(1, playerCount - 2);

        // ---- Pactes (docs/gdd/03-pactes.md) ----
        public float PacteOfferExpirySeconds = 40f;
        /// <summary>Un joueur ciblé N fois de suite ne peut plus l'être (pitié).</summary>
        public int MaxConsecutiveTargeting = 2;

        // ---- Accidents naturels (docs/gdd/04-deni-plausible.md) ----
        /// <summary>Après un événement de Pacte, suppression des naturels du même type pendant N s.</summary>
        public float AntiClusterSuppressionSeconds = 90f;
        /// <summary>Après un Pacte accepté, boost de proba d'un naturel d'un AUTRE type (ambiguïté).</summary>
        public float AmbiguityBoostMultiplier = 1.5f;
        public float AmbiguityBoostWindowSeconds = 120f;

        // ---- Tribunal (docs/gdd/07-tribunal.md) ----
        public int TribunalMinIncidents = 4, TribunalMaxIncidents = 6;
        public float AccusationPhaseSeconds = 20f;
        public float DefensePhaseSeconds = 15f;
        public float ReceiptSeconds = 10f;
    }
}
