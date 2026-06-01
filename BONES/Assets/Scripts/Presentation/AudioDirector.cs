using UnityEngine;
using Bones.Data;

namespace Bones.Presentation
{
    /// <summary>
    /// Adaptive audio hooks (manifest §4). Wire AudioClips in the inspector; missing clips are
    /// safely ignored so the project runs silent until assets arrive. Music stems can layer with Heat.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        [Header("Diegetic")]
        [SerializeField] private AudioClip clack;
        [SerializeField] private AudioClip coinClink;

        [Header("Stingers")]
        [SerializeField] private AudioClip winStinger;
        [SerializeField] private AudioClip jackpotStinger;
        [SerializeField] private AudioClip bustStinger;

        [Header("Music")]
        [SerializeField] private AudioSource musicBed;

        private AudioSource _sfx;

        private void Awake()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
        }

        public void Clack() => Play(clack, Random.Range(0.92f, 1.08f));
        public void Coins() => Play(coinClink);

        public void Stinger(JuiceTier tier)
        {
            switch (tier)
            {
                case JuiceTier.Jackpot: Play(jackpotStinger); break;
                case JuiceTier.Hot:
                case JuiceTier.PointWin: Play(winStinger); break;
                case JuiceTier.Bust: Play(bustStinger); break;
            }
        }

        /// <summary>Swell the music with Heat (placeholder: volume; replace with stem layering).</summary>
        public void SetHeat(double heat)
        {
            if (musicBed != null) musicBed.volume = Mathf.Clamp01(0.5f + 0.15f * (float)(heat - 1.0));
        }

        private void Play(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || _sfx == null) return;
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip);
        }
    }
}
