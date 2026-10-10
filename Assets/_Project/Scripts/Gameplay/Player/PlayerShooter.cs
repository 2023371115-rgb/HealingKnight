using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Brazo-pistola: dispara cápsulas del medicamento seleccionado y cambia entre medicamentos.
    /// El ciclo es: medicamentos genéricos + la cápsula regional del sistema actual.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Projectile capsulePrefab;

        [Tooltip("Opcional: se pinta del color del medicamento seleccionado (el brazo-pistola).")]
        [SerializeField] private SpriteRenderer tintedArm;

        [Tooltip("Para probar una escena sin pasar por el mapa: cápsula regional a usar si no hay sistema actual.")]
        [SerializeField] private MedicationData fallbackRegionalMedication;

        private PlayerController controller;
        private ComponentPool<Projectile> pool;
        private readonly List<MedicationData> loadout = new List<MedicationData>();
        private int selectedIndex;
        private float cooldown;

        public event Action<MedicationData> MedicationChanged;
        public event Action Fired;

        public MedicationData Current => loadout.Count > 0 ? loadout[selectedIndex] : null;
        public IReadOnlyList<MedicationData> Loadout => loadout;
        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            BuildLoadout();

            if (capsulePrefab == null)
            {
                Debug.LogError("[PlayerShooter] Falta asignar el prefab de la cápsula.", this);
                return;
            }

            int max = Mathf.Max(1, controller.Config.maxActiveCapsules);
            pool = new ComponentPool<Projectile>(capsulePrefab, max, max * 2);
        }

        private void OnDestroy()
        {
            pool?.Clear();
        }

        private void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
        }

        public void BuildLoadout()
        {
            loadout.Clear();
            foreach (MedicationData m in controller.Config.genericMedications)
            {
                if (m != null && !loadout.Contains(m)) loadout.Add(m);
            }

            MedicationData regional = GameSession.CurrentSystem != null
                ? GameSession.CurrentSystem.regionalMedication
                : fallbackRegionalMedication;

            if (regional != null && !loadout.Contains(regional)) loadout.Add(regional);

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, loadout.Count - 1));
            NotifyChanged();
        }

        public void CycleMedication(int step)
        {
            if (loadout.Count == 0) return;
            selectedIndex = (selectedIndex + step + loadout.Count) % loadout.Count;
            NotifyChanged();
        }

        public void SelectMedication(int index)
        {
            if (index < 0 || index >= loadout.Count) return;
            selectedIndex = index;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (tintedArm != null && Current != null) tintedArm.color = Current.color;
            MedicationChanged?.Invoke(Current);
        }

        /// <summary>Recompensa del parry: el siguiente disparo sale al instante.</summary>
        public void ResetCooldown() => cooldown = 0f;

        public bool TryShoot()
        {
            if (cooldown > 0f || pool == null || Current == null) return false;
            PlayerConfig c = controller.Config;
            if (pool.CountActive >= c.maxActiveCapsules) return false;

            DepthBody body = controller.Body;
            Projectile capsule = pool.Get();
            capsule.Launch(new ProjectileShot
            {
                origin = body.GroundPosition + new Vector2(body.Facing * c.muzzleForward, 0f),
                height = body.Height + c.muzzleHeight,
                direction = body.Facing,
                speed = c.capsuleSpeed,
                range = c.capsuleRange,
                damage = c.capsuleDamage,
                knockback = c.capsuleKnockback,
                laneTolerance = c.capsuleLaneTolerance,
                halfWidth = 0.15f,
                sourceTeam = Team.Player,
                targetTeam = Team.Enemy,
                medication = Current,
                color = Current.color
            }, p => pool.Release(p));

            cooldown = c.fireInterval;
            Fired?.Invoke();
            return true;
        }
    }
}
