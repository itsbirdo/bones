using System;
using System.Collections;
using UnityEngine;
using Bones.Core;
using Bones.Data;

namespace Bones.Presentation
{
    /// <summary>
    /// The heartbeat (spec §5, manifest §1.4). Two depth-separated sets of dice:
    /// • YOURS — front/bottom of the dice space, larger; roll IN from the right (right→left).
    /// • THE MARK'S — above and smaller (depth); roll IN from the left (left→right) on the counter-roll.
    /// Dice resolve ONE AT A TIME with the final die lingering. Outcome-first: the faces are decided
    /// by the logic; this just choreographs to them.
    /// </summary>
    public class DiceRollChoreographer : MonoBehaviour
    {
        [Header("Anchors (set by SceneBuilder)")]
        [SerializeField] private Transform bankerAnchor;  // front, larger
        [SerializeField] private Transform markAnchor;    // back, smaller
        [SerializeField] private GameDatabase database;

        [Header("Timing")]
        [SerializeField] private float perDieTravel = 0.5f;
        [SerializeField] private float gapBetweenDice = 0.14f;
        [SerializeField] private float finalDieLinger = 0.8f;

        [Header("Entry")]
        [SerializeField] private float entryOffset = 7f;  // how far off-screen dice start
        [SerializeField] private float dropHeight = 2f;   // how high they drop in from

        private static readonly float[] SlotX = { -0.95f, 0f, 0.95f };

        private DieView[] _banker;
        private DieView[] _mark;

        public event Action<int, int> DieLanded;   // (slotIndex, face) — per CLACK
        public event Action<string> CheatFired;     // die id whose tell should play

        // ---- Public reveal API ----

        /// <summary>Your throw: roll in from the right (right→left). Clears the mark's dice first.</summary>
        public IEnumerator RevealBanker(ResolvedThrow throwResult, string[] cupIds, Action onComplete)
        {
            EnsureBanker(cupIds);
            SetActive(_mark, false);   // opponent's dice from the previous round clear off
            SetActive(_banker, false); // start with NO dice on screen; each enters one at a time
            yield return RollInSet(_banker, throwResult, fromRight: true, lingerFinal: true);

            if (throwResult.FiredProcs != null)
                foreach (var id in throwResult.FiredProcs) CheatFired?.Invoke(id);

            yield return new WaitForSeconds(finalDieLinger);
            onComplete?.Invoke();
        }

        /// <summary>The mark's counter-roll: roll in from the left (left→right), above and smaller.</summary>
        public IEnumerator RevealMark(ResolvedThrow throwResult, Action onComplete)
        {
            EnsureMark();
            SetActive(_mark, false); // none visible until each rolls in
            yield return RollInSet(_mark, throwResult, fromRight: false, lingerFinal: true);
            yield return new WaitForSeconds(finalDieLinger * 0.6f);
            onComplete?.Invoke();
        }

        /// <summary>Drop the cached dice so they rebuild (new meshes/tints) after a loadout change.</summary>
        public void ResetDice()
        {
            DestroySet(ref _banker);
            DestroySet(ref _mark);
        }

        // ---- Internals ----

        private IEnumerator RollInSet(DieView[] dice, ResolvedThrow t, bool fromRight, bool lingerFinal)
        {
            int[] faces = { t.Face0, t.Face1, t.Face2 };
            float dir = fromRight ? 1f : -1f;
            for (int i = 0; i < 3; i++)
            {
                bool isFinal = i == 2;
                Vector3 end = new Vector3(SlotX[i], 0f, 0f);
                Vector3 start = new Vector3(SlotX[i] + dir * entryOffset, dropHeight, 0f);
                dice[i].gameObject.SetActive(true); // appears only now, off-screen, then rolls into frame
                yield return dice[i].RollInto(start, end, faces[i], perDieTravel, lingerFinal && isFinal);
                DieLanded?.Invoke(i, faces[i]);
                if (!isFinal) yield return new WaitForSeconds(gapBetweenDice);
            }
        }

        private void EnsureBanker(string[] cupIds)
        {
            if (_banker != null) return;
            var anchor = bankerAnchor != null ? bankerAnchor : transform;
            _banker = new DieView[3];
            for (int i = 0; i < 3; i++)
            {
                var def = database != null ? database.FindDie(cupIds != null ? cupIds[i] : null) : null;
                _banker[i] = DieFactory.Create(anchor, def, new Vector3(SlotX[i], 0f, 0f));
                _banker[i].gameObject.SetActive(false); // hidden until it rolls in
            }
        }

        private void EnsureMark()
        {
            if (_mark != null) return;
            var anchor = markAnchor != null ? markAnchor : transform;
            var def = database != null ? database.boneDie : null; // opponent rolls plain dice
            _mark = new DieView[3];
            for (int i = 0; i < 3; i++)
            {
                _mark[i] = DieFactory.Create(anchor, def, new Vector3(SlotX[i], 0f, 0f));
                _mark[i].gameObject.SetActive(false); // hidden until it rolls in
            }
        }

        private static void SetActive(DieView[] set, bool on)
        {
            if (set == null) return;
            foreach (var d in set) if (d != null) d.gameObject.SetActive(on);
        }

        private static void DestroySet(ref DieView[] set)
        {
            if (set == null) return;
            foreach (var d in set) if (d != null) Destroy(d.gameObject);
            set = null;
        }
    }
}
