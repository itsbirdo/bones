using System;

namespace Bones.Core
{
    /// <summary>
    /// Abstraction over randomness so game logic stays deterministic and unit-testable.
    /// The presentation layer never rolls dice directly — logic decides outcomes (outcome-first),
    /// and the choreographer animates to the result.
    /// </summary>
    public interface IRng
    {
        /// <summary>Inclusive-exclusive integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Uniform double in [0, 1).</summary>
        double NextDouble();
    }

    public static class RngExtensions
    {
        /// <summary>Roll a single d6 (1..6).</summary>
        public static int D6(this IRng rng) => rng.Range(1, 7);

        /// <summary>True with the given probability (0..1).</summary>
        public static bool Chance(this IRng rng, double probability) => rng.NextDouble() < probability;
    }

    /// <summary>System.Random-backed RNG. Seeded constructor gives reproducible runs/tests.</summary>
    public sealed class SystemRng : IRng
    {
        private readonly Random _random;

        public SystemRng() => _random = new Random();
        public SystemRng(int seed) => _random = new Random(seed);

        public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
        public double NextDouble() => _random.NextDouble();
    }
}
