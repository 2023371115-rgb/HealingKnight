using HealingKnight.Core.Depth;
using UnityEngine;

namespace HealingKnight.Gameplay.Movement
{
    /// <summary>
    /// Cuerpo en el plano 2.5D.
    /// - La posición del GameObject es el punto del piso (los pies).
    /// - La altura del salto se aplica solo al hijo "Visual"; la sombra se queda en el piso.
    /// - No usa física de Unity: el movimiento es predecible y fácil de ajustar.
    /// Estructura esperada:
    ///   Personaje (DepthBody, SortingGroup)
    ///   ├── Shadow (SpriteRenderer)
    ///   └── Visual (aquí van el sprite o las piezas animadas; se voltea con el Facing)
    /// </summary>
    public class DepthBody : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Transform shadow;

        [Tooltip("Gravedad usada cuando no se llama a Jump con valores propios.")]
        [SerializeField] private float defaultGravity = 40f;

        [Tooltip("Mantener dentro del área caminable del nivel/arena.")]
        [SerializeField] private bool clampToWalkableArea = true;

        private Vector2 groundVelocity;
        private float height;
        private float verticalVelocity;
        private float gravity;
        private float fallMultiplier = 1f;
        private int facing = 1;
        private Vector3 shadowBaseScale = Vector3.one;
        private Vector3 visualBaseScale = Vector3.one;

        public Vector2 GroundPosition => transform.position;
        public float Height => height;
        public bool IsGrounded => height <= 0f && verticalVelocity <= 0f;
        public int Facing => facing;
        public Transform Visual => visual;
        public Vector2 GroundVelocity => groundVelocity;

        private void Awake()
        {
            gravity = defaultGravity;
            if (shadow != null) shadowBaseScale = shadow.localScale;
            if (visual != null) visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
        }

        /// <summary>Velocidad en el piso (X horizontal, Y profundidad).</summary>
        public void SetGroundVelocity(Vector2 velocity)
        {
            groundVelocity = velocity;
        }

        public void SetFacing(int direction)
        {
            if (direction == 0) return;
            facing = direction > 0 ? 1 : -1;
            if (visual != null)
                visual.localScale = new Vector3(visualBaseScale.x * facing, visualBaseScale.y, visualBaseScale.z);
        }

        /// <summary>Salta con velocidad inicial y gravedad dadas (ver JumpMath).</summary>
        public void Jump(float initialVelocity, float jumpGravity, float fallGravityMultiplier = 1f)
        {
            verticalVelocity = initialVelocity;
            gravity = jumpGravity;
            fallMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        }

        /// <summary>Empuje vertical pequeño (por ejemplo al recibir un golpe).</summary>
        public void Pop(float initialVelocity)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, initialVelocity);
            gravity = defaultGravity;
            fallMultiplier = 1f;
        }

        public void Teleport(Vector2 groundPosition)
        {
            transform.position = new Vector3(groundPosition.x, groundPosition.y, transform.position.z);
            groundVelocity = Vector2.zero;
            height = 0f;
            verticalVelocity = 0f;
            ApplyVisualHeight();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Piso
            Vector2 next = (Vector2)transform.position + groundVelocity * dt;
            if (clampToWalkableArea) next = WalkableArea.Clamp(next);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            // Altura
            if (height > 0f || verticalVelocity > 0f)
            {
                float g = verticalVelocity < 0f ? gravity * fallMultiplier : gravity;
                verticalVelocity -= g * dt;
                height += verticalVelocity * dt;

                if (height <= 0f)
                {
                    height = 0f;
                    verticalVelocity = 0f;
                }
            }

            ApplyVisualHeight();
        }

        private void ApplyVisualHeight()
        {
            if (visual != null)
            {
                Vector3 p = visual.localPosition;
                visual.localPosition = new Vector3(p.x, height, p.z);
            }

            if (shadow != null)
            {
                // La sombra se encoge un poco mientras más alto estás.
                float k = Mathf.Clamp(1f - height * 0.15f, 0.55f, 1f);
                shadow.localScale = shadowBaseScale * k;
            }
        }

        /// <summary>Distancia en el piso a otro cuerpo, ignorando la altura.</summary>
        public float GroundDistanceTo(DepthBody other)
        {
            return other == null ? float.MaxValue : Vector2.Distance(GroundPosition, other.GroundPosition);
        }

        public bool SameLaneAs(DepthBody other, float laneTolerance)
        {
            return other != null && DepthMath.SameLane(GroundPosition.y, other.GroundPosition.y, laneTolerance);
        }
    }
}
