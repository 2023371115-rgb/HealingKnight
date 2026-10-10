using System;
using HealingKnight.Core.Depth;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Cerebro de Healing Knight: lee los controles y coordina movimiento, salto, disparo, golpe y escudo.
    /// Cada habilidad vive en su propio componente; este solo decide qué se puede hacer en cada momento.
    /// </summary>
    [RequireComponent(typeof(DepthBody), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public enum State { Normal, Hurt, Dead }

        [SerializeField] private PlayerConfig config;

        private DepthBody body;
        private PlayerInputReader input;
        private PlayerShooter shooter;
        private PlayerMelee meleeAttack;
        private PlayerShield shield;
        private PlayerHealth health;

        private State state = State.Normal;
        private float stateTimer;
        private Vector2 velocity;
        private float knockbackVelocity;

        /// <summary>El jugador actual (el juego es para un jugador).</summary>
        public static PlayerController Current { get; private set; }
        public static event Action<PlayerController> Spawned;

        public PlayerConfig Config => config;
        public DepthBody Body => body;
        public PlayerInputReader Input => input;
        public PlayerShooter Shooter => shooter;
        public PlayerMelee Melee => meleeAttack;
        public PlayerShield Shield => shield;
        public PlayerHealth Health => health;
        public State CurrentState => state;
        public bool IsBusyAttacking => meleeAttack != null && meleeAttack.IsAttacking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Spawned = null;
        }

        private void Awake()
        {
            body = GetComponent<DepthBody>();
            input = GetComponent<PlayerInputReader>();
            shooter = GetComponent<PlayerShooter>();
            meleeAttack = GetComponent<PlayerMelee>();
            shield = GetComponent<PlayerShield>();
            health = GetComponent<PlayerHealth>();

            if (config == null) Debug.LogError("[PlayerController] Falta asignar PlayerConfig.", this);
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void Start()
        {
            // En Start todos los componentes del jugador ya hicieron su Awake.
            Spawned?.Invoke(this);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || config == null) return;

            switch (state)
            {
                case State.Normal: UpdateNormal(dt); break;
                case State.Hurt: UpdateHurt(dt); break;
                case State.Dead: body.SetGroundVelocity(Vector2.zero); break;
            }
        }

        private void UpdateNormal(float dt)
        {
            bool shielding = shield != null && shield.IsActive;
            bool attacking = IsBusyAttacking;

            // Movimiento
            Vector2 wanted = input.Move;
            float speed = config.moveSpeed;
            if (shielding) speed *= config.shieldMoveSpeedMultiplier;
            if (attacking) speed *= 0.3f;

            Vector2 target = new Vector2(wanted.x * speed, wanted.y * speed * config.depthSpeedFactor);
            float control = body.IsGrounded ? 1f : config.airControl;
            float rate = (wanted.sqrMagnitude > 0.01f ? config.acceleration : config.deceleration) * control;
            velocity = Vector2.MoveTowards(velocity, target, rate * dt);
            body.SetGroundVelocity(velocity);

            // Hacia dónde mira: no se voltea mientras bloquea ni mientras golpea.
            if (!shielding && !attacking && Mathf.Abs(wanted.x) > 0.1f)
                body.SetFacing(wanted.x > 0f ? 1 : -1);

            // Salto
            if (input.JumpPressed && body.IsGrounded && !shielding && !attacking)
            {
                body.Jump(JumpMath.InitialVelocity(config.jumpHeight, config.timeToApex),
                    JumpMath.Gravity(config.jumpHeight, config.timeToApex),
                    config.fallGravityMultiplier);
            }

            // Medicamentos
            if (shooter != null)
            {
                if (input.MedicationStep != 0) shooter.CycleMedication(input.MedicationStep);
                if (input.DirectMedicationIndex >= 0) shooter.SelectMedication(input.DirectMedicationIndex);
            }

            // Escudo
            if (shield != null) shield.SetWantsBlock(input.ShieldHeld && !attacking);

            // Ataques (no con el escudo arriba)
            if (!shielding)
            {
                if (meleeAttack != null && input.MeleePressed) meleeAttack.TryAttack();
                else if (shooter != null && input.ShootHeld && !attacking) shooter.TryShoot();
            }
        }

        private void UpdateHurt(float dt)
        {
            stateTimer -= dt;
            knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, config.knockbackDecay * dt);
            body.SetGroundVelocity(new Vector2(knockbackVelocity, 0f));
            if (shield != null) shield.SetWantsBlock(false);

            if (stateTimer <= 0f)
            {
                state = State.Normal;
                velocity = Vector2.zero;
            }
        }

        /// <summary>Lo llama PlayerHealth al recibir un golpe.</summary>
        public void OnHurt(float knockbackDirection, float knockbackForce)
        {
            if (state == State.Dead) return;
            state = State.Hurt;
            stateTimer = config.hurtStunTime;
            knockbackVelocity = knockbackDirection * knockbackForce;
            meleeAttack?.Cancel();
            if (knockbackForce > 0.01f && body.IsGrounded) body.Pop(3f);
        }

        public void OnDied()
        {
            state = State.Dead;
            velocity = Vector2.zero;
            body.SetGroundVelocity(Vector2.zero);
            shield?.SetWantsBlock(false);
            meleeAttack?.Cancel();
        }

        public void Respawn(Vector2 groundPosition)
        {
            body.Teleport(groundPosition);
            body.SetFacing(1);
            velocity = Vector2.zero;
            knockbackVelocity = 0f;
            state = State.Normal;
            health?.ResetHealth();
            shield?.ResetEnergy();
        }
    }
}
