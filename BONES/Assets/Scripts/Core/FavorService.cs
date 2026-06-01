using System.Collections.Generic;

namespace Bones.Core
{
    /// <summary>
    /// Pure helper for favor math. A suspicion-reducing favor lowers the standing bust % while it
    /// has charges left; the reductions sum and feed <see cref="SuspicionService.BustChance"/>.
    /// Engine-free so it can be unit-tested without Unity. Spec §9.2.
    /// </summary>
    public static class FavorService
    {
        /// <summary>
        /// Total suspicion reduction from active favors: sum of magnitude over entries with at least
        /// one charge. Zero-charge favors contribute nothing. Clamped to be non-negative.
        /// </summary>
        public static double SuspicionReduction(IEnumerable<(double magnitude, int charges)> favors)
        {
            if (favors == null) return 0.0;
            double sum = 0.0;
            foreach (var (magnitude, charges) in favors)
                if (charges > 0 && magnitude > 0.0) sum += magnitude;
            return sum < 0.0 ? 0.0 : sum;
        }
    }
}
