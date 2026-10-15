using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Alvéolo que respira (Pulmones). Ciclo de 4 s = 15 respiraciones por minuto (normal en reposo):
    ///   se infla en 0.5 s → inflado 1.5 s → se desinfla en 0.5 s → desinflado 1.5 s.
    /// Los cambios de 0.5 s son el aviso para el jugador. Alvéolos del mismo grupo se desfasan hasta 2 s.
    ///
    /// - Inflado: si el jugador CAE encima, sale impulsado siempre a 4.0 u (velocidad vertical fija de 15.8 u/s),
    ///   sin importar desde dónde cayó ni si presiona saltar. El rebote no cuenta como salto: no lo gasta,
    ///   no lo recarga y en el aire sigue sin poder saltar.
    /// - Desinflado: plataforma baja normal, sin rebote; desde ahí se puede saltar.
    /// - Si el jugador solo está parado encima cuando se infla, no lo lanza (sube con él).
    ///
    /// Reglas para diseñar el nivel: dejar 5.5 u libres hacia el techo encima de cada alvéolo,
    /// la repisa de destino a máximo 3.5 u arriba del alvéolo y huecos de máximo 4.5 u.
    /// Estructura: raíz con BoxCollider2D (+ PlatformEffector2D) y este script; hijo "Visual" con las burbujas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreathingPlatform : MonoBehaviour
    {
        public const float CycleSeconds = 4f;
        public const float BounceHeight = 4f;
        public const float BounceVelocity = 15.8f;

        [SerializeField] private Transform visual;

        [Header("Ritmo (segundos, ciclo total 4 s)")]
        [SerializeField] private float inflateTime = 0.5f;
        [SerializeField] private float inflatedTime = 1.5f;
        [SerializeField] private float deflateTime = 0.5f;
        [SerializeField] private float deflatedTime = 1.5f;
        [Tooltip("Desfase dentro del grupo (0 a 2 s) para armar secuencias de timing.")]
        [SerializeField, Range(0f, 2f)] private float phaseOffset;

        [Header("Forma")]
        [Tooltip("Cuánto sube la superficie al inflarse.")]
        [SerializeField] private float inflatedRise = 0.6f;
        [SerializeField, Range(0.2f, 1f)] private float deflatedScale = 0.55f;

        private BoxCollider2D box;
        private Vector3 basePosition;
        private Vector3 visualBaseScale = Vector3.one;
        private float time;

        private bool playerWasOnTop;
        private float playerPreviousVelocityY;

        public bool IsInflated { get; private set; }
        private float Period => inflateTime + inflatedTime + deflateTime + deflatedTime;
        private float TopY => transform.position.y; // la superficie está en el origen del objeto

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            basePosition = transform.position;
            if (visual != null) visualBaseScale = visual.localScale;
            time = phaseOffset;
        }

        private void Update()
        {
            time = (time + Time.deltaTime) % Period;
            float k = InflationAt(time, out bool inflated);
            IsInflated = inflated;

            transform.position = basePosition + new Vector3(0f, inflatedRise * k, 0f);
            if (visual != null)
            {
                float s = Mathf.Lerp(deflatedScale, 1f, k);
                visual.localScale = new Vector3(visualBaseScale.x * Mathf.Lerp(0.85f, 1f, k), visualBaseScale.y * s, visualBaseScale.z);
            }

            CheckBounce();
        }

        /// <summary>0 = desinflado, 1 = inflado. 'inflated' solo es true en la fase de 1.5 s completamente inflado.</summary>
        private float InflationAt(float t, out bool inflated)
        {
            inflated = false;
            if (t < inflateTime) return Smooth(t / inflateTime);
            t -= inflateTime;
            if (t < inflatedTime) { inflated = true; return 1f; }
            t -= inflatedTime;
            if (t < deflateTime) return 1f - Smooth(t / deflateTime);
            return 0f;
        }

        private void CheckBounce()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null) return;

            Vector2 feet = player.Position;
            float halfWidth = box.size.x * 0.5f + 0.3f;
            bool onTop = Mathf.Abs(feet.x - transform.position.x) <= halfWidth
                         && feet.y >= TopY - 0.25f && feet.y <= TopY + 0.15f
                         && player.Motor.Velocity.y <= 0.1f;

            // Solo rebota si CAYÓ encima (venía bajando) y el alvéolo está inflado.
            // Si ya estaba parado encima cuando se infló, no lo lanza.
            if (onTop && !playerWasOnTop && IsInflated && playerPreviousVelocityY < -1f)
            {
                player.Bounce(BounceHeight, BounceVelocity);
                onTop = false;
            }

            playerWasOnTop = onTop;
            playerPreviousVelocityY = player.Motor.Velocity.y;
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private void OnDrawGizmosSelected()
        {
            // Espacio libre que debe tener hacia el techo y alcance de la repisa de destino.
            Vector3 top = Application.isPlaying ? basePosition : transform.position;
            Gizmos.color = new Color(1f, 0.5f, 0.8f, 0.6f);
            Gizmos.DrawLine(top, top + Vector3.up * 5.5f);
            Gizmos.color = new Color(0.5f, 1f, 0.6f, 0.6f);
            Gizmos.DrawLine(top + new Vector3(-1f, 3.5f, 0f), top + new Vector3(1f, 3.5f, 0f));
        }
    }
}
