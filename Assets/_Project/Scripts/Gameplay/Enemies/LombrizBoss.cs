using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Depth;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Jefe lombriz (versión 2.5D del jefe del prototipo).
    /// Ciclo: bajo tierra → aviso (montículo que tiembla en tu franja) → ataque → expuesta → bajo tierra.
    /// Ataques:
    ///  - Embestida: cruza la arena a ras de piso por tu franja. Se esquiva cambiando de profundidad.
    ///  - Arco: sale del suelo, salta en arco por tu franja y vuelve a entrar.
    /// Solo recibe daño mientras está fuera. Fases por vida: más rápida y, desde la fase 2, invoca parásitos.
    /// Huida: si 'fleeAtHealth01' > 0, al llegar a ese % de vida se escapa (Intestino delgado) en lugar de morir.
    /// El cuerpo son segmentos que siguen el rastro de la cabeza.
    /// </summary>
    [RequireComponent(typeof(DepthBody), typeof(EnemyHealth))]
    public class LombrizBoss : MonoBehaviour
    {
        private enum State { Intro, Underground, Telegraph, Moving, Exposed, Fleeing, Dead }
        private enum Attack { Lunge, Arc }

        [Header("Identidad")]
        [SerializeField] private string bossName = "LOMBRIZ";
        [SerializeField, TextArea] private string introMessage = "¡Un foco de infección! Elimínalo con Antiparasitario.";
        [SerializeField, TextArea] private string defeatMessage = "Foco eliminado.";

        [Header("Huida (0 = pelea hasta morir)")]
        [Range(0f, 0.95f)] [SerializeField] private float fleeAtHealth01 = 0f;
        [SerializeField, TextArea] private string fleeMessage = "NODO: ¡Escapa! Síguela.";

        [Header("Cuerpo")]
        [SerializeField] private Transform segmentsRoot;
        [SerializeField] private Sprite segmentSprite;
        [SerializeField] private Color segmentColor = new Color(0.95f, 0.6f, 0.6f);
        [SerializeField] private int segmentCount = 12;
        [SerializeField] private float segmentSpacing = 0.5f;
        [SerializeField] private float headSize = 1.4f;
        [SerializeField] private float tailSize = 0.45f;
        [SerializeField] private SpriteRenderer moundRenderer;

        [Header("Ritmo")]
        [SerializeField] private float undergroundTime = 1.4f;
        [SerializeField] private float telegraphTime = 1.0f;
        [SerializeField] private float exposedTime = 1.6f;

        [Header("Embestida")]
        [SerializeField] private float lungeSpeed = 9f;
        [SerializeField] private float lungeHeight = 0.6f;

        [Header("Arco")]
        [SerializeField] private float arcWidth = 7f;
        [SerializeField] private float arcHeight = 3.5f;
        [SerializeField] private float arcDuration = 1.3f;

        [Header("Daño")]
        [SerializeField] private float contactDamage = 20f;
        [SerializeField] private float contactKnockback = 7f;
        [SerializeField] private float laneTolerance = 0.5f;

        [Header("Fases (por % de vida)")]
        [SerializeField] private float phase2At = 0.6f;
        [SerializeField] private float phase3At = 0.3f;

        [Header("Invocar (fase 2 en adelante)")]
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private int minionsPerSummon = 2;
        [SerializeField] private int maxMinionsAlive = 3;

        private const float BelowGround = -1.2f;

        private DepthBody body;
        private EnemyHealth health;
        private Hurtbox hurtbox;

        private State state = State.Intro;
        private Attack attack;
        private float timer;
        private float laneY;
        private float startX, endX;
        private int direction = 1;
        private float progress;          // 0..1 del recorrido, sigue un poco más para enterrar la cola
        private float pathDuration;
        private bool hitThisAttack;
        private int chainedLunges;

        private readonly List<Vector3> trail = new List<Vector3>(); // (x, profundidad, altura)
        private SpriteRenderer[] headRenderers;
        private Transform[] segments;
        private SpriteRenderer[] segmentRenderers;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();

        private int Phase
        {
            get
            {
                float h = health.Health01;
                if (h <= phase3At) return 3;
                if (h <= phase2At) return 2;
                return 1;
            }
        }

        private float SpeedMultiplier => Phase == 3 ? 1.5f : Phase == 2 ? 1.25f : 1f;

        private void Awake()
        {
            body = GetComponent<DepthBody>();
            health = GetComponent<EnemyHealth>();
            hurtbox = GetComponent<Hurtbox>();
            body.SetKinematicHeight(true);
            headRenderers = body.Visual != null ? body.Visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            BuildSegments();
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
        }

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
        }

        private void Start()
        {
            health.HealthFloor01 = fleeAtHealth01;
            SetExposed(false);
            body.SetHeight(BelowGround);
            laneY = body.GroundPosition.y;
            if (moundRenderer != null) moundRenderer.enabled = false;

            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);

            state = State.Intro;
            timer = 1.2f;
        }

        // ---------------------------------------------------------------- ciclo

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;

            if (fleeAtHealth01 > 0f && state != State.Fleeing && health.Health01 <= fleeAtHealth01 + 0.0001f)
                BeginFlee();

            switch (state)
            {
                case State.Intro:
                case State.Underground:
                    timer -= dt;
                    if (timer <= 0f) BeginTelegraph();
                    break;

                case State.Telegraph:
                    timer -= dt;
                    UpdateMound(true);
                    if (timer <= 0f) BeginMove();
                    break;

                case State.Moving:
                case State.Fleeing:
                    UpdateMove(dt);
                    break;

                case State.Exposed:
                    timer -= dt;
                    body.SetHeight(0.35f + Mathf.Sin(Time.time * 6f) * 0.1f);
                    RecordTrail();
                    if (timer <= 0f) Burrow();
                    break;
            }

            UpdateSegments();
        }

        private void BeginTelegraph()
        {
            PlayerController player = PlayerController.Current;
            float playerX = player != null ? player.Body.GroundPosition.x : (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f;
            laneY = player != null ? player.Body.GroundPosition.y : body.GroundPosition.y;

            float minX = WalkableArea.MinX + 0.6f;
            float maxX = WalkableArea.MaxX - 0.6f;

            if (chainedLunges > 0)
            {
                attack = Attack.Lunge;
                chainedLunges--;
            }
            else
            {
                attack = Random.value < (Phase == 1 ? 0.6f : 0.5f) ? Attack.Lunge : Attack.Arc;
                if (attack == Attack.Lunge && Phase == 3) chainedLunges = 1; // en fase 3 encadena dos embestidas
            }

            if (attack == Attack.Lunge)
            {
                // Sale del lado contrario al jugador y cruza toda la arena.
                bool fromRight = playerX < (minX + maxX) * 0.5f;
                startX = fromRight ? maxX : minX;
                endX = fromRight ? minX : maxX;
                pathDuration = Mathf.Abs(endX - startX) / (lungeSpeed * SpeedMultiplier);
            }
            else
            {
                direction = Random.value < 0.5f ? 1 : -1;
                startX = Mathf.Clamp(playerX - direction * arcWidth * 0.5f, minX, maxX);
                endX = Mathf.Clamp(playerX + direction * arcWidth * 0.5f, minX, maxX);
                if (Mathf.Abs(endX - startX) < 2f) endX = Mathf.Clamp(startX + (startX < (minX + maxX) * 0.5f ? arcWidth : -arcWidth), minX, maxX);
                pathDuration = arcDuration / SpeedMultiplier;
            }

            direction = endX >= startX ? 1 : -1;
            body.Teleport(new Vector2(startX, laneY));
            body.SetHeight(BelowGround);
            body.SetFacing(direction);
            trail.Clear();

            state = State.Telegraph;
            timer = telegraphTime / Mathf.Lerp(1f, SpeedMultiplier, 0.5f);
        }

        private void BeginMove()
        {
            UpdateMound(false);
            state = State.Moving;
            progress = 0f;
            hitThisAttack = false;
        }

        private void UpdateMove(float dt)
        {
            // Avanza más allá de 1 para que la cola termine de entrar al suelo.
            float tailTime = segmentCount * segmentSpacing / Mathf.Max(1f, Mathf.Abs(endX - startX) / Mathf.Max(0.01f, pathDuration));
            float extra = tailTime / Mathf.Max(0.01f, pathDuration);
            progress += dt / Mathf.Max(0.01f, pathDuration);

            float t = progress;
            float x = Mathf.LerpUnclamped(startX, endX, t);
            float peak = attack == Attack.Lunge || state == State.Fleeing ? lungeHeight : arcHeight;
            float h = t <= 1f
                ? (peak - BelowGround) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) + BelowGround
                : BelowGround - (t - 1f) * 3f;

            // En la embestida va casi a ras de piso: solo se hunde al principio y al final.
            if ((attack == Attack.Lunge || state == State.Fleeing) && t > 0.12f && t < 0.88f) h = peak + Mathf.Sin(Time.time * 10f) * 0.08f;

            bool clampBefore = state != State.Fleeing;
            if (clampBefore) x = Mathf.Clamp(x, WalkableArea.MinX - 3f, WalkableArea.MaxX + 3f);

            transform.position = new Vector3(x, laneY, transform.position.z);
            body.SetHeight(h);
            RecordTrail();

            bool headOut = h > -0.2f;
            SetExposed(headOut && state == State.Moving);

            if (state == State.Moving && !hitThisAttack) TryHitPlayer();

            if (progress >= 1f + extra)
            {
                if (state == State.Fleeing)
                {
                    FinishFlee();
                    return;
                }

                if (attack == Attack.Lunge && chainedLunges == 0) EmergeExposed();
                else
                {
                    if (attack == Attack.Arc && Phase >= 2) SummonMinions();
                    Burrow();
                }
            }
        }

        private void EmergeExposed()
        {
            // Asoma la cabeza al final de la embestida, "aturdida": momento para castigarla.
            state = State.Exposed;
            timer = exposedTime;
            transform.position = new Vector3(Mathf.Clamp(endX, WalkableArea.MinX + 0.6f, WalkableArea.MaxX - 0.6f), laneY, transform.position.z);
            body.SetFacing(-direction);
            trail.Clear();
            SetExposed(true);
        }

        private void Burrow()
        {
            SetExposed(false);
            body.SetHeight(BelowGround);
            state = State.Underground;
            timer = undergroundTime / SpeedMultiplier;
        }

        // ---------------------------------------------------------------- daño

        private void TryHitPlayer()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;

            Hurtbox target = player.GetComponent<Hurtbox>();
            if (target == null) return;

            // Cabeza
            if (CheckHit(target, new Vector2(transform.position.x, laneY), body.Height, headSize * 0.45f)) return;

            // Cuerpo
            for (int i = 0; i < segments.Length; i++)
            {
                if (!segmentRenderers[i].enabled) continue;
                Vector3 p = segments[i].position;
                float segHeight = p.y - laneY;
                float r = segments[i].localScale.x * 0.45f;
                if (CheckHit(target, new Vector2(p.x, laneY), segHeight, r)) return;
            }
        }

        private bool CheckHit(Hurtbox target, Vector2 ground, float height, float radius)
        {
            if (height < -0.3f) return false;
            float attackHeight = Mathf.Max(0f, height) + radius * 0.5f;
            if (!target.IsHitBy(ground, radius, attackHeight, laneTolerance)) return false;

            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, ground);
            info.knockback = contactKnockback;
            target.Damageable.ApplyDamage(info);
            hitThisAttack = true;
            return true;
        }

        private void SetExposed(bool exposed)
        {
            health.Invulnerable = !exposed;
            if (hurtbox != null && hurtbox.enabled != exposed) hurtbox.enabled = exposed;
        }

        // ---------------------------------------------------------------- huida y muerte

        private void BeginFlee()
        {
            state = State.Fleeing;
            SetExposed(false);
            UpdateMound(false);
            if (!string.IsNullOrEmpty(fleeMessage)) GameplayEvents.RaiseMessage(fleeMessage);

            // Se escapa por la derecha (hacia el siguiente tramo del intestino).
            attack = Attack.Lunge;
            startX = transform.position.x;
            endX = WalkableArea.MaxX + 6f;
            direction = 1;
            body.SetFacing(1);
            pathDuration = Mathf.Abs(endX - startX) / (lungeSpeed * 1.4f);
            progress = 0.15f;
        }

        private void FinishFlee()
        {
            state = State.Dead;
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            health.Retreat();
            GameplayEvents.RaiseBossEnded();
            Destroy(gameObject, 0.1f);
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            UpdateMound(false);
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }

        // ---------------------------------------------------------------- invocar

        private void SummonMinions()
        {
            if (minionPrefab == null) return;
            minions.RemoveAll(m => m == null || !m.IsAlive);

            int toSpawn = Mathf.Min(minionsPerSummon, maxMinionsAlive - minions.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                Vector2 pos = WalkableArea.RandomPointInDepth(transform.position.x + Random.Range(-1.5f, 1.5f));
                EnemyHealth m = Instantiate(minionPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
                if (minionData != null)
                {
                    m.Configure(minionData, null);
                    EnemyBrain brain = m.GetComponent<EnemyBrain>();
                    if (brain != null) brain.Configure(minionData);
                }
                minions.Add(m);
            }
        }

        // ---------------------------------------------------------------- cuerpo y aviso

        private void BuildSegments()
        {
            if (segmentsRoot == null)
            {
                segmentsRoot = new GameObject("Segments").transform;
                segmentsRoot.SetParent(transform, false);
            }

            segments = new Transform[segmentCount];
            segmentRenderers = new SpriteRenderer[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                var go = new GameObject("Segment_" + i);
                go.transform.SetParent(segmentsRoot, false);
                float k = segmentCount > 1 ? (float)i / (segmentCount - 1) : 0f;
                float size = Mathf.Lerp(headSize * 0.85f, tailSize, k);
                go.transform.localScale = new Vector3(size, size, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = segmentSprite;
                // Anillos alternados para que se lea como lombriz.
                sr.color = i % 2 == 0 ? segmentColor : Color.Lerp(segmentColor, Color.black, 0.12f);
                sr.sortingOrder = -1 - i;
                sr.enabled = false;

                segments[i] = go.transform;
                segmentRenderers[i] = sr;
            }
        }

        private void RecordTrail()
        {
            Vector3 head = new Vector3(transform.position.x, laneY, body.Height);
            if (trail.Count == 0 || (head - trail[0]).sqrMagnitude > 0.0004f) trail.Insert(0, head);

            int max = segmentCount * 12 + 20;
            if (trail.Count > max) trail.RemoveRange(max, trail.Count - max);
        }

        private void UpdateSegments()
        {
            if (segments == null) return;

            // La cabeza solo se ve cuando está fuera del suelo.
            bool headVisible = body.Height > -0.25f && state != State.Underground && state != State.Telegraph && state != State.Intro;
            for (int i = 0; i < headRenderers.Length; i++)
                if (headRenderers[i] != null) headRenderers[i].enabled = headVisible;

            int index = 0;
            float walked = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                float targetDistance = (i + 1) * segmentSpacing;
                Vector3 point;
                bool found = TrailPointAt(targetDistance, ref index, ref walked, out point);

                bool visible = found && point.z > -0.25f && state != State.Underground && state != State.Telegraph && state != State.Intro;
                segmentRenderers[i].enabled = visible;
                if (!found) continue;

                // Ondulación ligera para que se vea orgánica.
                float wave = Mathf.Sin(Time.time * 8f - i * 0.7f) * 0.06f;
                segments[i].position = new Vector3(point.x, point.y + Mathf.Max(point.z, -2f) + wave, transform.position.z);
            }
        }

        private bool TrailPointAt(float distance, ref int index, ref float walked, out Vector3 point)
        {
            point = Vector3.zero;
            if (trail.Count < 2) return false;

            while (index < trail.Count - 1)
            {
                Vector3 a = trail[index];
                Vector3 b = trail[index + 1];
                float seg = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
                if (walked + seg >= distance)
                {
                    float k = seg > 0.0001f ? (distance - walked) / seg : 0f;
                    point = new Vector3(Mathf.Lerp(a.x, b.x, k), a.y, Mathf.Lerp(a.z, b.z, k));
                    return true;
                }
                walked += seg;
                index++;
            }
            return false;
        }

        private void UpdateMound(bool show)
        {
            if (moundRenderer == null) return;
            moundRenderer.enabled = show;
            if (!show) return;

            // Montículo que tiembla: el aviso de por dónde va a salir.
            float shake = Mathf.Sin(Time.time * 40f) * 0.05f;
            moundRenderer.transform.localPosition = new Vector3(shake, 0f, 0f);
        }
    }
}
