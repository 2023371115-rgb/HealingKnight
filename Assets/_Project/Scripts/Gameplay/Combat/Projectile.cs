using System;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Combat
{
    /// <summary>Parámetros de un disparo.</summary>
    public struct ProjectileShot
    {
        public Vector2 origin;          // punto del piso de donde sale
        public float height;            // altura a la que viaja
        public int direction;           // -1 izquierda, 1 derecha
        public float speed;
        public float range;
        public float damage;
        public float knockback;
        public float laneTolerance;
        public float halfWidth;
        public Team sourceTeam;
        public Team targetTeam;
        public MedicationData medication; // null en proyectiles enemigos
        public Color color;
    }

    /// <summary>
    /// Cápsula de medicamento o proyectil enemigo. Viaja en línea recta por su franja de profundidad.
    /// Si el medicamento es ineficaz, rebota en gris (GDD 5.7). Lo administra un pool.
    /// Estructura: Projectile (raíz en el piso) └── Visual (SpriteRenderer a la altura del disparo).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Rebote (medicamento ineficaz)")]
        [SerializeField] private float bounceDuration = 0.3f;
        [SerializeField] private float bounceSpeedFactor = 0.4f;
        [SerializeField] private Color bounceColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private ProjectileShot shot;
        private float travelled;
        private bool bouncing;
        private float bounceTimer;
        private Action<Projectile> onFinished;
        private bool finished;

        public static event Action<Projectile, HitResult> AnyHit;

        public void Launch(ProjectileShot data, Action<Projectile> finishedCallback)
        {
            shot = data;
            onFinished = finishedCallback;
            travelled = 0f;
            bouncing = false;
            bounceTimer = 0f;
            finished = false;

            transform.position = new Vector3(data.origin.x, data.origin.y, 0f);
            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, data.height, 0f);
                Vector3 s = visual.localScale;
                visual.localScale = new Vector3(Mathf.Abs(s.x) * (data.direction >= 0 ? 1f : -1f), s.y, s.z);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = data.color;
                if (data.medication != null && data.medication.capsuleSprite != null)
                    spriteRenderer.sprite = data.medication.capsuleSprite;
            }
        }

        private void Update()
        {
            if (finished) return;
            float dt = Time.deltaTime;

            if (bouncing)
            {
                bounceTimer += dt;
                transform.position += new Vector3(-shot.direction * shot.speed * bounceSpeedFactor * dt, 0f, 0f);
                if (visual != null) visual.localPosition += new Vector3(0f, 2f * dt, 0f);
                if (spriteRenderer != null)
                {
                    Color c = bounceColor;
                    c.a = Mathf.Clamp01(1f - bounceTimer / bounceDuration);
                    spriteRenderer.color = c;
                }
                if (bounceTimer >= bounceDuration) Finish();
                return;
            }

            float step = shot.speed * dt;
            transform.position += new Vector3(shot.direction * step, 0f, 0f);
            travelled += step;

            Vector2 ground = transform.position;
            Hurtbox target = Hurtbox.QueryFirst(shot.targetTeam, ground, shot.halfWidth, shot.height, shot.laneTolerance,
                ground.x - shot.direction * shot.halfWidth);

            if (target != null)
            {
                DamageInfo info = DamageInfo.Create(shot.damage, DamageKind.Projectile, shot.sourceTeam,
                    ground - new Vector2(shot.direction * 0.5f, 0f));
                info.knockback = shot.knockback;
                info.medication = shot.medication;

                HitResult result = target.Damageable.ApplyDamage(info);
                AnyHit?.Invoke(this, result);

                if (result.outcome == HitOutcome.Resisted) StartBounce();
                else if (result.outcome != HitOutcome.Ignored) Finish();
                return;
            }

            bool outOfLevel = ground.x < WalkableArea.LevelBounds.xMin - 2f || ground.x > WalkableArea.LevelBounds.xMax + 2f;
            if (travelled >= shot.range || outOfLevel) Finish();
        }

        private void StartBounce()
        {
            bouncing = true;
            bounceTimer = 0f;
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            if (onFinished != null) onFinished(this);
            else gameObject.SetActive(false);
        }
    }
}
