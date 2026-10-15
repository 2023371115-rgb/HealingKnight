using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Jefe del Hipocampo: el virus de la rabia (mutado). En la vida real viaja POR DENTRO de los nervios
    /// hasta el cerebro y causa agitación ("furia"). Por eso:
    ///  - Fase 1 (100–60 %): corre por los "rieles" de axones de la arena, se detiene, avisa y dispara ráfagas.
    ///    Cambia de riel. De vez en cuando invoca un rayo sobre ti.
    ///  - Fase 2 (60–30 %): además INFECTA neuronas sanas (las vuelve rojas, hiperexcitadas) y llama rayos en cadena.
    ///  - Fase 3 (30–0 %): FURIA. Embestidas en el aire hacia ti con aviso, rayos más seguidos y todo más rápido.
    /// Se trata con antiviral. Los rayos los controla él (la tormenta de la arena no cae sola).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class RabiesBoss : MonoBehaviour
    {
        private enum State { Intro, RailMove, Burst, SwitchRail, Infect, Charge, Recover, Dead }

        [Header("Identidad")]
        [SerializeField] private string bossName = "VIRUS DE LA RABIA";
        [SerializeField, TextArea] private string introMessage =
            "NODO: ¡Rabia! Viaja por dentro de los nervios hasta el cerebro. Antiviral. (En la vida real se previene con la vacuna.)";
        [SerializeField, TextArea] private string phase2Message = "NODO: ¡Está infectando neuronas! Cálmalas con Anticonvulsivo.";
        [SerializeField, TextArea] private string phase3Message = "NODO: ¡Fase de furia! Esquiva sus embestidas.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Virus de la rabia eliminado. El hipocampo está a salvo.";

        [Header("Partes")]
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer[] glowRenderers;
        [SerializeField] private Color glowIdle = new Color(0.9f, 0.2f, 0.45f);
        [SerializeField] private Color glowWarn = new Color(1f, 0.95f, 0.5f);

        [Header("Rieles de axones (altura sobre el piso)")]
        [SerializeField] private float floorY = 0f;
        [SerializeField] private float[] railHeights = { 3.4f, 6f };
        [SerializeField] private float railSpeed = 7f;

        [Header("Ráfaga")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float burstTelegraph = 0.55f;
        [SerializeField] private int burstShots = 3;
        [SerializeField] private float shotSpeed = 8.5f;
        [SerializeField] private float shotDamage = 10f;
        [SerializeField] private Color shotColor = new Color(1f, 0.4f, 0.7f);

        [Header("Rayos")]
        [SerializeField] private LightningStorm stormPrefab;

        [Header("Infección (fase 2+)")]
        [SerializeField] private EnemyHealth redNeuronPrefab;
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;

        [Header("Furia (fase 3)")]
        [SerializeField] private float chargeTelegraph = 0.6f;
        [SerializeField] private float chargeSpeed = 16f;

        [Header("Contacto")]
        [SerializeField] private float contactRadius = 1.3f;
        [SerializeField] private float contactDamage = 18f;

        [Header("Fases")]
        [SerializeField] private float phase2At = 0.6f;
        [SerializeField] private float phase3At = 0.3f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private LightningStorm storm;
        private EnemyHealth minion;

        private State state = State.Intro;
        private float timer;
        private int rail;
        private float targetX;
        private Vector2 chargeTarget;
        private Vector2 chargeDirection;
        private int lastPhase = 1;
        private int cycles;
        private float contactCooldown;
        private float lightningTimer;
        private int pendingStrikes;
        private float strikeGap;

        private int Phase => health.Health01 <= phase3At ? 3 : health.Health01 <= phase2At ? 2 : 1;
        private float Speed => Phase == 3 ? 1.45f : Phase == 2 ? 1.2f : 1f;
        private Vector2 Position => transform.position;
        private float RailY => floorY + railHeights[Mathf.Clamp(rail, 0, railHeights.Length - 1)];

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            if (storm != null) Destroy(storm.gameObject);
            if (minion != null) Destroy(minion.gameObject);
        }

        private void Start()
        {
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 10, 30);
            if (stormPrefab != null)
            {
                storm = Instantiate(stormPrefab);
                storm.Configure(WalkableArea.MinX, WalkableArea.MaxX, floorY);
                storm.AutoStrikes = false;
            }

            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);

            rail = 0;
            transform.position = new Vector3(WalkableArea.MaxX + 3f, RailY, 0f); // entra corriendo por el riel
            targetX = (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f + 3f;
            state = State.Intro;
            lightningTimer = 4f;
            SetGlow(false);
        }

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            if (contactCooldown > 0f) contactCooldown -= dt;

            PlayerController player = PlayerController.Current;
            bool playerAlive = player != null && player.Health != null && player.Health.IsAlive;

            CheckPhaseChange();
            UpdateLightning(player, playerAlive, dt);

            switch (state)
            {
                case State.Intro:
                case State.RailMove:
                    MoveAlongRail(dt);
                    if (Mathf.Abs(Position.x - targetX) < 0.15f)
                    {
                        if (state == State.Intro) { state = State.RailMove; PickNextAction(player); }
                        else if (playerAlive) BeginBurst();
                    }
                    break;

                case State.Burst:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        if (playerAlive) FireBurst(player);
                        SetGlow(false);
                        state = State.Recover;
                        timer = 0.5f / Speed;
                    }
                    break;

                case State.SwitchRail:
                {
                    Vector2 p = Vector2.MoveTowards(Position, new Vector2(Position.x, RailY), railSpeed * Speed * dt);
                    transform.position = new Vector3(p.x, p.y, 0f);
                    if (Mathf.Abs(Position.y - RailY) < 0.05f) PickNextAction(player);
                    break;
                }

                case State.Infect:
                    MoveTowards(targetPoint, railSpeed * Speed, dt);
                    if (Vector2.Distance(Position, targetPoint) < 0.3f)
                    {
                        InfectNeuronAt(targetPoint);
                        state = State.SwitchRail;
                    }
                    break;

                case State.Charge:
                    timer -= dt;
                    if (timer > 0f)
                    {
                        // Aviso: tiembla apuntando hacia ti.
                        if (visual != null) visual.localPosition = Random.insideUnitCircle * 0.12f;
                    }
                    else
                    {
                        if (visual != null) visual.localPosition = Vector3.zero;
                        Vector2 next = Position + chargeDirection * chargeSpeed * dt;
                        next.x = Mathf.Clamp(next.x, WalkableArea.MinX + 1f, WalkableArea.MaxX - 1f);
                        next.y = Mathf.Clamp(next.y, floorY + 1f, floorY + 7f);
                        transform.position = new Vector3(next.x, next.y, 0f);
                        if (timer < -0.8f || Vector2.Distance(Position, chargeTarget) < 0.4f)
                        {
                            SetGlow(false);
                            state = State.SwitchRail; // regresa a su riel
                        }
                    }
                    break;

                case State.Recover:
                    timer -= dt;
                    if (timer <= 0f) PickNextAction(player);
                    break;
            }

            Face(player);
            TryContactDamage(player);
        }

        private Vector2 targetPoint;

        // ---------------------------------------------------------------- decisiones

        private void PickNextAction(PlayerController player)
        {
            cycles++;
            int phase = Phase;
            bool playerAlive = player != null && player.Health.IsAlive;

            // Furia: embestida cada tanto.
            if (phase == 3 && playerAlive && cycles % 2 == 0)
            {
                BeginCharge(player);
                return;
            }

            // Infecta una neurona sana cercana (fase 2+).
            if (phase >= 2 && cycles % 3 == 0 && TryFindNeuron(out Vector2 neuronPos))
            {
                targetPoint = neuronPos + Vector2.up * 0.2f;
                state = State.Infect;
                return;
            }

            // Cambia de riel de vez en cuando.
            if (railHeights.Length > 1 && Random.value < 0.4f)
            {
                rail = (rail + 1) % railHeights.Length;
                state = State.SwitchRail;
                return;
            }

            // Corre por el riel hasta un punto cercano al jugador (pero no encima).
            float px = playerAlive ? player.Position.x : Position.x;
            float side = Random.value < 0.5f ? -1f : 1f;
            targetX = Mathf.Clamp(px + side * Random.Range(3f, 6f), WalkableArea.MinX + 1.5f, WalkableArea.MaxX - 1.5f);
            state = State.RailMove;
        }

        private void CheckPhaseChange()
        {
            int phase = Phase;
            if (phase == lastPhase) return;
            lastPhase = phase;
            if (phase == 2 && !string.IsNullOrEmpty(phase2Message)) GameplayEvents.RaiseMessage(phase2Message);
            if (phase == 3 && !string.IsNullOrEmpty(phase3Message)) GameplayEvents.RaiseMessage(phase3Message);
            if (phase >= 2) SpawnMinion();
        }

        // ---------------------------------------------------------------- movimiento

        private void MoveAlongRail(float dt)
        {
            float y = Mathf.MoveTowards(Position.y, RailY, railSpeed * dt);
            float x = Mathf.MoveTowards(Position.x, targetX, railSpeed * Speed * dt);
            transform.position = new Vector3(x, y, 0f);
        }

        private void MoveTowards(Vector2 target, float speed, float dt)
        {
            Vector2 p = Vector2.MoveTowards(Position, target, speed * dt);
            transform.position = new Vector3(p.x, p.y, 0f);
        }

        private void Face(PlayerController player)
        {
            if (visual == null) return;
            float dir;
            if (state == State.Charge && timer <= 0f) dir = chargeDirection.x >= 0f ? 1f : -1f;
            else if (state == State.RailMove || state == State.Intro) dir = targetX >= Position.x ? 1f : -1f;
            else dir = player != null && player.Position.x >= Position.x ? 1f : -1f;
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
        }

        // ---------------------------------------------------------------- ataques

        private void BeginBurst()
        {
            state = State.Burst;
            timer = burstTelegraph / Mathf.Lerp(1f, Speed, 0.5f);
            SetGlow(true);
        }

        private void FireBurst(PlayerController player)
        {
            if (pool == null) return;
            Vector2 target = player.Position + Vector2.up * 0.9f;
            Vector2 dir = (target - Position).normalized;
            int shots = burstShots + (Phase - 1);
            float spread = 12f;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - spread * (shots - 1) * 0.5f;
            for (int i = 0; i < shots; i++)
            {
                float a = (baseAngle + spread * i) * Mathf.Deg2Rad;
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = Position,
                    direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                    speed = shotSpeed * Mathf.Lerp(1f, Speed, 0.5f),
                    range = 16f,
                    damage = shotDamage,
                    knockback = 4f,
                    radius = 0.22f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    color = shotColor,
                    groundMask = groundMask
                }, finished => pool.Release(finished));
            }
        }

        private void BeginCharge(PlayerController player)
        {
            state = State.Charge;
            timer = chargeTelegraph / Mathf.Lerp(1f, Speed, 0.5f);
            chargeTarget = player.Position + Vector2.up * 0.9f;
            chargeDirection = (chargeTarget - Position).normalized;
            SetGlow(true);
        }

        /// <summary>Rayos: el jefe los invoca sobre el jugador (en cadena en fases 2 y 3).</summary>
        private void UpdateLightning(PlayerController player, bool playerAlive, float dt)
        {
            if (storm == null || !playerAlive || state == State.Intro) return;

            if (pendingStrikes > 0)
            {
                strikeGap -= dt;
                if (strikeGap <= 0f && storm.ForceStrike(player.Position.x + Random.Range(-1f, 1f)))
                {
                    pendingStrikes--;
                    strikeGap = 1.6f;
                }
                return;
            }

            lightningTimer -= dt;
            if (lightningTimer > 0f) return;
            int phase = Phase;
            pendingStrikes = phase == 1 ? 1 : phase == 2 ? 2 : 3;
            strikeGap = 0f;
            lightningTimer = phase == 1 ? 8f : phase == 2 ? 6.5f : 5f;
        }

        private bool TryFindNeuron(out Vector2 position)
        {
            position = default;
#pragma warning disable CS0618
            HealthyNeuron[] neurons = FindObjectsByType<HealthyNeuron>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            float best = float.MaxValue;
            bool found = false;
            foreach (HealthyNeuron n in neurons)
            {
                Vector2 p = n.transform.position;
                if (p.x < WalkableArea.MinX || p.x > WalkableArea.MaxX) continue;
                float d = Vector2.Distance(p, Position);
                if (d < best) { best = d; position = p; found = true; }
            }
            return found;
        }

        private void InfectNeuronAt(Vector2 point)
        {
            if (redNeuronPrefab == null) return;
#pragma warning disable CS0618
            HealthyNeuron[] neurons = FindObjectsByType<HealthyNeuron>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            foreach (HealthyNeuron n in neurons)
            {
                if (Vector2.Distance(n.transform.position, point) > 0.6f) continue;
                Instantiate(redNeuronPrefab, n.transform.position, Quaternion.identity);
                Destroy(n.gameObject);
                GameplayEvents.RaiseMessage("¡La rabia infectó una neurona! Cálmala con Anticonvulsivo.");
                return;
            }
        }

        private void SpawnMinion()
        {
            if (minionPrefab == null || (minion != null && minion.IsAlive)) return;
            minion = Instantiate(minionPrefab, Position + Vector2.down * 1f, Quaternion.identity);
            if (minionData != null)
            {
                minion.Configure(minionData, null);
                EnemyBrain brain = minion.GetComponent<EnemyBrain>();
                if (brain != null) brain.Configure(minionData);
            }
        }

        private void TryContactDamage(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(Position, contactRadius)) return;

            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, Position);
            info.knockback = 8f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void SetGlow(bool warning)
        {
            if (glowRenderers == null) return;
            foreach (SpriteRenderer r in glowRenderers)
                if (r != null) r.color = warning ? glowWarn : glowIdle;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            SetGlow(false);
            if (storm != null) Destroy(storm.gameObject);
            if (minion != null) Destroy(minion.gameObject);
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
