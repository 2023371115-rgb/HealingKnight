using System.Collections.Generic;
using System.IO;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
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
        public static string EstomagoPath => $"{Folder}/Levels/Digestivo/Digestivo_Estomago.unity";
        public static string DelgadoPath => $"{Folder}/Levels/Digestivo/Digestivo_IntestinoDelgado.unity";
        public static string GruesoPath => $"{Folder}/Levels/Digestivo/Digestivo_IntestinoGrueso.unity";
        public static string BocaPath => $"{Folder}/Levels/Digestivo/Digestivo_Boca.unity";
        public static string SandboxPath => $"{Folder}/Sandbox/Sandbox.unity";
        public static string NarizPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Nariz.unity";
        public static string TraqueaPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Traquea.unity";
        public static string PulmonesPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Pulmones.unity";

        public void Run()
        {
            CreateIfMissing(MainMenuPath, () => BuildMenuScene<MainMenuScreen>("MainMenu", new Color(0.35f, 0.08f, 0.14f)));
            CreateIfMissing(BodyMapPath, () => BuildMenuScene<BodyMapScreen>("BodyMap", new Color(0.12f, 0.06f, 0.1f)));
            CreateIfMissing(EstomagoPath, BuildEstomago);
            CreateIfMissing(DelgadoPath, BuildDelgado);
            CreateIfMissing(GruesoPath, BuildGrueso);
            CreateIfMissing(BocaPath, BuildBoca);
            CreateIfMissing(NarizPath, BuildNariz);
            CreateIfMissing(TraqueaPath, BuildTraquea);
            CreateIfMissing(PulmonesPath, BuildPulmones);
            CreateIfMissing(SandboxPath, BuildSandbox);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(BodyMapPath, true),
                new EditorBuildSettingsScene(EstomagoPath, true),
                new EditorBuildSettingsScene(DelgadoPath, true),
                new EditorBuildSettingsScene(GruesoPath, true),
                new EditorBuildSettingsScene(BocaPath, true),
                new EditorBuildSettingsScene(NarizPath, true),
                new EditorBuildSettingsScene(TraqueaPath, true),
                new EditorBuildSettingsScene(PulmonesPath, true),
                new EditorBuildSettingsScene(SandboxPath, true),
            };
        }

        private void CreateIfMissing(string path, System.Action build)
        {
            if (File.Exists(path)) return;
            SetupUtils.EnsureFolder(Path.GetDirectoryName(path));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Al abrir una escena nueva Unity descarga los assets que no se están usando, y las
            // referencias que teníamos a prefabs y datos quedan "destruidas". Se vuelven a cargar
            // del disco (no se crea nada nuevo: ya existen).
            data.Run();
            prefabs.Run();

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
            go.transform.position = new Vector3(0f, 2.5f, -10f);
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

        private const float FloorY = 0f;   // la parte de arriba del piso
        private LevelStyle currentStyle;
        private Transform environment;

        /// <summary>
        /// Nivel base en vista lateral: pared de fondo, piso sólido (capa Ground) con huecos opcionales,
        /// jugador, HUD y LevelManager. 'pits' = huecos en el piso (x = inicio, y = ancho): caer cuesta vida.
        /// </summary>
        private LevelManager BuildLevelBase(float length, LevelStyle style, string intro, params Vector2[] pits)
        {
            CreateCamera(style.background, true);
            currentStyle = style;

            float startX = -10f;
            float endX = startX + length;

            environment = new GameObject("Environment").transform;
            SetupUtils.SpriteChild(environment, "BackWall", PlaceholderArt.Square, style.wall,
                new Vector3(startX + length * 0.5f, FloorY, 0f), new Vector2(length + 30f, 10f), -200);

            // Piso por tramos (los huecos quedan entre tramos).
            var sorted = new List<Vector2>(pits ?? new Vector2[0]);
            sorted.Sort((a, b) => a.x.CompareTo(b.x));
            float segmentStart = startX - 15f;
            int index = 0;
            foreach (Vector2 pit in sorted)
            {
                FloorSegment($"Floor_{index++}", segmentStart, pit.x);
                segmentStart = pit.x + pit.y;
            }
            FloorSegment($"Floor_{index}", segmentStart, endX + 15f);

            var random = new System.Random(length.GetHashCode());
            for (float x = startX - 5f; x < endX + 10f; x += 4f + (float)random.NextDouble() * 3f)
            {
                float size = 1f + (float)random.NextDouble() * 2.5f;
                float y = FloorY + 2.5f + (float)random.NextDouble() * 5f;
                SetupUtils.SpriteChild(environment, "Blob", PlaceholderArt.Circle, style.blob,
                    new Vector3(x, y, 0f), new Vector2(size, size * (0.7f + (float)random.NextDouble() * 0.6f)), -150);
            }

            Vector2 spawn = new Vector2(startX + 3f, FloorY + 0.05f);

            var managerGo = new GameObject("LevelManager");
            var manager = managerGo.AddComponent<LevelManager>();
            // yMin = altura de caída al vacío.
            SetupUtils.SetRect(manager, "levelBounds", new Rect(startX, FloorY - 6f, length, 20f));
            SetupUtils.SetVector2(manager, "spawnPoint", spawn);
            SetupUtils.SetFloat(manager, "exitX", endX - 4f);
            SetupUtils.SetRef(manager, "playerPrefab", prefabs.Player);
            SetupUtils.SetString(manager, "introMessage", intro);

            // El jugador se coloca en la escena para poder verlo y probar; el LevelManager lo usa.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player.gameObject);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);

            new GameObject("HUD", typeof(HUD));
            new GameObject("PauseMenu", typeof(PauseMenu));

            return manager;
        }

        private void FloorSegment(string name, float fromX, float toX)
        {
            float width = toX - fromX;
            if (width <= 0.01f) return;
            const float depth = 4f;

            var go = new GameObject(name);
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX + width * 0.5f, FloorY, 0f);
            go.layer = PhysicsLayers.Ground;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, depth);
            box.offset = new Vector2(0f, -depth * 0.5f);

            SetupUtils.SpriteChild(go.transform, "Fill", PlaceholderArt.Square, currentStyle.floor,
                new Vector3(0f, -depth, 0f), new Vector2(width, depth), -100);
            SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                new Vector3(0f, -0.2f, 0f), new Vector2(width, 0.2f), -99);
        }

        /// <summary>Plataforma de un solo sentido: se atraviesa saltando desde abajo y se pisa desde arriba.</summary>
        private void Platform(float centerX, float topY, float width)
        {
            const float thickness = 0.35f;
            var go = new GameObject("Platform");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(centerX, topY, 0f);
            go.layer = PhysicsLayers.Ground;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, thickness);
            box.offset = new Vector2(0f, -thickness * 0.5f);
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            SetupUtils.SpriteChild(go.transform, "Sprite", PlaceholderArt.Square, Color.Lerp(currentStyle.floor, Color.black, 0.1f),
                new Vector3(0f, -thickness, 0f), new Vector2(width, thickness), -90);
            SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                new Vector3(0f, -0.12f, 0f), new Vector2(width, 0.12f), -89);
        }

        private ArenaZone Arena(string name, float triggerX, float width, string startMessage, params Wave[] waves)
        {
            var arena = new GameObject(name).AddComponent<ArenaZone>();
            arena.transform.position = new Vector3(triggerX, FloorY, 0f);
            arena.floorY = FloorY;
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

        private void Acid(float x)
        {
            var acid = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Acid.gameObject);
            acid.transform.position = new Vector3(x, FloorY, 0f);
        }

        // ---------- Niveles del Sistema Digestivo ----------

        /// <summary>
        /// Bloqueo tratable de los bronquiolos (GDD 5.12): mucosidad (Mucolítico) o inflamación (Antiinflamatorio).
        /// Es una pared más alta que cualquier salto: hay que disolverla para seguir.
        /// </summary>
        private void Block(float x, bool mucus)
        {
            EnemyHealth prefab = mucus ? prefabs.Mucosidad : prefabs.Inflamacion;
            var block = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            block.transform.position = new Vector3(x, FloorY, 0f);
        }

        /// <summary>Arena que termina en un bloqueo: los enemigos de la derecha aparecen antes de la pared.</summary>
        private ArenaZone BlockedArena(string name, float triggerX, float width, string startMessage, bool mucus, params Wave[] waves)
        {
            ArenaZone arena = Arena(name, triggerX, width, startMessage, waves);
            arena.rightSpawnInset = 2.2f;
            Block(arena.rightBound - 1.2f, mucus);
            return arena;
        }

        /// <summary>Pasaje secreto; 'groundY' = piso o plataforma donde está.</summary>
        private void Secret(float x, float groundY)
        {
            var secret = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Secret.gameObject);
            secret.transform.position = new Vector3(x, groundY + 0.7f, 0f);
        }

        private void BuildEstomago()
        {
            BuildLevelBase(100f, new LevelStyle
            {
                background = new Color(0.22f, 0.08f, 0.04f),
                wall = new Color(0.6f, 0.28f, 0.2f),
                floor = new Color(0.82f, 0.48f, 0.36f),
                floorEdge = new Color(0.95f, 0.75f, 0.35f),
                blob = new Color(0.9f, 0.8f, 0.3f, 0.35f)
            }, "NODO: Estás en el estómago. Muévete con A/D, salta con K o Espacio. Apunta con W (W + A/D = diagonal).",
                new Vector2(31f, 2.5f), new Vector2(65.5f, 2.5f));

            // Plataformas para practicar el salto y el disparo hacia abajo.
            Platform(0f, 2.2f, 3f);
            Platform(4.5f, 3.6f, 2.5f);

            // Tutorial: un enemigo a la vez.
            Arena("Arena_1_Tutorial", 8f, 18f,
                "H. pylori: una bacteria. Dispara con J (de cerca acuchilla solo). Cambia de cápsula con Q/E hasta encontrar la que funciona.",
                W(0.8f, E(prefabs.HPylori, data.HPylori, 1)),
                W(0.8f, E(prefabs.HPylori, data.HPylori, 2)));

            Acid(27.5f);

            // Pasaje secreto en la plataforma más alta, entre la primera y la segunda arena (desbloquea la Boca).
            Platform(35.5f, 2.2f, 3f);
            Platform(39f, 4.2f, 2.5f);
            Secret(39.3f, 4.2f);

            Arena("Arena_2_Escudo", 44f, 18f,
                "Escudo: mantén Shift (o I). Si lo levantas justo antes del golpe haces PARRY. Bomba de medicamento: L.",
                W(0.6f, E(prefabs.HPylori, data.HPylori, 2), E(prefabs.HPylori, data.HPylori, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.HPylori, data.HPylori, 3, SpawnSide.Both)));
            Platform(50f, 2.4f, 4f);

            Acid(62f);

            Arena("Arena_3_Acido", 72f, 20f, "Ácido gástrico: sáltalo. El escudo no te protege.",
                W(0.6f, E(prefabs.HPylori, data.HPylori, 3)),
                W(1.0f, E(prefabs.HPylori, data.HPylori, 2), E(prefabs.HPylori, data.HPylori, 2, SpawnSide.Left)));
            Acid(80f);
            Platform(75f, 2.2f, 3f);
            Platform(85f, 2.2f, 3f);
        }

        private void BuildDelgado()
        {
            BuildLevelBase(95f, new LevelStyle
            {
                background = new Color(0.2f, 0.06f, 0.1f),
                wall = new Color(0.58f, 0.25f, 0.35f),
                floor = new Color(0.8f, 0.45f, 0.5f),
                floorEdge = new Color(0.92f, 0.62f, 0.65f),
                blob = new Color(0.75f, 0.4f, 0.5f, 0.5f)
            }, "NODO: Intestino delgado. Aquí viven los parásitos: usa la cápsula Antiparasitario.",
                new Vector2(28f, 2.5f), new Vector2(55f, 3f));

            Platform(3f, 2.2f, 3f);

            Arena("Arena_1_Giardia", 10f, 18f, "Giardia: parásito. Antiparasitario. Agáchate (S) para darle a los bajitos.",
                W(0.6f, E(prefabs.Giardia, data.Giardia, 2)),
                W(1.0f, E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Both)));
            Platform(18f, 2.4f, 3.5f);

            Arena("Arena_2_Rotavirus", 36f, 18f, "Rotavirus: vuela y dispara. Apunta arriba (W) o en diagonal. Antiviral.",
                W(0.6f, E(prefabs.Rotavirus, data.Rotavirus, 2)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Left)));
            Platform(40f, 2.2f, 3f);
            Platform(47f, 3.4f, 3f);

            // La lombriz aparece, pelea un rato y huye al intestino grueso. Arena sin plataformas.
            Arena("Arena_3_Lombriz", 64f, 22f, null,
                W(1.0f, E(prefabs.LombrizDelgado, data.Lombriz, 1)));
        }

        private void BuildGrueso()
        {
            BuildLevelBase(95f, new LevelStyle
            {
                background = new Color(0.16f, 0.07f, 0.06f),
                wall = new Color(0.5f, 0.3f, 0.25f),
                floor = new Color(0.68f, 0.45f, 0.38f),
                floorEdge = new Color(0.8f, 0.58f, 0.48f),
                blob = new Color(0.55f, 0.35f, 0.3f, 0.5f)
            }, "NODO: Intestino grueso. La lombriz está cerca.",
                new Vector2(29f, 3f));

            Arena("Arena_1_Amebas", 10f, 18f, "Entamoeba: una ameba lenta y resistente. Las bombas (L) ayudan.",
                W(0.6f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Both)));
            Platform(14f, 2.2f, 3f);

            Platform(31f, 2.4f, 2.5f);

            Arena("Arena_2_CDifficile", 36f, 18f, "C. difficile: bacteria. Antibiótico.",
                W(0.6f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Oxiuro, data.Oxiuro, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)));
            Platform(42f, 2.2f, 3f);
            Platform(48f, 2.2f, 3f);

            // Pelea final: desde la fase 2 invoca oxiuros. Arena sin plataformas.
            Arena("Arena_3_LombrizFinal", 64f, 22f, null,
                W(1.0f, E(prefabs.LombrizGrueso, data.Lombriz, 1)));
        }

        private void BuildBoca()
        {
            BuildLevelBase(70f, new LevelStyle
            {
                background = new Color(0.25f, 0.06f, 0.1f),
                wall = new Color(0.55f, 0.2f, 0.28f),
                floor = new Color(0.78f, 0.38f, 0.45f),
                floorEdge = new Color(0.95f, 0.92f, 0.85f),
                blob = new Color(0.95f, 0.92f, 0.85f, 0.5f)
            }, "NIVEL SECRETO · BOCA. NODO: Placa bacteriana detectada.");

            // Dientes: plataformas blancas.
            Arena("Arena_1_Caries", 8f, 18f, "S. mutans: la bacteria de la caries. Antibiótico.",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 3)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 2), E(prefabs.SMutans, data.SMutans, 2, SpawnSide.Left)));
            Platform(12f, 2.2f, 2f);
            Platform(18f, 2.2f, 2f);

            Arena("Arena_2_Placa", 34f, 20f, "La placa: muchas a la vez. (Mini jefe 'caries': próximamente)",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 3, SpawnSide.Both)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 4, SpawnSide.Both)));
            Platform(38f, 2.2f, 2f);
            Platform(44f, 3.4f, 2f);
            Platform(50f, 2.2f, 2f);
        }

        // ---------- Niveles del Sistema Respiratorio (GDD: avance por bronquiolos, mucosidad e inflamación) ----------

        private void BuildNariz()
        {
            BuildLevelBase(85f, new LevelStyle
            {
                background = new Color(0.22f, 0.07f, 0.1f),
                wall = new Color(0.78f, 0.45f, 0.5f),
                floor = new Color(0.92f, 0.62f, 0.64f),
                floorEdge = new Color(1f, 0.85f, 0.85f),
                blob = new Color(1f, 0.92f, 0.92f, 0.35f)
            }, "NODO: Sistema respiratorio. Entraste por la nariz. Tu cápsula regional ahora es el MUCOLÍTICO.",
                new Vector2(33f, 2.5f));

            Platform(1f, 2.2f, 3f);

            // Primer contacto: virus del resfriado y polen.
            Arena("Arena_1_Resfriado", 8f, 18f,
                "Rinovirus: virus del resfriado, flota y dispara. Antiviral. El polen es un alérgeno: Antihistamínico.",
                W(0.8f, E(prefabs.Rinovirus, data.Rinovirus, 1), E(prefabs.Polen, data.Polen, 2)),
                W(1.0f, E(prefabs.Rinovirus, data.Rinovirus, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Left)));
            Platform(12f, 2.4f, 3f);
            Platform(19f, 2.4f, 3f);

            // Tutorial de la mucosidad, sin enemigos: aprendes a disolverla con calma.
            Platform(34.2f, 2.4f, 2.6f);
            Block(41f, true);

            BlockedArena("Arena_2_Estafilococo", 48f, 22f,
                "S. aureus: vive en la nariz y ataca en grupo. Antibiótico. Luego disuelve la mucosidad con Mucolítico.", true,
                W(0.6f, E(prefabs.SAureus, data.SAureus, 3)),
                W(1.0f, E(prefabs.SAureus, data.SAureus, 2, SpawnSide.Left), E(prefabs.Polen, data.Polen, 2), E(prefabs.Rinovirus, data.Rinovirus, 1)));
            Platform(53f, 2.4f, 3f);
            Platform(60f, 2.4f, 3f);

            SetMessage("Mucosidad: tapa el paso y te frena si la tocas. Dispárale MUCOLÍTICO (el antihistamínico también sirve, pero menos).", 37f);
        }

        private void BuildTraquea()
        {
            BuildLevelBase(95f, new LevelStyle
            {
                background = new Color(0.16f, 0.08f, 0.12f),
                wall = new Color(0.82f, 0.66f, 0.7f),
                floor = new Color(0.95f, 0.85f, 0.85f),
                floorEdge = new Color(1f, 1f, 1f),
                blob = new Color(1f, 0.95f, 0.95f, 0.4f) // anillos de cartílago
            }, "NODO: Tráquea. Detectamos tos ferina: Bordetella pertussis. Antibiótico.",
                new Vector2(36f, 3f));

            Arena("Arena_1_TosFerina", 8f, 18f, "B. pertussis: bacteria de la tos ferina. Empuja fuerte al chocar. Antibiótico.",
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 2)),
                W(1.0f, E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Both), E(prefabs.Rinovirus, data.Rinovirus, 1)));
            Platform(13f, 2.2f, 3f);

            // Inflamación: tutorial sin enemigos.
            Block(29f, false);
            SetMessage("Inflamación: el tejido hinchado cierra el paso. Dispárale ANTIINFLAMATORIO hasta que se contraiga.", 24f);

            // Tramo de plataformas sobre un hueco (practica doble salto y caída lenta).
            Platform(35f, 2.4f, 2.5f);
            Platform(38.5f, 3.6f, 2.5f);
            Platform(42f, 2.4f, 2.5f);

            BlockedArena("Arena_2_Bronquio", 50f, 22f,
                "Influenza: virus que dispara más rápido. Antiviral. Al final, mucosidad: Mucolítico.", true,
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 3)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 1), E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Both)));
            Platform(55f, 2.4f, 3f);
            Platform(63f, 2.4f, 3f);

            Block(80f, false);
        }

        private void BuildPulmones()
        {
            // Paleta azul y blanca, ambiente abierto y aéreo (GDD 7.2). 3 tramos, cada uno con un bloqueo.
            LevelManager manager = BuildLevelBase(120f, new LevelStyle
            {
                background = new Color(0.06f, 0.1f, 0.2f),
                wall = new Color(0.45f, 0.62f, 0.85f),
                floor = new Color(0.75f, 0.85f, 0.98f),
                floorEdge = new Color(1f, 1f, 1f),
                blob = new Color(0.85f, 0.93f, 1f, 0.35f) // alvéolos
            }, "NODO: Pulmones. La infección se asentó en los bronquiolos. Cada tramo está bloqueado: elimina al grupo y despeja el paso.",
                new Vector2(30f, 3f), new Vector2(37f, 3f), new Vector2(68f, 3f));

            // Tramo 1
            BlockedArena("Tramo_1_Neumonia", 8f, 20f,
                "S. pneumoniae (neumonía): atacan en grupo. Antibiótico.", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 3)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 2, SpawnSide.Left), E(prefabs.Influenza, data.Influenza, 1)));
            Platform(13f, 2.4f, 3f);

            // Alvéolos: plataformas sobre huecos.
            Platform(31.5f, 2.6f, 2.4f);
            Platform(34.5f, 4f, 2.2f);
            Platform(38.5f, 2.6f, 2.4f);
            Platform(41.5f, 4.2f, 2.2f);

            // Tramo 2
            BlockedArena("Tramo_2_Influenza", 46f, 20f,
                "Influenza en el aire y neumococos en el piso. Al final, inflamación: Antiinflamatorio.", false,
                W(0.6f, E(prefabs.Influenza, data.Influenza, 2)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 3, SpawnSide.Both), E(prefabs.Influenza, data.Influenza, 1)));
            Platform(52f, 2.4f, 3f);
            Platform(58f, 3.4f, 3f);

            Platform(69.5f, 2.6f, 2.4f);

            // Tramo 3: clímax (sin jefe). La mayor concentración de enemigos y un bloqueo doble.
            BlockedArena("Tramo_3_FocoTuberculosis", 78f, 26f,
                "NODO: ¡Foco de tuberculosis! M. tuberculosis es resistente: necesita muchas dosis de antibiótico.", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 4, SpawnSide.Both)),
                W(1.0f, E(prefabs.Tuberculosis, data.Tuberculosis, 1), E(prefabs.Influenza, data.Influenza, 2)),
                W(1.2f, E(prefabs.Tuberculosis, data.Tuberculosis, 2, SpawnSide.Both), E(prefabs.Neumococo, data.Neumococo, 3),
                    E(prefabs.Influenza, data.Influenza, 2, SpawnSide.Left)));
            Platform(84f, 2.4f, 3f);
            Platform(92f, 3.4f, 3f);
            Block(78f + 26f - 3f + 2.2f, false); // detrás de la mucosidad, la inflamación

            // Cierre de la entrega de diciembre (GDD 8.6).
            SetupUtils.SetString(manager, "completeMessage",
                "NODO: Fórmula 2 enviada a Aurelis. Alerta: la infección avanza hacia el cerebro...");
            SetupUtils.SetFloat(manager, "completeDelay", 4.5f);
        }

        /// <summary>Mensaje de NODO que aparece al pasar por una X (tutoriales sin arena).</summary>
        private static void SetMessage(string text, float x)
        {
            var go = new GameObject("Mensaje");
            go.transform.position = new Vector3(x, FloorY, 0f);
            var trigger = go.AddComponent<MessageTrigger>();
            SetupUtils.SetString(trigger, "message", text);
        }

        private void BuildSandbox()
        {
            BuildLevelBase(70f, new LevelStyle
            {
                background = new Color(0.1f, 0.1f, 0.12f),
                wall = new Color(0.3f, 0.3f, 0.34f),
                floor = new Color(0.45f, 0.45f, 0.5f),
                floorEdge = new Color(0.6f, 0.6f, 0.65f),
                blob = new Color(0.4f, 0.4f, 0.45f, 0.5f)
            }, "SANDBOX: escena libre para probar mecánicas. Duplícala para tus pruebas.",
                new Vector2(5f, 2f));

            Acid(1f);
            Platform(-3f, 2.2f, 3f);
            Platform(1f, 3.8f, 2.5f);

            Arena("Arena_Enemigos", 10f, 20f, "Todos los enemigos",
                W(0.5f, E(prefabs.HPylori, data.HPylori, 1), E(prefabs.Giardia, data.Giardia, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.CDifficile, data.CDifficile, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)));
            Platform(14f, 2.2f, 3f);
            Platform(22f, 2.2f, 3f);

            Arena("Arena_Jefe", 38f, 22f, "Jefe lombriz (versión final)",
                W(1.0f, E(prefabs.LombrizGrueso, data.Lombriz, 1)));
        }
    }
}
