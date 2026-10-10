using System;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Cerebro de Healing Knight (estilo Metal Slug): correr a velocidad constante, salto de altura fija,
    /// agacharse, apuntar en 8 direcciones (diagonal = dos teclas), disparar, cuchillazo automático,
    /// bomba de medicamento y escudo burbuja. Cada habilidad vive en su propio componente.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor2D), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public enum State { Normal, Hurt, Dead }

        [SerializeField] private PlayerConfig config;
        [Tooltip("Opcional: se aplasta al agacharse (placeholder hasta tener animaciones).")]
        [SerializeField] private Transform bodyVisual;

        private CharacterMotor2D motor;
        private PlayerInputReader input;
        private PlayerShooter shooter;
        private PlayerBombs bombs;
        private PlayerShield shield;
        private PlayerHealth health;
        private Hurtbox hurtbox;

        private State state = State.Normal;
        private float stateTimer;
        private bool crouching;
        private Vector2 aim = Vector2.right;
        private Vector2 lastSafePosition;
        private Vector3 bodyVisualBaseScale = Vector3.one;

        /// <summary>El jugador actual (el juego es para un jugador).</summary>
        public static PlayerController Current { get; private set; }
        public static event Action<PlayerController> Spawned;

        public PlayerConfig Config => config;
        public CharacterMotor2D Motor => motor;
        public PlayerInputReader Input => input;
        public PlayerShooter Shooter => shooter;
        public PlayerBombs Bombs => bombs;
        public PlayerShield Shield => shield;
        public PlayerHealth Health => health;
        public State CurrentState => state;
        public bool IsCrouching => crouching;
        public Vector2 Aim => aim;
        public Vector2 Position => motor.Position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Spawned = null;
        }

        private void Awake()
        {
            motor = GetComponent<CharacterMotor2D>();
            input = GetComponent<PlayerInputReader>();
            shooter = GetComponent<PlayerShooter>();
            bombs = GetComponent<PlayerBombs>();
            shield = GetComponent<PlayerShield>();
            health = GetComponent<PlayerHealth>();
            hurtbox = GetComponent<Hurtbox>();
            if (bodyVisual != null) bodyVisualBaseScale = bodyVisual.localScale;

            if (config == null) Debug.LogError("[PlayerController] Falta asignar PlayerConfig.", this);
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            lastSafePosition = motor.Position;
            // En Start todos los componentes del jugador ya hicieron su Awake.
            Spawned?.Invoke(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || config == null) return;

            switch (state)
            {
                case State.Normal: UpdateNormal(); break;
                case State.Hurt:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = State.Normal;
                    break;
                case State.Dead:
                    motor.SetHorizontalVelocity(0f);
                    break;
            }

            CheckFall();
        }

        private void UpdateNormal()
        {
            Vector2 move = input.Move;
            bool shielding = shield != null && shield.IsActive;

            // Agacharse (solo en el suelo).
            SetCrouch(motor.IsGrounded && move.y < -0.5f && !shielding);

            // Hacia dónde mira.
            if (Mathf.Abs(move.x) > 0.3f) motor.SetFacing(move.x > 0f ? 1 : -1);

            // Correr: velocidad constante, sin inercia (como Metal Slug).
            float speed = crouching ? config.crouchSpeed : config.runSpeed;
            if (shielding) speed *= config.shieldMoveSpeedMultiplier;
            float target = Mathf.Abs(move.x) > 0.3f ? Mathf.Sign(move.x) * speed : 0f;
            float vx = motor.IsGrounded ? target : Mathf.Lerp(motor.Velocity.x, target, config.airControl);
            motor.SetHorizontalVelocity(vx);

            // Salto de altura fija.
            if (input.JumpPressed && motor.IsGrounded && !shielding)
            {
                SetCrouch(false);
                motor.Jump(config.jumpHeight, config.timeToApex);
            }

            // Apuntar: arriba, diagonales (dos teclas), abajo solo en el aire.
            aim = crouching ? new Vector2(motor.Facing, 0f) : AimMath.Resolve(move, motor.Facing, motor.IsGrounded);

            // Medicamentos.
            if (shooter != null)
            {
                if (input.MedicationStep != 0) shooter.CycleMedication(input.MedicationStep);
                if (input.DirectMedicationIndex >= 0) shooter.SelectMedication(input.DirectMedicationIndex);
            }

            // Escudo.
            if (shield != null) shield.SetWantsBlock(input.ShieldHeld);

            if (shielding) return;

            // Disparo (o cuchillazo automático si hay un enemigo pegado).
            if (shooter != null && (input.ShootPressed || input.ShootHeld))
                shooter.TryAttack(aim, crouching, input.ShootPressed);

            // Bomba de medicamento.
            if (bombs != null && input.BombPressed) bombs.TryThrow();

            if (motor.IsGrounded) lastSafePosition = motor.Position;
        }

        private void SetCrouch(bool value)
        {
            if (crouching == value) return;
            crouching = value;
            motor.SetColliderHeight(value ? config.standingColliderHeight * config.crouchHeightFactor : config.standingColliderHeight);
            if (hurtbox != null) hurtbox.SetHeightFactor(value ? config.crouchHeightFactor : 1f);
            if (bodyVisual != null)
                bodyVisual.localScale = value
                    ? new Vector3(bodyVisualBaseScale.x * 1.1f, bodyVisualBaseScale.y * config.crouchHeightFactor, bodyVisualBaseScale.z)
                    : bodyVisualBaseScale;
        }

        private void CheckFall()
        {
            if (state == State.Dead || motor.Position.y > WalkableArea.KillY) return;

            // Caída al vacío: daño y regreso al último suelo seguro.
            DamageInfo info = DamageInfo.Create(config.fallDamage, DamageKind.Environment, Team.Neutral, motor.Position);
            info.blockable = false;
            health?.ApplyDamage(info);
            if (state != State.Dead) motor.Teleport(lastSafePosition + Vector2.up * 0.5f);
        }

        /// <summary>Lo llama PlayerHealth al recibir un golpe.</summary>
        public void OnHurt(float knockbackDirection, float knockbackForce)
        {
            if (state == State.Dead) return;
            state = State.Hurt;
            stateTimer = config.hurtStunTime;
            SetCrouch(false);
            motor.SetVelocity(new Vector2(knockbackDirection * knockbackForce, Mathf.Min(4f, knockbackForce)));
        }

        public void OnDied()
        {
            state = State.Dead;
            SetCrouch(false);
            motor.SetHorizontalVelocity(0f);
            shield?.SetWantsBlock(false);
        }

        public void Respawn(Vector2 position)
        {
            motor.Teleport(position);
            motor.SetFacing(1);
            lastSafePosition = position;
            state = State.Normal;
            health?.ResetHealth();
            shield?.ResetEnergy();
            bombs?.Refill(); // como en Metal Slug: vida nueva, bombas completas
        }
    }
}
