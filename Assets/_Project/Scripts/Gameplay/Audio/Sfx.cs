using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Reproductor de efectos de sonido sencillo: un grupo de AudioSources que se reparten los sonidos,
    /// con variación de tono al azar y un límite para que el mismo sonido no se apile en el mismo instante.
    /// Uso: Sfx.Play(clip, volumen);
    /// </summary>
    public static class Sfx
    {
        private const int Voices = 12;
        private const float SameClipGap = 0.035f;

        private static AudioSource[] sources;
        private static int next;
        private static readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sources = null;
            next = 0;
            lastPlayed.Clear();
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (clip == null) return;
            if (lastPlayed.TryGetValue(clip, out float t) && Time.unscaledTime - t < SameClipGap) return;
            lastPlayed[clip] = Time.unscaledTime;

            EnsureSources();
            AudioSource s = sources[next];
            next = (next + 1) % sources.Length;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume);
        }

        private static void EnsureSources()
        {
            if (sources != null && sources[0] != null) return;
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f; // 2D
            }
        }
    }
}
