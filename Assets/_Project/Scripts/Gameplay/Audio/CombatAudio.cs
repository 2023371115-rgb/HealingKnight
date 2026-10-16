using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Sonidos generales del combate (los de cada medicamento van en su MedicationData).
    /// Vive en Data/Resources/CombatAudio para cargarse solo.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Sonidos de combate", fileName = "CombatAudio")]
    public class CombatAudio : ScriptableObject
    {
        public const string ResourcePath = "CombatAudio";

        [Tooltip("Cápsula ineficaz que rebota en gris.")]
        public AudioClip resistSound;

        [Header("Volumen")]
        [Range(0f, 1f)] public float fireVolume = 0.45f;
        [Range(0f, 1f)] public float hitVolume = 0.6f;
        [Range(0f, 1f)] public float resistVolume = 0.55f;
        [Tooltip("Variación de tono al azar para que no suene repetitivo (0.06 = ±6 %).")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.06f;

        private static CombatAudio cached;
        private static bool loaded;

        public static CombatAudio Instance
        {
            get
            {
                if (!loaded)
                {
                    cached = Resources.Load<CombatAudio>(ResourcePath);
                    loaded = true;
                }
                return cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
            loaded = false;
        }
    }
}
