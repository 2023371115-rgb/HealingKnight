using System;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Bombas de medicamento (las granadas de Metal Slug): cantidad limitada por nivel,
    /// se lanzan en arco hacia donde miras y liberan el medicamento seleccionado en un área.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBombs : MonoBehaviour
    {
        [SerializeField] private MedicineBomb bombPrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        private PlayerController controller;
        private ComponentPool<MedicineBomb> pool;
        private int count;
        private float cooldown;

        public int Count => count;
        public event Action<int> CountChanged;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            Refill();
            if (bombPrefab != null) pool = new ComponentPool<MedicineBomb>(bombPrefab, 3, 6);
        }

        private void OnDestroy() => pool?.Clear();

        private void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
        }

        public void Refill()
        {
            count = controller.Config.bombsPerLevel;
            CountChanged?.Invoke(count);
        }

        public void Add(int amount)
        {
            count += amount;
            CountChanged?.Invoke(count);
        }

        public bool TryThrow()
        {
            if (count <= 0 || cooldown > 0f || pool == null) return false;
            PlayerConfig c = controller.Config;
            int facing = controller.Motor.Facing;

            MedicineBomb bomb = pool.Get();
            Vector2 origin = controller.Motor.Position + new Vector2(facing * 0.4f, 1.3f);
            Vector2 velocity = new Vector2(facing * c.bombThrowVelocity.x + controller.Motor.Velocity.x * 0.5f, c.bombThrowVelocity.y);
            bomb.Launch(origin, velocity, c.bombDamage, c.bombBlastRadius, controller.Shooter != null ? controller.Shooter.Current : null,
                groundMask, b => pool.Release(b));

            count--;
            cooldown = c.bombCooldown;
            CountChanged?.Invoke(count);
            return true;
        }
    }
}
