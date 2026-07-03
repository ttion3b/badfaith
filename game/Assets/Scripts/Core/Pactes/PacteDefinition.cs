using System.Collections.Generic;
using BadFaith.Core.Hazards;

namespace BadFaith.Core.Pactes
{
    public enum PacteTargeting { None, Player, Zone }

    /// <summary>Définition statique d'un Pacte. Le catalogue MVP est verrouillé à 8 (risque R8).</summary>
    public sealed class PacteDefinition
    {
        public int Id;
        public string NameFr;
        public int Reward;                 // $ en Poche Perso à l'acceptation
        public HazardType? Consequence;    // null = Pacte sans conséquence (Le Cadeau, Dépôt masqué)
        public PacteTargeting Targeting;
        public float ConsequenceDurationSeconds;
        /// <summary>Poids de tirage ; le Cadeau garde un poids élevé pour rester un alibi statistique crédible.</summary>
        public float DrawWeight = 1f;
        public bool IsGift;                // "gentil" : gain sans conséquence

        public static readonly IReadOnlyList<PacteDefinition> Catalog = new[]
        {
            new PacteDefinition { Id = 1, NameFr = "Blackout",              Reward = 2000, Consequence = HazardType.Blackout,         Targeting = PacteTargeting.Zone,   ConsequenceDurationSeconds = 60f },
            new PacteDefinition { Id = 2, NameFr = "Porte grippée",         Reward = 1500, Consequence = HazardType.DoorJam,          Targeting = PacteTargeting.Zone,   ConsequenceDurationSeconds = 90f },
            new PacteDefinition { Id = 3, NameFr = "Brouillage",            Reward = 2500, Consequence = HazardType.RadioJam,         Targeting = PacteTargeting.Player, ConsequenceDurationSeconds = 45f },
            new PacteDefinition { Id = 4, NameFr = "Prime sur tête",        Reward = 0,    Consequence = null,                        Targeting = PacteTargeting.Player, ConsequenceDurationSeconds = 0f },
            new PacteDefinition { Id = 5, NameFr = "Fuite de gaz",          Reward = 3000, Consequence = HazardType.GasLeak,          Targeting = PacteTargeting.Zone,   ConsequenceDurationSeconds = 90f },
            new PacteDefinition { Id = 6, NameFr = "Dépôt masqué",          Reward = 500,  Consequence = null,                        Targeting = PacteTargeting.None,   ConsequenceDurationSeconds = 0f },
            new PacteDefinition { Id = 7, NameFr = "Doigt de la Direction", Reward = 4000, Consequence = HazardType.ElectrifiedFloor, Targeting = PacteTargeting.Zone,   ConsequenceDurationSeconds = 30f },
            new PacteDefinition { Id = 8, NameFr = "Le Cadeau",             Reward = 1000, Consequence = null,                        Targeting = PacteTargeting.None,   ConsequenceDurationSeconds = 0f, IsGift = true, DrawWeight = 1.2f },
        };
    }
}
