using System.Collections;
using UnityEngine;
using Bones.Data;

namespace Bones.Juice
{
    /// <summary>
    /// The dopamine machine (manifest §3): a tiered, stackable feedback system whose intensity
    /// scales with stake × Heat × proc. Drives screen shake, flash, slow-mo, and (hooks for)
    /// particles + audio. Tiers must feel categorically different, not just "more".
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        [SerializeField] private JuiceTierConfig config;
        [SerializeField] private Camera shakeCamera;
        [SerializeField] private CanvasGroup flashOverlay; // optional full-screen flash (uGUI) — or drive a UIToolkit element

        private Vector3 _camHome;

        private void Awake()
        {
            if (shakeCamera == null) shakeCamera = Camera.main;
            if (shakeCamera != null) _camHome = shakeCamera.transform.localPosition;
        }

        /// <summary>Pick the tier from the settled result + Heat (manifest §3.1).</summary>
        public JuiceTier TierFor(Bones.Core.CeeloResult result, double heat, bool busted)
        {
            if (busted) return JuiceTier.Bust;
            if (result.Kind == Bones.Core.CeeloKind.Triple) return JuiceTier.Jackpot;
            if (result.Kind == Bones.Core.CeeloKind.FourFiveSix) return heat >= 2.0 ? JuiceTier.Jackpot : JuiceTier.Hot;
            if (heat >= 2.0) return JuiceTier.Hot;
            return JuiceTier.PointWin;
        }

        public void PlayClack()
        {
            var p = config != null ? config.For(JuiceTier.Clack) : default;
            StartCoroutine(Shake(p.screenShake, 0.08f));
        }

        public void Play(JuiceTier tier)
        {
            var p = config != null ? config.For(tier) : default;
            StartCoroutine(Shake(p.screenShake, 0.25f));
            if (p.flashColor.a > 0f) StartCoroutine(Flash(p.flashColor));
            if (p.timeScaleDip > 0f) StartCoroutine(SlowMo(p.timeScaleDip));
            // Hooks (wire to your VFX/audio): p.particleBurst, p.musicSwell, p.panelFracture.
        }

        private IEnumerator Shake(float magnitude, float duration)
        {
            if (shakeCamera == null || magnitude <= 0f) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                Vector2 off = Random.insideUnitCircle * magnitude * 0.5f;
                shakeCamera.transform.localPosition = _camHome + new Vector3(off.x, off.y, 0f);
                yield return null;
            }
            shakeCamera.transform.localPosition = _camHome;
        }

        private IEnumerator Flash(Color color)
        {
            if (flashOverlay == null) yield break;
            flashOverlay.alpha = color.a;
            float t = 0f, dur = 0.35f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                flashOverlay.alpha = Mathf.Lerp(color.a, 0f, t / dur);
                yield return null;
            }
            flashOverlay.alpha = 0f;
        }

        private IEnumerator SlowMo(float strength)
        {
            float target = Mathf.Clamp(1f - strength * 0.4f, 0.1f, 1f);
            Time.timeScale = target;
            float t = 0f, dur = 0.35f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(target, 1f, t / dur);
                yield return null;
            }
            Time.timeScale = 1f;
        }
    }
}
