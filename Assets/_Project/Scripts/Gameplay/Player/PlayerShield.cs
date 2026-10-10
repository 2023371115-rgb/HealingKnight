using System;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Depth;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Escudo (GDD 5.8): bloquea golpes y proyectiles que llegan por el frente y gasta energía.
    /// Si la energía llega a 0 se rompe y no se puede usar un rato. No bloquea el ácido ni el daño ambiental.
    /// Con el escudo arriba no se puede disparar, golpear ni saltar, y te mueves más lento.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShield : MonoBehaviour
    {
        [Tooltip("Visual del escudo (se activa mientras bloquea).")]
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private Color fullColor = new Color(0.4f, 0.9f, 1f, 0.7f);
        [SerializeField] private Color lowColor = new Color(1f, 0.35f, 0.25f, 0.7f);

        private PlayerController controller;
        private SpriteRenderer shieldRenderer;
        private float energy;
        private bool wantsBlock;
        private bool broken;
        private float brokenTimer;
        private float lastHitTime = -999f;

        public bool IsActive => wantsBlock && !broken && energy > 0f;
        public bool IsBroken => broken;
        public float Energy01 => controller != null && controller.Config.shieldMaxEnergy > 0f ? energy / controller.Config.shieldMaxEnergy : 0f;

        public event Action Blocked;
        public event Action Broke;
        public event Action<float> EnergyChanged; // 0..1

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (shieldVisual != null)
            {
                shieldRenderer = shieldVisual.GetComponentInChildren<SpriteRenderer>(true);
                shieldVisual.SetActive(false);
            }
        }

        private void Start()
        {
            ResetEnergy();
        }

        public void SetWantsBlock(bool value) => wantsBlock = value;

        public void ResetEnergy()
        {
            energy = controller.Config.shieldMaxEnergy;
            broken = false;
            EnergyChanged?.Invoke(Energy01);
        }

        private void Update()
        {
            PlayerConfig c = controller.Config;
            float dt = Time.deltaTime;

            if (broken)
            {
                brokenTimer -= dt;
                if (brokenTimer <= 0f)
                {
                    broken = false;
                    energy = c.shieldMaxEnergy * 0.25f;
                    EnergyChanged?.Invoke(Energy01);
                }
            }
            else if (energy < c.shieldMaxEnergy && Time.time >= lastHitTime + c.shieldRegenDelay)
            {
                energy = Mathf.Min(c.shieldMaxEnergy, energy + c.shieldRegenPerSecond * dt);
                EnergyChanged?.Invoke(Energy01);
            }

            if (shieldVisual != null)
            {
                bool active = IsActive;
                if (shieldVisual.activeSelf != active) shieldVisual.SetActive(active);
                if (active && shieldRenderer != null) shieldRenderer.color = Color.Lerp(lowColor, fullColor, Energy01);
            }
        }

        /// <summary>Intenta bloquear un golpe. Devuelve true si lo detuvo.</summary>
        public bool TryBlock(DamageInfo info)
        {
            if (!IsActive || !info.blockable || info.kind == DamageKind.Environment) return false;

            float ownerX = controller.Body.GroundPosition.x;
            if (!DepthMath.IsInFront(ownerX, controller.Body.Facing, info.sourcePosition.x)) return false;

            PlayerConfig c = controller.Config;
            float cost = info.kind == DamageKind.Projectile ? c.shieldProjectileCost : c.shieldContactCost;
            energy = Mathf.Max(0f, energy - cost);
            lastHitTime = Time.time;
            Blocked?.Invoke();

            if (energy <= 0f)
            {
                broken = true;
                brokenTimer = c.shieldBreakCooldown;
                Broke?.Invoke();
            }

            EnergyChanged?.Invoke(Energy01);
            return true;
        }
    }
}
