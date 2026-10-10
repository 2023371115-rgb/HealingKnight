using System.Collections.Generic;
using System.IO;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Crea las escenas base: menú, mapa del cuerpo, los 3 niveles del Sistema Digestivo en gris y un Sandbox.
    /// Los niveles son de prueba: muévelos, cambia oleadas y pon tu arte encima.
    /// </summary>
    internal class SceneSetup
    {
        public const string Folder = "Assets/_Project/Scenes";

        private readonly DataSetup data;
        private readonly PrefabSetup prefabs;
        private readonly List<string> created = new List<string>();

        public SceneSetup(DataSetup data, PrefabSetup prefabs)
        {
            this.data = data;
            this.prefabs = prefabs;
        }

        public IReadOnlyList<string> Created => created;

        public static string MainMenuPath => $"{Folder}/MainMenu.unity";
        public static string BodyMapPath => $"{Folder}/BodyMap.unity";
        public static string BocaPath => $"{Folder}/Levels/Digestivo/Digestivo_Boca.unity";
        public static string EstomagoPath => $"{Folder}/Levels/Digestivo/Digestivo_Estomago.unity";
        public static string IntestinoPath => $"{Folder}/Levels/Digestivo/Digestivo_Intestino.unity";
        public static string SandboxPath => $"{Folder}/Sandbox/Sandbox.unity";

        public void Run()
        {
            CreateIfMissing(MainMenuPath, () => BuildMenuScene<MainMenuScreen>("MainMenu", new Color(0.35f, 0.08f, 0.14f)));
            CreateIfMissing(BodyMapPath, () => BuildMenuScene<BodyMapScreen>("BodyMap", new Color(0.12f, 0.06f, 0.1f)));
            CreateIfMissing(BocaPath, BuildBoca);
            CreateIfMissing(EstomagoPath, BuildEstomago);
            CreateIfMissing(IntestinoPath, BuildIntestino);
            CreateIfMissing(SandboxPath, BuildSandbox);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(BodyMapPath, true),
                new EditorBuildSettingsScene(BocaPath, true),
                new EditorBuildSettingsScene(EstomagoPath, true),
                new EditorBuildSettingsScene(IntestinoPath, true),
                new EditorBuildSettingsScene(SandboxPath, true),
            };
        }

        private void CreateIfMissing(string path, System.Action build)
        {
            if (File.Exists(path)) return;
            SetupUtils.EnsureFolder(Path.GetDirectoryName(path));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build();
            EditorSceneManager.SaveScene(scene, path);
            created.Add(path);
        }

        // ---------- Menús ----------

        private static void BuildMenuScene<T>(string name, Color background) where T : Component
        {
            CreateCamera(background, false);
            new GameObject(name, typeof(T));
        }

        // ---------- Piezas comunes de nivel ----------

        private static Camera CreateCamera(Color background, bool gameplay)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 1.6f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<AudioListener>();
            if (gameplay) go.AddComponent<GameCamera>();

            AddGlobalLight2D();
            return cam;
        }

        /// <summary>
        /// Agrega una luz global 2D (URP). Se busca por nombre porque, según la versión de Unity,
        /// Light2D vive en un assembly distinto; así no dependemos de cuál sea.
        /// </summary>
        private static void AddGlobalLight2D()
        {
            System.Type lightType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                lightType = assembly.GetType("UnityEngine.Rendering.Universal.Light2D");
                if (lightType != null) break;
            }
            if (lightType == null) return;

            var go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(lightType);

            var typeProp = lightType.GetProperty("lightType");
            if (typeProp != null && typeProp.CanWrite)
                typeProp.SetValue(light, System.Enum.Parse(typeProp.PropertyType, "Global"));

            var intensityProp = lightType.GetProperty("intensity");
            if (intensityProp != null && intensityProp.CanWrite) intensityProp.SetValue(light, 1f);
        }

        private struct LevelStyle
        {
            public Color background, wall, floor, floorEdge, blob;
        }

        private LevelManager BuildLevelBase(float length, LevelStyle style, string intro)
        {
            CreateCamera(style.background, true);

            const float floorMinY = -3.4f;
            const float floorMaxY = -0.6f;
            float startX = -10f;

            // Escenario placeholder: pared del fondo, piso y unas manchas orgánicas.
            var env = new GameObject("Environment").transform;
            SetupUtils.SpriteChild(env, "BackWall", PlaceholderArt.Square, style.wall,
                new Vector3(startX + length * 0.5f, floorMaxY, 0f), new Vector2(length + 30f, 9f), -200);
            SetupUtils.SpriteChild(env, "Floor", PlaceholderArt.Square, style.floor,
                new Vector3(startX + length * 0.5f, floorMinY - 1.2f, 0f), new Vector2(length + 30f, floorMaxY - floorMinY + 1.2f), -100);
            SetupUtils.SpriteChild(env, "FloorEdge", PlaceholderArt.Square, style.floorEdge,
                new Vector3(startX + length * 0.5f, floorMaxY - 0.1f, 0f), new Vector2(length + 30f, 0.2f), -99);

            var random = new System.Random(length.GetHashCode());
            for (float x = startX - 5f; x < startX + length + 10f; x += 4f + (float)random.NextDouble() * 3f)
            {
                float size = 1f + (float)random.NextDouble() * 2.5f;
                float y = 1.5f + (float)random.NextDouble() * 4f;
                SetupUtils.SpriteChild(env, "Blob", PlaceholderArt.Circle, style.blob,
                    new Vector3(x, y, 0f), new Vector2(size, size * (0.7f + (float)random.NextDouble() * 0.6f)), -150);
            }

            var managerGo = new GameObject("LevelManager");
            var manager = managerGo.AddComponent<LevelManager>();
            SetupUtils.SetRect(manager, "levelBounds", new Rect(startX, floorMinY, length, floorMaxY - floorMinY));
            SetupUtils.SetVector2(manager, "spawnPoint", new Vector2(startX + 3f, (floorMinY + floorMaxY) * 0.5f));
            SetupUtils.SetFloat(manager, "exitX", startX + length - 4f);
            SetupUtils.SetRef(manager, "playerPrefab", prefabs.Player);
            SetupUtils.SetString(manager, "introMessage", intro);

            // El jugador se coloca en la escena para poder verlo y probar; el LevelManager lo usa.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player.gameObject);
            player.transform.position = new Vector3(startX + 3f, (floorMinY + floorMaxY) * 0.5f, 0f);

            new GameObject("HUD", typeof(HUD));
            new GameObject("PauseMenu", typeof(PauseMenu));

            return manager;
        }

        private ArenaZone Arena(string name, float triggerX, float width, string startMessage, params Wave[] waves)
        {
            var arena = new GameObject(name).AddComponent<ArenaZone>();
            arena.transform.position = new Vector3(triggerX, 0f, 0f);
            arena.triggerX = triggerX;
            arena.leftBound = triggerX - 3f;
            arena.rightBound = triggerX - 3f + width;
            arena.waves = new List<Wave>(waves);
            arena.effectivenessTable = data.Table;
            arena.startMessage = startMessage;
            return arena;
        }

        private static Wave W(float delay, params SpawnEntry[] entries)
        {
            return new Wave { delayBefore = delay, entries = new List<SpawnEntry>(entries) };
        }

        private static SpawnEntry E(EnemyHealth prefab, EnemyData enemyData, int count, SpawnSide side = SpawnSide.Right)
        {
            return new SpawnEntry { prefab = prefab, data = enemyData, count = count, side = side };
        }

        private void Acid(float x, float y)
        {
            var acid = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Acid.gameObject);
            acid.transform.position = new Vector3(x, y, 0f);
        }

        // ---------- Niveles del Sistema Digestivo ----------

        private void BuildBoca()
        {
            BuildLevelBase(70f, new LevelStyle
            {
                background = new Color(0.25f, 0.06f, 0.1f),
                wall = new Color(0.55f, 0.2f, 0.28f),
                floor = new Color(0.78f, 0.38f, 0.45f),
                floorEdge = new Color(0.9f, 0.55f, 0.6f),
                blob = new Color(0.7f, 0.3f, 0.38f, 0.6f)
            }, "NODO: Entrada completada. Muévete con WASD y salta con Espacio.");

            Arena("Arena_1_Parasitos", 8f, 18f,
                "Parásitos. Dispara con J. Cambia de cápsula con Q/E: prueba cuál funciona.",
                W(0.8f, E(prefabs.Parasito, data.Parasito, 2)));

            Arena("Arena_2_Mixta", 32f, 18f,
                "Bacterias: usa Antibiótico. Escudo con K, golpe cercano con L.",
                W(0.6f, E(prefabs.Bacteria, data.Bacteria, 2), E(prefabs.Parasito, data.Parasito, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.Parasito, data.Parasito, 2, SpawnSide.Both)));
        }

        private void BuildEstomago()
        {
            BuildLevelBase(90f, new LevelStyle
            {
                background = new Color(0.22f, 0.08f, 0.04f),
                wall = new Color(0.6f, 0.28f, 0.2f),
                floor = new Color(0.82f, 0.48f, 0.36f),
                floorEdge = new Color(0.95f, 0.75f, 0.35f),
                blob = new Color(0.9f, 0.8f, 0.3f, 0.35f)
            }, "NODO: Ácido gástrico. Rodéalo o sáltalo: el escudo no te protege.");

            Acid(4f, -2.6f);
            Acid(14f, -1.4f);

            Arena("Arena_1_HPylori", 22f, 18f, "H. pylori: bacterias que resisten el ácido.",
                W(0.6f, E(prefabs.Bacteria, data.Bacteria, 3)),
                W(1.0f, E(prefabs.Bacteria, data.Bacteria, 2), E(prefabs.Parasito, data.Parasito, 1, SpawnSide.Left)));

            Acid(44f, -2.2f);

            Arena("Arena_2_Mixta", 52f, 20f, null,
                W(0.6f, E(prefabs.Parasito, data.Parasito, 2), E(prefabs.Bacteria, data.Bacteria, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.ParasitoGrande, data.ParasitoGrande, 1)));
        }

        private void BuildIntestino()
        {
            BuildLevelBase(80f, new LevelStyle
            {
                background = new Color(0.18f, 0.05f, 0.12f),
                wall = new Color(0.5f, 0.22f, 0.4f),
                floor = new Color(0.72f, 0.4f, 0.55f),
                floorEdge = new Color(0.85f, 0.55f, 0.7f),
                blob = new Color(0.65f, 0.35f, 0.6f, 0.5f)
            }, "NODO: Intestino. Un virus dispara desde lejos: usa Antiviral y el escudo.");

            Arena("Arena_1_Rotavirus", 10f, 18f, "Rotavirus.",
                W(0.6f, E(prefabs.Rotavirus, data.Rotavirus, 2)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Bacteria, data.Bacteria, 2, SpawnSide.Left)));

            // Arena final: placeholder del jefe lombriz (por ahora, oleadas fuertes).
            Arena("Arena_2_Final", 38f, 22f, "Energía biológica elevada... (jefe lombriz: próximamente)",
                W(0.8f, E(prefabs.ParasitoGrande, data.ParasitoGrande, 1), E(prefabs.Parasito, data.Parasito, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 2), E(prefabs.ParasitoGrande, data.ParasitoGrande, 1, SpawnSide.Left)));
        }

        private void BuildSandbox()
        {
            BuildLevelBase(60f, new LevelStyle
            {
                background = new Color(0.1f, 0.1f, 0.12f),
                wall = new Color(0.3f, 0.3f, 0.34f),
                floor = new Color(0.45f, 0.45f, 0.5f),
                floorEdge = new Color(0.6f, 0.6f, 0.65f),
                blob = new Color(0.4f, 0.4f, 0.45f, 0.5f)
            }, "SANDBOX: escena libre para probar mecánicas. Duplícala para tus pruebas.");

            Acid(2f, -2f);
            Arena("Arena_Pruebas", 10f, 20f, "Todos los enemigos",
                W(0.5f, E(prefabs.Parasito, data.Parasito, 1), E(prefabs.Bacteria, data.Bacteria, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.ParasitoGrande, data.ParasitoGrande, 1, SpawnSide.Left)));
        }
    }
}
