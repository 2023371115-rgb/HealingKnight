using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Neurona sana (GDD 5.14): no es hostil, pero está en la línea de fuego.
    /// Si una cápsula del jugador la toca, resta 5 a la salud del cerebro; un proyectil enemigo, 3.
    /// Detiene el disparo (riesgo-recompensa: apunta con cuidado). Las bombas no la dañan.
    /// Necesita un Hurtbox en el mismo objeto.
    /// </summary>
    public class HealthyNeuron : MonoBehaviour, IDamageable
    {
        [SerializeField] private float playerShotDamage = 5f;
        [SerializeField] private float enemyShotDamage = 3f;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private Color hurtColor = new Color(1f, 0.25f, 0.3f);
        [Tooltip("Cada golpe la apaga un poco (se ve que sufrió daño).")]
        [SerializeField] private float dimPerHit = 0.12f;

        private Color[] baseColors;
        private float flashTimer;
        private float dim;

        public Team Team => Team.Neutral;
        public bool IsAlive => true;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }

        public HitResult ApplyDamage(DamageInfo info)
        {
            if (info.kind != DamageKind.Projectile) return HitResult.Ignored;

            float amount = info.sourceTeam == Team.Player ? playerShotDamage : enemyShotDamage;
            if (BrainHealth.Current != null) BrainHealth.Current.Damage(amount);
            if (info.sourceTeam == Team.Player)
                GameplayEvents.RaiseMessage("¡Neurona sana dañada! (−5 salud del cerebro)");

            dim = Mathf.Min(0.6f, dim + dimPerHit);
            flashTimer = 0.2f;
            return new HitResult { outcome = HitOutcome.Damaged, effectiveness = Core.Combat.Effectiveness.Ineffective, damageDealt = 0f };
        }

        private void Update()
        {
            // Late suave (neurona viva) y destello rojo al recibir daño.
            float pulse = 1f + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.06f;
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color baseC = Color.Lerp(baseColors[i], new Color(0.25f, 0.25f, 0.35f, baseColors[i].a), dim);
                Color c = flashTimer > 0f ? hurtColor : baseC * pulse;
                c.a = baseColors[i].a;
                renderers[i].color = c;
            }
        }
    }
}
