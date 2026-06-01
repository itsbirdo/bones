using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class CeeloEngineTests
    {
        // ---- Categorization ----

        [Test]
        public void FourFiveSix_InAnyOrder_IsInstantWin()
        {
            Assert.AreEqual(CeeloKind.FourFiveSix, CeeloEngine.Evaluate(4, 5, 6).Kind);
            Assert.AreEqual(CeeloKind.FourFiveSix, CeeloEngine.Evaluate(6, 4, 5).Kind);
            Assert.AreEqual(CeeloKind.FourFiveSix, CeeloEngine.Evaluate(5, 6, 4).Kind);
        }

        [Test]
        public void Triples_AreWins_AndRankAboveFourFiveSix()
        {
            var triple = CeeloEngine.Evaluate(2, 2, 2);
            Assert.AreEqual(CeeloKind.Triple, triple.Kind);
            Assert.AreEqual(2, triple.Value);
            Assert.Greater(triple.Rank, CeeloEngine.Evaluate(4, 5, 6).Rank);
            Assert.Greater(CeeloEngine.Evaluate(6, 6, 6).Rank, CeeloEngine.Evaluate(1, 1, 1).Rank);
        }

        [Test]
        public void OneTwoThree_IsInstantLoss()
        {
            Assert.AreEqual(CeeloKind.InstantLoss, CeeloEngine.Evaluate(1, 2, 3).Kind);
            Assert.AreEqual(CeeloKind.InstantLoss, CeeloEngine.Evaluate(3, 1, 2).Kind);
        }

        [Test]
        public void Point_IsTheOddDie()
        {
            Assert.AreEqual(5, CeeloEngine.Evaluate(3, 3, 5).Value);
            Assert.AreEqual(2, CeeloEngine.Evaluate(6, 6, 2).Value);
            Assert.AreEqual(CeeloKind.Point, CeeloEngine.Evaluate(3, 3, 5).Kind);
        }

        [Test]
        public void NonScoring_IsNothing()
        {
            Assert.AreEqual(CeeloKind.Nothing, CeeloEngine.Evaluate(2, 3, 4).Kind);
            Assert.AreEqual(CeeloKind.Nothing, CeeloEngine.Evaluate(1, 4, 5).Kind);
        }

        // ---- The 216 sample space (spec exact counts) ----

        [Test]
        public void OutcomeDistribution_MatchesThe216SampleSpace()
        {
            int win = 0, loss = 0, point = 0, nothing = 0;
            for (int a = 1; a <= 6; a++)
            for (int b = 1; b <= 6; b++)
            for (int c = 1; c <= 6; c++)
            {
                switch (CeeloEngine.Evaluate(a, b, c).Kind)
                {
                    case CeeloKind.FourFiveSix:
                    case CeeloKind.Triple: win++; break;
                    case CeeloKind.InstantLoss: loss++; break;
                    case CeeloKind.Point: point++; break;
                    default: nothing++; break;
                }
            }
            Assert.AreEqual(12, win, "instant wins (4-5-6 + triples)");
            Assert.AreEqual(6, loss, "1-2-3");
            Assert.AreEqual(90, point, "points");
            Assert.AreEqual(108, nothing, "re-rolls");
        }

        // ---- Round resolution ----

        [Test]
        public void BankerInstantWin_BeatsAnything_MarkNeverRolls()
        {
            var banker = CeeloEngine.Evaluate(4, 5, 6);
            var markTriple = CeeloEngine.Evaluate(6, 6, 6);
            // Banker rolled first and hit 4-5-6: per the round flow the mark never rolls,
            // so the banker wins regardless of what we pass as the mark.
            Assert.AreEqual(GameOutcome.BankerWin, CeeloEngine.ResolveRound(banker, markTriple, TieRule.Push));
        }

        [Test]
        public void HigherPoint_Wins_LowerPoint_Loses()
        {
            var bankerSix = CeeloEngine.Evaluate(2, 2, 6); // point 6
            var markFour = CeeloEngine.Evaluate(1, 1, 4);  // point 4
            Assert.AreEqual(GameOutcome.BankerWin, CeeloEngine.ResolveRound(bankerSix, markFour, TieRule.Push));

            var bankerTwo = CeeloEngine.Evaluate(5, 5, 2); // point 2
            var markFive = CeeloEngine.Evaluate(3, 3, 5);  // point 5
            Assert.AreEqual(GameOutcome.BankerLoss, CeeloEngine.ResolveRound(bankerTwo, markFive, TieRule.Push));
        }

        [Test]
        public void SamePoint_RespectsTieRule()
        {
            var banker = CeeloEngine.Evaluate(3, 3, 5); // point 5
            var mark = CeeloEngine.Evaluate(1, 1, 5);   // point 5
            Assert.AreEqual(GameOutcome.BankerWin, CeeloEngine.ResolveRound(banker, mark, TieRule.Banker));
            Assert.AreEqual(GameOutcome.Push, CeeloEngine.ResolveRound(banker, mark, TieRule.Push));
            Assert.AreEqual(GameOutcome.BankerLoss, CeeloEngine.ResolveRound(banker, mark, TieRule.Mark));
        }

        // ---- The Squeeze: exact banker win rates (the spec's headline numbers) ----

        [Test]
        public void BankerWinRate_TiesToBanker_Is5625Percent()
        {
            AssertBankerWinRate(TieRule.Banker, 0.5625, out _);
        }

        [Test]
        public void BankerWinRate_TiesToPush_Is4468Percent()
        {
            AssertBankerWinRate(TieRule.Push, 0.446759, out double pushRate);
            // Push (same point) probability under the push rule ≈ 11.57%.
            Assert.AreEqual(0.115741, pushRate, 1e-4);
        }

        [Test]
        public void BankerWinRate_TiesToMark_HasSameWinRateAsPush()
        {
            AssertBankerWinRate(TieRule.Mark, 0.446759, out double pushRate);
            Assert.AreEqual(0.0, pushRate, 1e-9, "ties go to the mark — no pushes");
        }

        /// <summary>
        /// Exact enumeration: condition both throws on being decisive (re-rolls excluded), weight
        /// each decisive throw by its count in the 216 space, and resolve the full round.
        /// </summary>
        private static void AssertBankerWinRate(TieRule tie, double expectedWin, out double pushRate)
        {
            // Build the decisive distribution: result -> count out of 216.
            var results = new System.Collections.Generic.List<(CeeloResult r, int count)>();
            var counts = new System.Collections.Generic.Dictionary<string, (CeeloResult r, int c)>();
            for (int a = 1; a <= 6; a++)
            for (int b = 1; b <= 6; b++)
            for (int c = 1; c <= 6; c++)
            {
                var r = CeeloEngine.Evaluate(a, b, c);
                if (!r.IsDecisive) continue;
                string key = r.Kind + ":" + r.Value;
                if (counts.TryGetValue(key, out var e)) counts[key] = (r, e.c + 1);
                else counts[key] = (r, 1);
            }
            foreach (var kv in counts.Values) results.Add((kv.r, kv.c));

            long totalDecisive = 0;
            foreach (var x in results) totalDecisive += x.count;

            double win = 0, push = 0, denom = 0;
            foreach (var bankerEntry in results)
            {
                double pBanker = (double)bankerEntry.count / totalDecisive;
                if (bankerEntry.r.IsInstantWin)
                {
                    win += pBanker; denom += pBanker; continue;
                }
                if (bankerEntry.r.Kind == CeeloKind.InstantLoss)
                {
                    denom += pBanker; continue; // a loss
                }
                // Banker point — integrate over the mark's decisive distribution.
                foreach (var markEntry in results)
                {
                    double p = pBanker * ((double)markEntry.count / totalDecisive);
                    var outcome = CeeloEngine.ResolveRound(bankerEntry.r, markEntry.r, tie);
                    if (outcome == GameOutcome.BankerWin) win += p;
                    else if (outcome == GameOutcome.Push) push += p;
                    denom += p;
                }
            }

            Assert.AreEqual(1.0, denom, 1e-9, "probabilities must sum to 1");
            Assert.AreEqual(expectedWin, win, 1e-4, "banker win rate");
            pushRate = push;
        }
    }
}
