using System.Collections.Generic;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// IA de enemigo en vista lateral (estilo Metal Slug):
    /// - De suelo: camina hacia el jugador sin caerse de las plataformas, avisa (se ilumina) y embiste.
    /// - Volador: flota sobre el jugador, guarda distancia, avisa y dispara apuntándole.
    /// Usa "fichas de ataque" (máx. 2 atacando a la vez) para que los grupos sean justos.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyBrain : MonoBehaviour
    {
        private enum State { Idle, Approach, Windup, Attack, Recover, Hurt, Dead }

        [SerializeField] private EnemyData data;
        [SerializeField] private Transform visual;

        [Tooltip("Solo para enemigos que disparan.")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        [Tooltip("Distancia a la que esperan su turno los que no tienen ficha de ataque.")]
        [SerializeField] private float waitDistance = 3.5f;

        private static readonly List<EnemyBrain> active = new List<EnemyBrain>();

        private CharacterMotor2D motor;   // null en voladores
        private EnemyHealth health;
        private ComponentPool<Projectile> projectilePool;
        private State state = State.Idle;
        private float timer;
        private float attackCooldown;
        private float knockbackVelocity;
        private int facing = -1;
        private int attackDirection;
        private bool attackLanded;
        private float bobPhase;
        private Vector3 visualBaseScale = Vector3.one;

        public EnemyData Data => data;
        private bool IsFlying => motor == null;
        private Vector2 Position => transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        private void Awake()
        {
            motor = GetComponent<CharacterMotor2D>();
            health = GetComponent<EnemyHealth>();
            attackCooldown = Random.Range(0.4f, 1.2f);
            bobPhase = Random.value * 10f;
            if (visual != null) visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
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

        private void OnDestroy() => projectilePool?.Clear();

        public void Configure(EnemyData enemyData) => data = enemyData;

        private void Start()
        {
            if (data != null && data.attackType == EnemyAttackType.Ranged && projectilePrefab != null)
                projectilePool = new ComponentPool<Projectile>(projectilePrefab, 2, 6);
        }

        private void Update()
        {
            if (data == null || state == State.Dead) return;

            // Cayó a un hueco: cuenta como eliminado.
            if (Position.y < WalkableArea.KillY)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime;
            if (attackCooldown > 0f) attackCooldown -= dt;

            PlayerController player = PlayerController.Current;
            bool playerAvailable = player != null && player.Health != null && player.Health.IsAlive;

            switch (state)
            {
                case State.Idle:
                    Move(Vector2.zero);
                    if (playerAvailable && Vector2.Distance(Position, player.Position) <= data.aggroRange) state = State.Approach;
                    break;

                case State.Approach:
                    if (!playerAvailable) { GoIdle(); break; }
                    UpdateApproach(player);
                    break;

                case State.Windup:
                    Move(Vector2.zero);
                    timer -= dt;
                    if (timer <= 0f) BeginAttack(player);
                    break;

                case State.Attack:
                    UpdateAttack(player);
                    break;

                case State.Recover:
                    Move(Vector2.zero);
                    timer -= dt;
                    if (timer <= 0f) state = State.Approach;
                    break;

                case State.Hurt:
                    timer -= dt;
                    knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, 20f * dt);
                    Move(new Vector2(knockbackVelocity, 0f));
                    if (timer <= 0f) state = State.Approach;
                    break;
            }
        }

        // ---------------------------------------------------------------- movimiento

        private void Move(Vector2 velocity)
        {
            if (motor != null)
            {
                motor.SetHorizontalVelocity(velocity.x);
            }
            else
            {
                // Volador: se mueve directo, con un vaivén suave.
                bobPhase += Time.deltaTime * 2.5f;
                Vector2 bob = new Vector2(0f, Mathf.Sin(bobPhase) * 0.6f);
                transform.position += (Vector3)((velocity + bob) * Time.deltaTime);
            }
        }

        private void Face(int direction)
        {
            if (direction == 0) return;
            facing = direction;
            if (motor != null) motor.SetFacing(direction);
            else if (visual != null) visual.localScale = new Vector3(visualBaseScale.x * direction, visualBaseScale.y, visualBaseScale.z);
        }

        private void UpdateApproach(PlayerController player)
        {
            Vector2 me = Position;
            Vector2 target = player.Position;
            int side = me.x >= target.x ? 1 : -1; // de qué lado del jugador estoy
            Face(-side);

            bool hasToken = AttackTokens.TryAcquire(this);
            float desiredDistance = hasToken ? data.attackDistance * (data.attackType == EnemyAttackType.Melee ? 0.85f : 1f) : waitDistance;
            if (data.attackType == EnemyAttackType.Ranged) desiredDistance = Mathf.Max(desiredDistance, data.attackDistance);

            float desiredX = target.x + side * desiredDistance;
            float dx = desiredX - me.x;
            Vector2 velocity = Vector2.zero;
            if (Mathf.Abs(dx) > 0.2f) velocity.x = Mathf.Sign(dx) * data.moveSpeed;

            if (IsFlying)
            {
                float desiredY = target.y + data.flyHeight;
                float dy = desiredY - me.y;
                if (Mathf.Abs(dy) > 0.2f) velocity.y = Mathf.Sign(dy) * data.moveSpeed * 0.7f;
            }
            else if (velocity.x != 0f)
            {
                // No caerse de la plataforma.
                int moveDir = velocity.x > 0f ? 1 : -1;
                motor.SetFacing(moveDir);
                if (!motor.HasGroundAhead()) velocity.x = 0f;
                motor.SetFacing(-side);
            }

            velocity.x += Separation() * 1.5f;
            Move(velocity);

            // ¿Listo para atacar?
            bool inRange = Mathf.Abs(target.x - me.x) <= desiredDistance + 0.4f;
            bool heightOk = IsFlying || Mathf.Abs(target.y - me.y) < 1.2f;
            if (hasToken && inRange && heightOk && attackCooldown <= 0f)
            {
                state = State.Windup;
                timer = data.attackWindup;
                attackDirection = -side;
                health.SetTelegraph(true);
            }
        }

        private float Separation()
        {
            float push = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBrain other = active[i];
                if (other == this || other.IsFlying != IsFlying) continue;
                float d = Position.x - other.Position.x;
                if (Mathf.Abs(d) < 0.7f && Mathf.Abs(Position.y - other.Position.y) < 1f)
                    push += (d >= 0f ? 1f : -1f) * (1f - Mathf.Abs(d) / 0.7f);
            }
            return push;
        }

        // ---------------------------------------------------------------- ataque

        private void BeginAttack(PlayerController player)
        {
            health.SetTelegraph(false);
            attackLanded = false;

            if (data.attackType == EnemyAttackType.Ranged)
            {
                FireProjectile(player);
                state = State.Recover;
                timer = 0.4f;
                attackCooldown = data.attackCooldown;
                AttackTokens.Release(this);
                return;
            }

            state = State.Attack;
            timer = 0.22f;
        }

        private void UpdateAttack(PlayerController player)
        {
            timer -= Time.deltaTime;
            Move(new Vector2(attackDirection * data.moveSpeed * 3.5f, 0f));

            if (!attackLanded && player != null)
            {
                Rect area = HitMath.FromCenter(Position + new Vector2(attackDirection * 0.5f, 0.6f), new Vector2(0.9f, 1.2f));
                Hurtbox playerBox = player.GetComponent<Hurtbox>();
                if (playerBox != null && playerBox.IsAlive && playerBox.OverlapsRect(area))
                {
                    DamageInfo info = DamageInfo.Create(data.attackDamage, DamageKind.Contact, Team.Enemy, Position);
                    info.knockback = data.attackKnockback;
                    HitResult result = playerBox.Damageable.ApplyDamage(info);
                    attackLanded = true;

                    if (result.outcome == HitOutcome.Blocked)
                    {
                        // Rebota contra el escudo.
                        knockbackVelocity = -attackDirection * 6f;
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

        private void FireProjectile(PlayerController player)
        {
            if (projectilePool == null || player == null) return;
            Vector2 origin = Position + new Vector2(facing * 0.4f, IsFlying ? 0f : 0.8f);
            Vector2 targetPoint = player.Position + new Vector2(0f, player.IsCrouching ? 0.5f : 1f);
            Vector2 dir = IsFlying ? (targetPoint - origin).normalized : new Vector2(facing, 0f);

            Projectile p = projectilePool.Get();
            p.Launch(new ProjectileShot
            {
                origin = origin,
                direction = dir,
                speed = data.projectileSpeed,
                range = data.projectileRange,
                damage = data.attackDamage,
                knockback = data.attackKnockback,
                radius = 0.22f,
                sourceTeam = Team.Enemy,
                targetTeam = Team.Player,
                medication = null,
                color = data.projectileColor,
                groundMask = groundMask
            }, finished => projectilePool.Release(finished));
        }

        // ---------------------------------------------------------------- daño

        private void OnHit(DamageInfo info, HitResult result)
        {
            if (!result.Landed || state == State.Dead) return;

            health.SetTelegraph(false);
            AttackTokens.Release(this);

            float dir = Position.x >= info.sourcePosition.x ? 1f : -1f;
            knockbackVelocity = dir * info.knockback * (1f - data.knockbackResistance);

            // Los pesados no se interrumpen con cada golpe mientras preparan su ataque.
            if (data.knockbackResistance < 0.8f || state != State.Windup)
            {
                state = State.Hurt;
                timer = data.hurtStunTime;
            }
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            Move(Vector2.zero);
            AttackTokens.Release(this);
            enabled = false;
        }

        private void GoIdle()
        {
            state = State.Idle;
            health.SetTelegraph(false);
            AttackTokens.Release(this);
            Move(Vector2.zero);
        }
    }
}
