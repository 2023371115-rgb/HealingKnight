using System;
using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Golpe cercano con el brazo-pistola. Sin combos: un golpe, una pausa.
    /// Hace daño genérico (no usa la tabla de efectividad) y sobre todo empuja:
    /// sirve para quitarte enemigos de encima, no para sustituir el tratamiento correcto.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMelee : MonoBehaviour
    {
        private enum Phase { Idle, Windup, Active, Recovery }

        private PlayerController controller;
        private Phase phase = Phase.Idle;
        private float timer;
        private float cooldown;
        private readonly List<Hurtbox> hits = new List<Hurtbox>();
        private readonly HashSet<Hurtbox> alreadyHit = new HashSet<Hurtbox>();

        public bool IsAttacking => phase != Phase.Idle;
        public event Action Swung;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (cooldown > 0f) cooldown -= dt;
            if (phase == Phase.Idle) return;

            PlayerConfig c = controller.Config;
            timer -= dt;

            switch (phase)
            {
                case Phase.Windup:
                    if (timer <= 0f)
                    {
                        phase = Phase.Active;
                        timer = c.meleeActiveTime;
                        alreadyHit.Clear();
                        Swung?.Invoke();
                    }
                    break;

                case Phase.Active:
                    DoHits(c);
                    if (timer <= 0f)
                    {
                        phase = Phase.Recovery;
                        timer = Mathf.Max(0f, c.meleeCooldown - c.meleeWindup - c.meleeActiveTime);
                    }
                    break;

                case Phase.Recovery:
                    if (timer <= 0f) phase = Phase.Idle;
                    break;
            }
        }

        public bool TryAttack()
        {
            if (phase != Phase.Idle || cooldown > 0f) return false;
            PlayerConfig c = controller.Config;
            phase = Phase.Windup;
            timer = c.meleeWindup;
            cooldown = c.meleeCooldown;
            return true;
        }

        public void Cancel()
        {
            phase = Phase.Idle;
            timer = 0f;
        }

        private void DoHits(PlayerConfig c)
        {
            DepthBody body = controller.Body;
            Vector2 center = body.GroundPosition + new Vector2(body.Facing * c.meleeRange * 0.5f, 0f);
            float height = body.Height + 0.8f;

            Hurtbox.Query(Team.Enemy, center, c.meleeRange * 0.5f, height, c.meleeLaneTolerance, hits);
            foreach (Hurtbox h in hits)
            {
                if (!alreadyHit.Add(h)) continue;

                DamageInfo info = DamageInfo.Create(c.meleeDamage, DamageKind.Melee, Team.Player, body.GroundPosition);
                info.knockback = c.meleeKnockback;
                h.Damageable.ApplyDamage(info);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null || controller.Config == null || controller.Body == null) return;
            PlayerConfig c = controller.Config;
            DepthBody body = controller.Body;
            Vector3 center = (Vector3)body.GroundPosition + new Vector3(body.Facing * c.meleeRange * 0.5f, 0.8f, 0f);
            Gizmos.color = phase == Phase.Active ? Color.red : new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireCube(center, new Vector3(c.meleeRange, 0.6f, 0f));
        }
    }
}
