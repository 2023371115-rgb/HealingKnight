using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Alvéolo que respira (Pulmones): se infla, se mantiene lleno, tiembla como aviso y se desinfla.
    /// Lleno es una plataforma normal; desinflado no se puede pisar (si estás encima, caes).
    /// Con 'phaseOffset' distinto entre alvéolos vecinos se alternan y hay que medir el salto.
    /// Estructura: raíz con BoxCollider2D (+ PlatformEffector2D) y este script; hijo "Visual" con las burbujas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreathingPlatform : MonoBehaviour
    {
        [SerializeField] private Transform visual;

        [Header("Ritmo (segundos)")]
        [SerializeField] private float inflateTime = 0.6f;
        [SerializeField] private float fullTime = 2.2f;
        [SerializeField] private float warningTime = 0.7f;
        [SerializeField] private float deflateTime = 0.35f;
        [SerializeField] private float emptyTime = 1.4f;
        [Tooltip("0..1: en qué parte del ciclo empieza (para alternar con los vecinos).")]
        [SerializeField, Range(0f, 1f)] private float phaseOffset;

        [Header("Forma")]
        [SerializeField, Range(0.05f, 1f)] private float emptyScale = 0.25f;
        [Tooltip("Cuánto sube al inflarse (se siente como respiración).")]
        [SerializeField] private float rise = 0.25f;
        [Tooltip("Por debajo de este tamaño ya no se puede pisar.")]
        [SerializeField, Range(0.1f, 1f)] private float solidAbove = 0.7f;

        private BoxCollider2D box;
        private Vector2 fullSize;
        private Vector3 visualBaseScale = Vector3.one;
        private Vector3 basePosition;
        private float time;

        public bool IsSolid => box != null && box.enabled;
        private float Period => inflateTime + fullTime + warningTime + deflateTime + emptyTime;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            fullSize = box.size;
            basePosition = transform.position;
            if (visual != null) visualBaseScale = visual.localScale;
            time = phaseOffset * Period;
        }

        private void Update()
        {
            time = (time + Time.deltaTime) % Period;

            float t = time;
            float scale;
            float shake = 0f;
            if (t < inflateTime) scale = Mathf.Lerp(emptyScale, 1f, Smooth(t / inflateTime));
            else if ((t -= inflateTime) < fullTime) scale = 1f + Mathf.Sin(t * 3f) * 0.02f;
            else if ((t -= fullTime) < warningTime)
            {
                scale = 1f;
                shake = Mathf.Sin(t * 45f) * 0.06f; // tiembla: "me voy a desinflar"
            }
            else if ((t -= warningTime) < deflateTime) scale = Mathf.Lerp(1f, emptyScale, Smooth(t / deflateTime));
            else scale = emptyScale;

            if (visual != null)
            {
                visual.localScale = new Vector3(visualBaseScale.x * scale, visualBaseScale.y * Mathf.Lerp(0.6f, 1f, scale), visualBaseScale.z);
                visual.localPosition = new Vector3(shake, 0f, 0f);
            }

            float k = Mathf.InverseLerp(emptyScale, 1f, scale);
            transform.position = basePosition + new Vector3(0f, rise * k, 0f);

            bool solid = scale >= solidAbove;
            if (box.enabled != solid) box.enabled = solid;
            box.size = new Vector2(fullSize.x * Mathf.Max(scale, 0.1f), fullSize.y);
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);
    }
}
