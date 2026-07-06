namespace BadFaith.Core.Hazards
{
    /// <summary>
    /// Un seul enum pour naturel ET Pacte : règle d'indistinguabilité
    /// (docs/gdd/04-deni-plausible.md). Le code qui exécute un hazard ne doit
    /// JAMAIS brancher sur l'origine.
    /// </summary>
    public enum HazardType
    {
        Blackout,        // lumières d'une aile
        DoorJam,         // porte verrouillée temporairement
        RadioJam,        // micro d'un joueur coupé/grésillant
        GasLeak,         // zone toxique
        ElectrifiedFloor,// couloir électrifié
        TerminalDisplayFault, // panne d'affichage du Terminal (déni des dépôts)
        GunShot,         // le tir du Juge — jamais naturel, jamais déniable (docs/gdd/05-le-juge.md)
    }

    public enum HazardOrigin { Natural, Pacte }

    /// <summary>
    /// Événement horodaté. L'origine et l'auteur n'existent que dans le log
    /// du Tribunal (côté hôte) — jamais répliqués aux clients pendant la manche.
    /// </summary>
    public sealed class HazardEvent
    {
        public HazardType Type;
        public float GameTime;          // secondes depuis le début de la manche
        public int ZoneId;              // aile/salle/couloir ciblé (-1 = global)
        public int TargetPlayerId = -1; // pour RadioJam
        public float DurationSeconds;

        // --- Secret hôte, consommé uniquement par le TribunalDirector ---
        public HazardOrigin Origin;
        public int AuthorPlayerId = -1; // -1 si Natural
        public int SourcePacteId = -1;  // -1 si Natural
    }
}
