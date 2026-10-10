using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// IA de enemigo de beat 'em up:
    /// 1. Se acerca al jugador y se alinea con su franja de profundidad.
    /// 2. Pide una "ficha de ataque" (máx. 2 atacando a la vez). Sin ficha, espera a cierta distancia.
    /// 3. Avisa (se ilumina), ataca (embestida o disparo) y se recupera.
    /// 4. Al recibir daño se aturde un momento y sale empujado.
    /// </summary>
    [RequireComponent(typeof(DepthBody), typeof(EnemyHealth))]
    public class EnemyBrain : MonoBehaviour
    {
        private enum State { Idle, Approach, Wait, Windup, Attack, Recover, Hurt, Dead }

        [SerializeField] private EnemyData data;

        [Tooltip("Solo para enemigos a distancia.")]
        [SerializeField] private Projectile projectilePrefab;

        [Tooltip("Distancia a la que esperan su turno los que no tienen ficha de ataque.")]
        [SerializeField] private float waitDistance = 3.2f;

        [Tooltip("Separación mínima entre enemigos para que no se encimen.")]
        [SerializeField] private float separationRadius = 0.7f;

        private static readonly List<EnemyBrain> active = new List<EnemyBrain>();

        private DepthBody body;
        private EnemyHealth health;
        private ComponentPool<Projectile> projectilePool;
        private State state = State.Idle;
        private float timer;
        private float attackCooldown;
        private float knockbackVelocity;
        private float depthOffset;
        private int attackDirection;
        private bool attackLanded;

        public EnemyData Data => data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        private void Awake()
        {
            body = GetComponent<DepthBody>();
            health = GetComponent<EnemyHealth>();
            depthOffset = Random.Range(-0.6f, 0.6f);
            attackCooldown = Random.Range(0.3f, 1.2f);
        }

        private void OnEnable()
        {
            active.Add(this);
            health.Hit += OnHit;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            active.Remove(this);
            health.Hit -= OnHit;
            health.Died -= OnDied;
            AttackTokens.Release(this);
            health.SetTelegraph(false);
        }

        private void OnDestroy()
        {
            projectilePool?.Clear();
        }

        public void Configure(EnemyData enemyData)
        {
            data = enemyData;
        }

        private void Start()
        {
            if (data != null && data.attackType == EnemyAttackType.Ranged && projectilePrefab != null)
                projectilePool = new ComponentPool<Projectile>(projectilePrefab, 2, 6);
        }

        private void Update()
        {
            if (data == null || state == State.Dead) return;

            float dt = Time.deltaTime;
            if (attackCooldown > 0f) attackCooldown -= dt;

            PlayerController player = PlayerController.Current;
            bool playerAvailable = player != null && player.Health != null && player.Health.IsAlive;

            switch (state)
            {
                case State.Idle:
                    body.SetGroundVelocity(Vector2.zero);
                    if (playerAvailable && Vector2.Distance(body.GroundPosition, player.Body.GroundPosition) <= data.aggroRange)
                        state = State.Approach;
                    break;

                case State.Approach:
                case State.Wait:
                    if (!playerAvailable) { GoIdle(); break; }
                    UpdateApproach(player, dt);
                    break;

                case State.Windup:
                    body.SetGroundVelocity(Vector2.zero);
                    timer -= dt;
                    if (timer <= 0f) BeginAttack(player);
                    break;

                case State.Attack:
                    UpdateAttack(player, dt);
                    break;

                case State.Recover:
                    body.SetGroundVelocity(Vector2.zero);
                    timer -= dt;
                    if (timer <= 0f) state = State.Approach;
                    break;

                case State.Hurt:
                    timer -= dt;
                    knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, 20f * dt);
                    body.SetGroundVelocity(new Vector2(knockbackVelocity, 0f));
                    if (timer <= 0f) state = State.Approach;
                    break;
            }
        }

        private void UpdateApproach(PlayerController player, float dt)
        {
            Vector2 me = body.GroundPosition;
            Vector2 target = player.Body.GroundPosition;
            int side = me.x >= target.x ? 1 : -1;   // de qué lado del jugador estoy

            bool hasToken = AttackTokens.TryAcquire(this);
            state = hasToken ? State.Approach : State.Wait;

            float desiredDistance = data.attackType == EnemyAttackType.Ranged
                ? data.attackDistance
                : (hasToken ? data.attackDistance * 0.85f : waitDistance);

            if (!hasToken && data.attackType == EnemyAttackType.Ranged) desiredDistance = Mathf.Max(desiredDistance, waitDistance);

            // Con ficha se alinea exacto con el jugador; sin ficha se queda un poco desfasado.
            float desiredDepth = target.y + (hasToken ? 0f : depthOffset);
            Vector2 desired = new Vector2(target.x + side * desiredDistance, desiredDepth);

            Vector2 toDesired = desired - me;
            Vector2 move = Vector2.zero;
            if (Mathf.Abs(toDesired.x) > 0.15f) move.x = Mathf.Sign(toDesired.x);
            if (Mathf.Abs(toDesired.y) > 0.08f) move.y = Mathf.Sign(toDesired.y) * data.depthSpeedFactor;

            move += Separation() * 0.8f;
            Vector2 velocity = move.sqrMagnitude > 1f ? move.normalized * data.moveSpeed : move * data.moveSpeed;
            body.SetGroundVelocity(velocity);
            body.SetFacing(target.x >= me.x ? 1 : -1);

            // ¿Listo para atacar?
            bool inLane = Mathf.Abs(target.y - me.y) <= data.laneTolerance * 0.8f;
            bool inRange = Mathf.Abs(target.x - me.x) <= desiredDistance + 0.25f;
            if (hasToken && inLane && inRange && attackCooldown <= 0f)
            {
                state = State.Windup;
                timer = data.attackWindup;
                attackDirection = target.x >= me.x ? 1 : -1;
                body.SetFacing(attackDirection);
                health.SetTelegraph(true);
            }
        }

        private Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            Vector2 me = body.GroundPosition;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBrain other = active[i];
                if (other == this) continue;
                Vector2 d = me - other.body.GroundPosition;
                float dist = d.magnitude;
                if (dist > 0.001f && dist < separationRadius)
                    push += d / dist * (1f - dist / separationRadius);
            }
            return push;
        }

        private void BeginAttack(PlayerController player)
        {
            health.SetTelegraph(false);
            attackLanded = false;

            if (data.attackType == EnemyAttackType.Ranged)
            {
                FireProjectile();
                state = State.Recover;
                timer = 0.35f;
                attackCooldown = data.attackCooldown;
                AttackTokens.Release(this);
                return;
            }

            // Embestida corta hacia el jugador.
            state = State.Attack;
            timer = 0.22f;
        }

        private void UpdateAttack(PlayerController player, float dt)
        {
            timer -= dt;
            body.SetGroundVelocity(new Vector2(attackDirection * data.moveSpeed * 3.5f, 0f));

            if (!attackLanded && player != null)
            {
                Vector2 center = body.GroundPosition + new Vector2(attackDirection * 0.5f, 0f);
                Hurtbox target = Hurtbox.QueryFirst(Team.Player, center, 0.5f, 0.8f, data.laneTolerance, body.GroundPosition.x);
                if (target != null)
                {
                    DamageInfo info = DamageInfo.Create(data.attackDamage, DamageKind.Contact, Team.Enemy, body.GroundPosition);
                    info.knockback = data.attackKnockback;
                    HitResult result = target.Damageable.ApplyDamage(info);
                    attackLanded = true;

                    if (result.outcome == HitOutcome.Blocked)
                    {
                        // Rebota contra el escudo.
                        knockbackVelocity = -attackDirection * 5f;
                        state = State.Hurt;
                        timer = 0.35f;
                        attackCooldown = data.attackCooldown;
                        AttackTokens.Release(this);
                        return;
                    }
                }
            }

            if (timer <= 0f)
            {
                state = State.Recover;
                timer = 0.5f;
                attackCooldown = data.attackCooldown;
                AttackTokens.Release(this);
            }
        }

        private void FireProjectile()
        {
            if (projectilePool == null) return;
            Projectile p = projectilePool.Get();
            p.Launch(new ProjectileShot
            {
                origin = body.GroundPosition + new Vector2(attackDirection * 0.5f, 0f),
                height = body.Height + 0.9f,
                direction = attackDirection,
                speed = data.projectileSpeed,
                range = data.projectileRange,
                damage = data.attackDamage,
                knockback = data.attackKnockback,
                laneTolerance = data.laneTolerance,
                halfWidth = 0.2f,
                sourceTeam = Team.Enemy,
                targetTeam = Team.Player,
                medication = null,
                color = data.projectileColor
            }, finished => projectilePool.Release(finished));
        }

        private void OnHit(DamageInfo info, HitResult result)
        {
            if (!result.Landed || state == State.Dead) return;

            health.SetTelegraph(false);
            AttackTokens.Release(this);

            float dir = body.GroundPosition.x >= info.sourcePosition.x ? 1f : -1f;
            knockbackVelocity = dir * info.knockback * (1f - data.knockbackResistance);

            // Los enemigos pesados no se interrumpen con cada golpe.
            if (data.knockbackResistance < 0.8f || state != State.Windup)
            {
                state = State.Hurt;
                timer = data.hurtStunTime;
            }
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            body.SetGroundVelocity(Vector2.zero);
            AttackTokens.Release(this);
            enabled = false;
        }

        private void GoIdle()
        {
            state = State.Idle;
            health.SetTelegraph(false);
            AttackTokens.Release(this);
            body.SetGroundVelocity(Vector2.zero);
        }
    }
}
