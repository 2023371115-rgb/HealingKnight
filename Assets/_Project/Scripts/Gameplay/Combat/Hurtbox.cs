using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Depth;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Combat
{
    /// <summary>
    /// Zona donde se puede golpear a un personaje. Se registra en una lista global para que
    /// cápsulas y golpes encuentren objetivos sin usar la física de Unity (que no entiende la profundidad).
    /// Debe estar en el mismo GameObject que el componente que implementa IDamageable.
    /// </summary>
    [RequireComponent(typeof(DepthBody))]
    public class Hurtbox : MonoBehaviour
    {
        private static readonly List<Hurtbox> all = new List<Hurtbox>();
        public static IReadOnlyList<Hurtbox> All => all;

        [Tooltip("Medio ancho del cuerpo en unidades.")]
        [SerializeField] private float halfWidth = 0.4f;

        [Tooltip("Alto del cuerpo desde los pies.")]
        [SerializeField] private float bodyHeight = 1.8f;

        private DepthBody body;
        private IDamageable damageable;

        public DepthBody Body => body;
        public IDamageable Damageable => damageable;
        public float HalfWidth => halfWidth;
        public float BodyHeight => bodyHeight;
        public Team Team => damageable != null ? damageable.Team : Team.Neutral;
        public bool IsAlive => damageable != null && damageable.IsAlive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            body = GetComponent<DepthBody>();
            damageable = GetComponent<IDamageable>();
            if (damageable == null)
                Debug.LogError($"[Hurtbox] {name} no tiene un componente IDamageable.", this);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public bool IsHitBy(Vector2 attackCenter, float attackHalfWidth, float attackHeight, float laneTolerance)
        {
            return DepthMath.Overlaps(
                attackCenter, attackHalfWidth, attackHeight,
                body.GroundPosition, halfWidth, body.Height, bodyHeight,
                laneTolerance);
        }

        /// <summary>Busca objetivos vivos de un equipo que toquen la zona de ataque.</summary>
        public static void Query(Team targetTeam, Vector2 attackCenter, float attackHalfWidth, float attackHeight,
            float laneTolerance, List<Hurtbox> results)
        {
            results.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                Hurtbox h = all[i];
                if (h == null || h.Team != targetTeam || !h.IsAlive) continue;
                if (h.IsHitBy(attackCenter, attackHalfWidth, attackHeight, laneTolerance)) results.Add(h);
            }
        }

        /// <summary>El primer objetivo tocado (el más cercano a 'fromX').</summary>
        public static Hurtbox QueryFirst(Team targetTeam, Vector2 attackCenter, float attackHalfWidth, float attackHeight,
            float laneTolerance, float fromX)
        {
            Hurtbox best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                Hurtbox h = all[i];
                if (h == null || h.Team != targetTeam || !h.IsAlive) continue;
                if (!h.IsHitBy(attackCenter, attackHalfWidth, attackHeight, laneTolerance)) continue;

                float d = Mathf.Abs(h.body.GroundPosition.x - fromX);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = h;
                }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 p = transform.position;
            float h = Application.isPlaying && body != null ? body.Height : 0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(p + new Vector3(0f, h + bodyHeight * 0.5f, 0f), new Vector3(halfWidth * 2f, bodyHeight, 0f));
        }
    }
}
