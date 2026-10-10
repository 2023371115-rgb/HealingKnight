using System.Collections;
using System.Collections.Generic;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Dirige un nivel: límites del piso, aparición del jugador, arenas, muerte y reaparición, y la salida.
    /// Al completar el nivel lo marca en el guardado y regresa al mapa del cuerpo.
    /// Si la escena no está en el catálogo (escenas Sandbox), al terminar simplemente se reinicia.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Piso caminable (X = largo del nivel, Y = profundidad)")]
        [SerializeField] private Rect levelBounds = new Rect(-10f, -3.2f, 60f, 2.6f);

        [Header("Jugador")]
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private Vector2 spawnPoint = new Vector2(-7f, -1.9f);
        [SerializeField] private float respawnDelay = 1.5f;

        [Header("Salida")]
        [Tooltip("Al cruzar esta X (sin arenas pendientes) se completa el nivel.")]
        [SerializeField] private float exitX = 48f;
        [SerializeField] private float completeDelay = 2.5f;

        [Header("Mensajes")]
        [SerializeField, TextArea] private string introMessage;
        [SerializeField] private string completeMessage = "¡ZONA DESINFECTADA!";

        private readonly List<ArenaZone> arenas = new List<ArenaZone>();
        private PlayerController player;
        private float checkpointX;
        private bool completed;

        public static LevelManager Current { get; private set; }
        public Rect LevelBounds => levelBounds;
        public float ExitX => exitX;

        private void Awake()
        {
            Current = this;
            WalkableArea.SetLevelBounds(levelBounds);
            GameSession.RegisterActiveLevel(SceneManager.GetActiveScene().name);
            #pragma warning disable CS0618 // La API cambia entre versiones de Unity 6; esta sigue funcionando.
            arenas.AddRange(FindObjectsByType<ArenaZone>(FindObjectsSortMode.None));
#pragma warning restore CS0618
            arenas.Sort((a, b) => a.triggerX.CompareTo(b.triggerX));
            checkpointX = spawnPoint.x;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (player != null && player.Health != null) player.Health.Died -= OnPlayerDied;
            foreach (ArenaZone a in arenas)
                if (a != null) a.Cleared -= OnArenaCleared;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null && playerPrefab != null)
                player = Instantiate(playerPrefab, new Vector3(spawnPoint.x, spawnPoint.y, 0f), Quaternion.identity);

            if (player == null)
            {
                Debug.LogError("[LevelManager] No hay jugador en la escena ni prefab asignado.", this);
                return;
            }

            player.Body.Teleport(spawnPoint);
            player.Health.Died += OnPlayerDied;
            foreach (ArenaZone a in arenas) a.Cleared += OnArenaCleared;

            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        private void Update()
        {
            if (completed || player == null || !player.Health.IsAlive) return;
            if (player.Body.GroundPosition.x < exitX) return;

            foreach (ArenaZone a in arenas)
            {
                if (a.triggerX < exitX && !a.IsCleared) return; // aún quedan arenas por limpiar
            }

            StartCoroutine(CompleteLevel());
        }

        private void OnArenaCleared(ArenaZone arena)
        {
            checkpointX = Mathf.Max(checkpointX, arena.rightBound - 2f);
        }

        private void OnPlayerDied()
        {
            GameplayEvents.RaisePlayerDefeated();
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            float respawnX = checkpointX;
            foreach (ArenaZone a in arenas)
            {
                if (a.IsRunning)
                {
                    respawnX = a.RespawnX;
                    a.ResetArena();
                }
            }

            float depth = (levelBounds.yMin + levelBounds.yMax) * 0.5f;
            // Reaparece un poco antes de la arena para que esta vuelva a activarse al avanzar.
            player.Respawn(new Vector2(respawnX - 1f, depth));
        }

        private IEnumerator CompleteLevel()
        {
            completed = true;
            player.Input.Blocked = true;
            player.Body.SetGroundVelocity(Vector2.zero);
            GameplayEvents.RaiseLevelCompleted();
            if (!string.IsNullOrEmpty(completeMessage)) GameplayEvents.RaiseMessage(completeMessage);

            yield return new WaitForSeconds(completeDelay);

            if (GameSession.CurrentLevel != null)
            {
                GameSession.CompleteCurrentLevel();
                GameSession.GoToBodyMap();
            }
            else
            {
                GameSession.ReloadCurrentScene();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            Vector3 c = new Vector3(levelBounds.center.x, levelBounds.center.y, 0f);
            Gizmos.DrawWireCube(c, new Vector3(levelBounds.width, levelBounds.height, 0f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(spawnPoint.x, spawnPoint.y, 0f), 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(exitX, levelBounds.yMin, 0f), new Vector3(exitX, levelBounds.yMax + 3f, 0f));
        }
    }
}
