namespace BadFaith.Core
{
    /// <summary>
    /// Tout l'aléatoire de gameplay passe par ici (jamais de Random direct) :
    /// indispensable pour tester le Directeur d'Accidents et rejouer des manches.
    /// </summary>
    public interface IGameRandom
    {
        /// <summary>Flottant dans [0, 1).</summary>
        float NextFloat();
        /// <summary>Entier dans [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
    }

    public sealed class SeededGameRandom : IGameRandom
    {
        private readonly System.Random _rng;
        public SeededGameRandom(int seed) { _rng = new System.Random(seed); }
        public float NextFloat() => (float)_rng.NextDouble();
        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public float Range(float min, float max) => min + (max - min) * (float)_rng.NextDouble();
    }
}
