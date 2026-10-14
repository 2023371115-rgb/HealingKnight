using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    public enum SpawnSide { Right, Left, Both }

    [Serializable]
    public class SpawnEntry
    {
        public EnemyHealth prefab;
        public EnemyData data;
        [Min(1)] public int count = 1;
        public SpawnSide side = SpawnSide.Right;
    }

    [Serializable]
    public class Wave
    {
        public List<SpawnEntry> entries = new List<SpawnEntry>();
        [Tooltip("Pausa antes de que aparezca esta oleada.")]
        public float delayBefore = 0.6f;
    }

    /// <summary>
    /// Arena de combate: cuando el jugador llega a 'triggerX' la cámara se bloquea entre 'leftBound' y 'rightBound',
    /// aparecen las oleadas en orden y, al derrotarlas todas, se desbloquea ("¡AVANZA!").
    /// Igual que las pantallas que se cierran en Metal Slug.
    /// </summary>
    public class ArenaZone : MonoBehaviour
    {
        [Header("Zona")]
        [Tooltip("X a partir de la cual empieza la arena.")]
        public float triggerX = 0f;
        public float leftBound = -8f;
        public float rightBound = 8f;
        [Tooltip("Y del piso de la arena (donde caen los enemigos de suelo).")]
        public float floorY = 0f;
        [Tooltip("Los enemigos que entran por la derecha aparecen esta distancia más adentro " +
                 "(para que no caigan detrás de un bloqueo de mucosidad o inflamación).")]
        public float rightSpawnInset = 0f;

        [Header("Oleadas")]
        public List<Wave> waves = new List<Wave>();
        public EffectivenessTable effectivenessTable;

        [Header("Mensajes (opcional)")]
        [TextArea] public string startMessage;
        [TextArea] public string clearMessage = "¡AVANZA!";

        private enum Status { Waiting, Running, Cleared }
        private Status status = Status.Waiting;
        private int waveIndex = -1;
        private float waveTimer;
        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();

        public bool IsRunning => status == Status.Running;
        public bool IsCleared => status == Status.Cleared;
        public float RespawnX => leftBound + 1.5f;

        public event Action<ArenaZone> Cleared;

        private void Update()
        {
            PlayerController player = PlayerController.Current;

            if (status == Status.Waiting)
            {
                if (player != null && player.Position.x >= triggerX) Begin();
                return;
            }

            if (status != Status.Running) return;

            alive.RemoveAll(e => e == null || !e.IsAlive);
            if (alive.Count > 0) return;

            waveTimer -= Time.deltaTime;
            if (waveTimer > 0f) return;

            if (waveIndex + 1 < waves.Count) SpawnWave(waveIndex + 1);
            else Finish();
        }

        private void Begin()
        {
            status = Status.Running;
            waveIndex = -1;
            waveTimer = waves.Count > 0 ? waves[0].delayBefore : 0f;
            WalkableArea.Lock(leftBound, rightBound);
            GameplayEvents.RaiseArenaStarted(this);
            if (!string.IsNullOrEmpty(startMessage)) GameplayEvents.RaiseMessage(startMessage);
        }

        private void SpawnWave(int index)
        {
            waveIndex = index;
            Wave wave = waves[index];
            GameplayEvents.RaiseWaveStarted(this, index + 1, waves.Count);

            int n = 0;
            foreach (SpawnEntry entry in wave.entries)
            {
                if (entry == null || entry.prefab == null) continue;
                for (int i = 0; i < entry.count; i++, n++)
                {
                    bool fromRight = entry.side == SpawnSide.Right || (entry.side == SpawnSide.Both && n % 2 == 0);
                    float x = fromRight ? rightBound - 0.8f - rightSpawnInset - (i % 3) * 0.6f : leftBound + 0.8f + (i % 3) * 0.6f;
                    // Los de suelo caen desde arriba de la pantalla (como en Metal Slug); los voladores entran flotando.
                    // Los jefes (sin motor de física) salen directo del piso.
                    bool flying = entry.data != null && entry.data.movement == EnemyMovement.Flying;
                    bool walker = entry.prefab.GetComponent<CharacterMotor2D>() != null;
                    float y = flying ? floorY + entry.data.flyHeight + 1f + (i % 2) * 0.6f
                        : walker ? floorY + 3f + (i % 3) * 0.8f
                        : floorY;
                    Vector2 pos = new Vector2(x, y);

                    EnemyHealth enemy = Instantiate(entry.prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
                    if (entry.data != null)
                    {
                        enemy.Configure(entry.data, effectivenessTable);
                        EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
                        if (brain != null) brain.Configure(entry.data);
                    }
                    alive.Add(enemy);
                }
            }

            // La siguiente oleada espera su propio retraso cuando esta termine.
            waveTimer = index + 1 < waves.Count ? waves[index + 1].delayBefore : 0.3f;
        }

        private void Finish()
        {
            status = Status.Cleared;
            WalkableArea.Unlock();
            GameplayEvents.RaiseArenaCleared(this);
            if (!string.IsNullOrEmpty(clearMessage)) GameplayEvents.RaiseMessage(clearMessage);
            Cleared?.Invoke(this);
        }

        /// <summary>Al morir el jugador: se borran los enemigos y la arena vuelve a empezar.</summary>
        public void ResetArena()
        {
            foreach (EnemyHealth e in alive)
            {
                if (e != null) Destroy(e.gameObject);
            }
            alive.Clear();

            if (status == Status.Running)
            {
                status = Status.Waiting;
                WalkableArea.Unlock();
            }
        }

        private void OnDrawGizmos()
        {
            float yMin = floorY - 1f;
            float yMax = floorY + 4f;

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawLine(new Vector3(leftBound, yMin, 0f), new Vector3(leftBound, yMax + 4f, 0f));
            Gizmos.DrawLine(new Vector3(rightBound, yMin, 0f), new Vector3(rightBound, yMax + 4f, 0f));
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            Gizmos.DrawLine(new Vector3(triggerX, yMin, 0f), new Vector3(triggerX, yMax + 2f, 0f));
        }
    }
}
