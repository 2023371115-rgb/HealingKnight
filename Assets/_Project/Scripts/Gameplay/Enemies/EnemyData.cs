using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    public enum EnemyMovement
    {
        /// <summary>Camina por el suelo y las plataformas (con gravedad).</summary>
        Ground,
        /// <summary>Flota en el aire (virus): no le afecta la gravedad.</summary>
        Flying
    }

    public enum EnemyAttackType
    {
        /// <summary>Se acerca y embiste (parásitos, bacterias).</summary>
        Melee,
        /// <summary>Guarda distancia y dispara por su franja (virus).</summary>
        Ranged
    }

    /// <summary>Valores de un tipo de enemigo. Un asset por enemigo: Parásito, Bacteria, Rotavirus...</summary>
    [CreateAssetMenu(menuName = "Healing Knight/Enemigo", fileName = "Enemigo")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "Parásito";

        [Tooltip("Define qué medicamento lo trata (tabla de efectividad).")]
        public ThreatType threat = ThreatType.Parasito;

        [Header("Vida")]
        public float maxHealth = 30f;
        [Tooltip("Multiplicador del daño del golpe cercano del jugador.")]
        public float meleeDamageTakenMultiplier = 1f;
        [Tooltip("0 = vuela con cualquier golpe, 1 = no se mueve.")]
        [Range(0f, 1f)] public float knockbackResistance = 0f;
        [Tooltip("Tiempo aturdido al recibir un golpe que sí hace daño.")]
        public float hurtStunTime = 0.2f;

        [Header("Movimiento")]
        public EnemyMovement movement = EnemyMovement.Ground;
        public float moveSpeed = 2.2f;
        [Tooltip("Solo voladores: altura sobre el jugador a la que intenta flotar.")]
        public float flyHeight = 2.2f;
        [Tooltip("A qué distancia empieza a perseguirte.")]
        public float aggroRange = 14f;

        [Header("Ataque")]
        public EnemyAttackType attackType = EnemyAttackType.Melee;
        [Tooltip("Cuerpo a cuerpo: distancia a la que embiste. A distancia: distancia que intenta mantener.")]
        public float attackDistance = 1.2f;
        [Tooltip("Aviso antes de atacar (se ilumina). Es lo que hace justo al combate.")]
        public float attackWindup = 0.45f;
        public float attackCooldown = 1.4f;
        public float attackDamage = 10f;
        public float attackKnockback = 4f;

        [Header("Solo a distancia")]
        public float projectileSpeed = 7f;
        public float projectileRange = 9f;
        public Color projectileColor = new Color(0.7f, 1f, 0.3f);

        [Header("Recompensas")]
        [Tooltip("Carga que da a Linfocito al ser derrotado cerca de él (para cuando se implemente).")]
        public int linfocitoCharge = 1;

        [Header("Placeholder visual")]
        public Color tint = Color.white;
        public float visualScale = 1f;
    }
}
