using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Todos los valores de balance de Healing Knight en un asset.
    /// Se ajustan desde el Inspector, incluso en Play Mode, sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Configuración del jugador", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento en el piso")]
        [Tooltip("Velocidad máxima horizontal (u/s).")]
        public float moveSpeed = 5.5f;
        [Tooltip("La profundidad se recorre más lento que lo horizontal, como en los beat 'em up.")]
        [Range(0.3f, 1f)] public float depthSpeedFactor = 0.65f;
        public float acceleration = 45f;
        public float deceleration = 60f;

        [Header("Salto (esquivar y atacar desde el aire)")]
        public float jumpHeight = 1.6f;
        public float timeToApex = 0.32f;
        [Tooltip("La caída es más rápida que la subida.")]
        public float fallGravityMultiplier = 1.5f;
        [Tooltip("Control horizontal en el aire (0 a 1).")]
        [Range(0f, 1f)] public float airControl = 0.8f;

        [Header("Salud")]
        public float maxHealth = 100f;
        public float invulnerabilityTime = 1.0f;
        public float hurtStunTime = 0.25f;
        public float knockbackDecay = 18f;

        [Header("Disparo de cápsulas")]
        [Tooltip("Segundos entre disparos al mantener el botón.")]
        public float fireInterval = 0.25f;
        public float capsuleSpeed = 12f;
        public float capsuleRange = 10f;
        public float capsuleDamage = 10f;
        public float capsuleKnockback = 1.5f;
        [Tooltip("Altura de la boca del cañón sobre los pies.")]
        public float muzzleHeight = 1.0f;
        public float muzzleForward = 0.7f;
        public int maxActiveCapsules = 6;
        [Tooltip("Qué tan lejos en profundidad puede estar el enemigo y aun así recibir la cápsula.")]
        public float capsuleLaneTolerance = 0.35f;

        [Header("Golpe cercano")]
        public float meleeDamage = 6f;
        public float meleeRange = 1.1f;
        public float meleeLaneTolerance = 0.45f;
        public float meleeWindup = 0.08f;
        public float meleeActiveTime = 0.1f;
        public float meleeCooldown = 0.4f;
        public float meleeKnockback = 4f;

        [Header("Escudo (GDD 5.8)")]
        public float shieldMaxEnergy = 100f;
        public float shieldProjectileCost = 20f;
        public float shieldContactCost = 30f;
        public float shieldRegenDelay = 1.5f;
        public float shieldRegenPerSecond = 15f;
        public float shieldBreakCooldown = 2.5f;
        [Range(0.1f, 1f)] public float shieldMoveSpeedMultiplier = 0.5f;

        [Header("Medicamentos genéricos (la cápsula regional la agrega el sistema)")]
        public List<MedicationData> genericMedications = new List<MedicationData>();
    }
}
