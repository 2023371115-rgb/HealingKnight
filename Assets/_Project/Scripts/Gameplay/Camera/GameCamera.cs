using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.CameraSystem
{
    /// <summary>
    /// Cámara de beat 'em up: sigue al jugador en X, mantiene una altura fija y respeta
    /// los límites del nivel y de la arena activa (cuando hay arena, no se puede avanzar).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameCamera : MonoBehaviour
    {
        [SerializeField] private float followSmoothing = 6f;
        [Tooltip("Cuánto se adelanta la cámara hacia donde mira el jugador.")]
        [SerializeField] private float lookAhead = 1.5f;
        [Tooltip("Altura fija de la cámara (centro de la pantalla).")]
        [SerializeField] private float fixedY = 1.5f;

        private Camera cam;
        private float shakeTimer;
        private float shakeStrength;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        private void LateUpdate()
        {
            PlayerController player = PlayerController.Current;
            float targetX = transform.position.x;
            if (player != null) targetX = player.Body.GroundPosition.x + player.Body.Facing * lookAhead;

            float halfWidth = cam.orthographicSize * cam.aspect;
            float min = WalkableArea.MinX + halfWidth;
            float max = WalkableArea.MaxX - halfWidth;
            targetX = min <= max ? Mathf.Clamp(targetX, min, max) : (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f;

            float x = Mathf.Lerp(transform.position.x, targetX, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
            Vector3 pos = new Vector3(x, fixedY, transform.position.z);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                pos += (Vector3)(Random.insideUnitCircle * shakeStrength);
            }

            transform.position = pos;
        }

        public void Shake(float strength = 0.15f, float duration = 0.15f)
        {
            shakeStrength = strength;
            shakeTimer = duration;
        }

        /// <summary>Coloca la cámara de golpe (al cargar o reaparecer).</summary>
        public void SnapTo(float x)
        {
            transform.position = new Vector3(x, fixedY, transform.position.z);
        }
    }
}
