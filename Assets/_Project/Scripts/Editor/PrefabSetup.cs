using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Crea los prefabs con figuras de placeholder. Cuando tengas el arte, reemplaza lo que está
    /// dentro de "Visual" por tus piezas: los scripts de la raíz no cambian.
    /// </summary>
    internal class PrefabSetup
    {
        public const string Folder = "Assets/_Project/Prefabs";

        public Projectile Capsule, EnemyProjectile;
        public MedicineBomb Bomb;
        public PlayerController Player;
        public EnemyHealth HPylori, Giardia, Rotavirus, Entamoeba, CDifficile, Oxiuro, SMutans;
        public EnemyHealth LombrizDelgado, LombrizGrueso;
        public AcidPool Acid;
        public SecretPassage Secret;

        private readonly DataSetup data;

        public PrefabSetup(DataSetup data)
        {
            this.data = data;
        }

        public void Run()
        {
            Capsule = LoadOrBuild<Projectile>($"{Folder}/Projectiles/Capsule.prefab", () => BuildProjectile("Capsule", new Vector2(0.55f, 0.28f)));
            EnemyProjectile = LoadOrBuild<Projectile>($"{Folder}/Projectiles/EnemyProjectile.prefab", () => BuildProjectile("EnemyProjectile", new Vector2(0.4f, 0.4f)));
            Bomb = LoadOrBuild<MedicineBomb>($"{Folder}/Projectiles/MedicineBomb.prefab", BuildBomb);
            Player = LoadOrBuild<PlayerController>($"{Folder}/Player/HealingKnight.prefab", BuildPlayer);

            HPylori = Enemy("Bacteria_HPylori", data.HPylori, EnemyShape.Capsule);
            Giardia = Enemy("Parasito_Giardia", data.Giardia, EnemyShape.Pear);
            Rotavirus = Enemy("Virus_Rotavirus", data.Rotavirus, EnemyShape.Round);
            Entamoeba = Enemy("Parasito_Entamoeba", data.Entamoeba, EnemyShape.Blob);
            CDifficile = Enemy("Bacteria_CDifficile", data.CDifficile, EnemyShape.Capsule);
            Oxiuro = Enemy("Parasito_Oxiuro", data.Oxiuro, EnemyShape.Long);
            SMutans = Enemy("Bacteria_SMutans", data.SMutans, EnemyShape.Blob);

            LombrizDelgado = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Lombriz_IntestinoDelgado.prefab",
                () => BuildLombriz("Lombriz_IntestinoDelgado", 0.5f, null, null,
                    "¡LA LOMBRIZ! Usa Antiparasitario. Salta sobre su embestida.",
                    "NODO: ¡Escapa hacia el intestino grueso! Síguela."));
            LombrizGrueso = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Lombriz_IntestinoGrueso.prefab",
                () => BuildLombriz("Lombriz_IntestinoGrueso", 0f, Oxiuro, data.Oxiuro,
                    "NODO: Aquí se esconde. Acaba con ella.",
                    "NODO: Foco eliminado. Fórmula 1 obtenida."));

            Secret = LoadOrBuild<SecretPassage>($"{Folder}/Level/SecretPassage.prefab", BuildSecret);

            Acid = LoadOrBuild<AcidPool>($"{Folder}/Hazards/AcidPool.prefab", BuildAcid);
        }

        private EnemyHealth Enemy(string name, EnemyData enemyData, EnemyShape shape)
        {
            return LoadOrBuild<EnemyHealth>($"{Folder}/Enemies/{name}.prefab", () => BuildEnemy(name, enemyData, shape));
        }

        private static T LoadOrBuild<T>(string path, System.Func<GameObject> build) where T : Component
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<T>();
            return SetupUtils.SavePrefab<T>(build(), path);
        }

        // ---------- Física ----------

        private PhysicsMaterial2D noFriction;

        /// <summary>Sin fricción: evita que los personajes se queden pegados a las paredes al saltar.</summary>
        private PhysicsMaterial2D NoFriction
        {
            get
            {
                if (noFriction != null) return noFriction;
                const string path = "Assets/_Project/Data/Physics/NoFriction.physicsMaterial2D";
                noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
                if (noFriction == null)
                {
                    SetupUtils.EnsureFolder("Assets/_Project/Data/Physics");
                    noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
                    AssetDatabase.CreateAsset(noFriction, path);
                }
                return noFriction;
            }
        }

        /// <summary>Rigidbody2D + BoxCollider2D + CharacterMotor2D con los pies en el origen del objeto.</summary>
        private CharacterMotor2D AddMotor(GameObject root, Transform visual, Vector2 colliderSize)
        {
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = new Vector2(0f, colliderSize.y * 0.5f);
            box.sharedMaterial = NoFriction;

            var motor = root.AddComponent<CharacterMotor2D>();
            SetupUtils.SetRef(motor, "visual", visual);
            return motor;
        }

        private static void SetHurtbox(Hurtbox hurtbox, Vector2 offset, Vector2 size)
        {
            SetupUtils.SetVector2(hurtbox, "offset", offset);
            SetupUtils.SetVector2(hurtbox, "size", size);
        }

        // ---------- Proyectiles ----------

        private GameObject BuildProjectile(string name, Vector2 size)
        {
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>().sortingOrder = 20;
            var projectile = root.AddComponent<Projectile>();
            var sr = SetupUtils.SpriteChild(root.transform, "Sprite", PlaceholderArt.Capsule, Color.white, Vector3.zero, size, 0);
            SetupUtils.SetRef(projectile, "spriteRenderer", sr);
            return root;
        }

        private GameObject BuildBomb()
        {
            var root = new GameObject("MedicineBomb");
            root.AddComponent<SortingGroup>().sortingOrder = 21;
            var bomb = root.AddComponent<MedicineBomb>();
            var sr = SetupUtils.SpriteChild(root.transform, "Sprite", PlaceholderArt.Circle, Color.white, Vector3.zero, new Vector2(0.45f, 0.45f), 1);
            SetupUtils.SpriteChild(sr.transform, "Cap", PlaceholderArt.SquareCentered, new Color(1f, 1f, 1f, 0.9f),
                new Vector3(0f, 0.45f, 0f), new Vector2(0.35f, 0.25f), 2);
            var blast = SetupUtils.SpriteChild(root.transform, "Blast", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.6f), Vector3.zero, Vector2.one, 0);
            blast.enabled = false;
            SetupUtils.SetRef(bomb, "spriteRenderer", sr);
            SetupUtils.SetRef(bomb, "blastRenderer", blast);
            return root;
        }

        // ---------- Jugador ----------

        private GameObject BuildPlayer()
        {
            PlayerConfig config = data.PlayerConfig;
            var root = new GameObject("HealingKnight");
            root.layer = PhysicsLayers.Player;
            root.AddComponent<SortingGroup>().sortingOrder = 10;

            // Visual: se voltea al mirar a la izquierda. Los pies están en (0, 0).
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Transform v = visual.transform;

            // Body: piernas, torso y cabeza (se aplasta al agacharse). Reemplázalo por tus piezas.
            GameObject bodyGo = SetupUtils.Child(v, "Body", Vector3.zero);
            Transform b = bodyGo.transform;
            SetupUtils.SpriteChild(b, "BackLeg", PlaceholderArt.Square, new Color(0.65f, 0.67f, 0.72f), new Vector3(-0.15f, 0f, 0f), new Vector2(0.25f, 0.45f), -2);
            SetupUtils.SpriteChild(b, "FrontLeg", PlaceholderArt.Square, new Color(0.85f, 0.87f, 0.9f), new Vector3(0.12f, 0f, 0f), new Vector2(0.25f, 0.45f), 1);
            SetupUtils.SpriteChild(b, "Torso", PlaceholderArt.Square, new Color(0.92f, 0.93f, 0.96f), new Vector3(0f, 0.4f, 0f), new Vector2(0.75f, 0.6f), 0);
            SetupUtils.SpriteChild(b, "Emblem", PlaceholderArt.SquareCentered, new Color(0.9f, 0.15f, 0.2f), new Vector3(0.05f, 0.72f, 0f), new Vector2(0.18f, 0.18f), 2);
            SetupUtils.SpriteChild(b, "Head", PlaceholderArt.CircleBottom, new Color(0.95f, 0.96f, 0.98f), new Vector3(0f, 0.95f, 0f), new Vector2(1.05f, 1.0f), 3);
            SetupUtils.SpriteChild(b, "Visor", PlaceholderArt.SquareCentered, new Color(0.3f, 0.9f, 1f), new Vector3(0.22f, 1.48f, 0f), new Vector2(0.55f, 0.14f), 4);

            // Brazo-cañón: gira hacia donde apuntas (8 direcciones). El pivote es el hombro.
            GameObject armPivot = SetupUtils.Child(v, "ArmPivot", new Vector3(0.1f, config.muzzleHeight, 0f));
            SpriteRenderer arm = SetupUtils.SpriteChild(armPivot.transform, "ArmCannon", PlaceholderArt.Capsule, Color.white,
                new Vector3(0.35f, 0f, 0f), new Vector2(0.8f, 0.32f), 5);

            // Escudo burbuja (diseño del prototipo): rodea todo el cuerpo.
            SpriteRenderer shieldSprite = SetupUtils.SpriteChild(v, "ShieldBubble", PlaceholderArt.Circle, new Color(0.03f, 0.91f, 0.95f, 0.45f),
                new Vector3(0.05f, 0.95f, 0f), new Vector2(2.1f, 2.1f), 8);

            AddMotor(root, v, new Vector2(0.7f, config.standingColliderHeight));

            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<PlayerController>();
            SetupUtils.SetRef(controller, "config", config);
            SetupUtils.SetRef(controller, "bodyVisual", b);

            var shooter = root.AddComponent<PlayerShooter>();
            SetupUtils.SetRef(shooter, "capsulePrefab", Capsule);
            SetupUtils.SetRef(shooter, "armPivot", armPivot.transform);
            SetupUtils.SetRef(shooter, "tintedArm", arm);
            SetupUtils.SetRef(shooter, "fallbackRegionalMedication", data.Antiparasitario);

            var bombs = root.AddComponent<PlayerBombs>();
            SetupUtils.SetRef(bombs, "bombPrefab", Bomb);

            var shield = root.AddComponent<PlayerShield>();
            SetupUtils.SetRef(shield, "shieldVisual", shieldSprite.gameObject);

            root.AddComponent<PlayerHealth>();

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, new Vector2(0f, config.standingColliderHeight * 0.5f), new Vector2(0.75f, config.standingColliderHeight));

            return root;
        }

        // ---------- Enemigos ----------

        private enum EnemyShape { Long, Capsule, Round, Blob, Pear }

        private GameObject BuildEnemy(string name, EnemyData enemyData, EnemyShape shape)
        {
            float s = enemyData.visualScale;
            bool flying = enemyData.movement == EnemyMovement.Flying;
            var root = new GameObject(name);
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 5;

            // Los de suelo tienen los pies en (0, 0); los voladores, el centro.
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer bodySprite;
            Vector2 bodySize;

            switch (shape)
            {
                case EnemyShape.Long: // parásito: alargado y pegado al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.4f * s, 0.6f * s), 0);
                    bodySize = new Vector2(1.3f * s, 0.6f * s);
                    break;
                case EnemyShape.Capsule: // bacteria: cápsula/bastón de pie
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Capsule, enemyData.tint,
                        new Vector3(0f, 0.55f * s, 0f), new Vector2(1.1f * s, 0.55f * s), 0);
                    bodySprite.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
                    bodySize = new Vector2(0.6f * s, 1.1f * s);
                    break;
                case EnemyShape.Blob: // ameba / colonia: mancha pegada al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.1f * s, 0.85f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Lobe", PlaceholderArt.CircleBottom, Color.Lerp(enemyData.tint, Color.white, 0.2f),
                        new Vector3(-0.35f * s, 0f, 0f), new Vector2(0.6f * s, 0.55f * s), -1);
                    bodySize = new Vector2(1.1f * s, 0.85f * s);
                    break;
                case EnemyShape.Pear: // giardia: forma de pera con "ojos"
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        new Vector3(0f, 0.1f * s, 0f), new Vector2(0.9f * s, 1.0f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Tail", PlaceholderArt.Capsule, Color.Lerp(enemyData.tint, Color.black, 0.15f),
                        new Vector3(-0.45f * s, 0.35f * s, 0f), new Vector2(0.6f * s, 0.18f * s), -1);
                    bodySize = new Vector2(0.85f * s, 1.1f * s);
                    break;
                default: // virus: esfera
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        flying ? Vector3.zero : new Vector3(0f, 0.45f * s, 0f), new Vector2(0.9f * s, 0.9f * s), 0);
                    // Espículas para que se lea como virus.
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        SetupUtils.SpriteChild(bodySprite.transform, "Spike" + i, PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.white, 0.3f),
                            new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.55f, 0f), new Vector2(0.22f, 0.22f), -1);
                    }
                    bodySize = new Vector2(0.9f * s, 0.9f * s);
                    break;
            }

            // Ojo para saber hacia dónde mira.
            Vector3 eyeOffset = shape == EnemyShape.Capsule ? new Vector3(0.12f * s, 0.85f * s, 0f) : bodySprite.transform.localPosition + new Vector3(0.3f * s, 0.25f * s, 0f);
            SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black, eyeOffset, new Vector2(0.14f * s, 0.14f * s), 1);

            if (!flying) AddMotor(root, visual.transform, new Vector2(Mathf.Min(bodySize.x, 0.9f), bodySize.y));

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", enemyData);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { bodySprite });

            var brain = root.AddComponent<EnemyBrain>();
            SetupUtils.SetRef(brain, "data", enemyData);
            SetupUtils.SetRef(brain, "visual", visual.transform);
            if (enemyData.attackType == EnemyAttackType.Ranged) SetupUtils.SetRef(brain, "projectilePrefab", EnemyProjectile);

            var hurtbox = root.AddComponent<Hurtbox>();
            Vector2 hurtSize = bodySize + new Vector2(0.15f, 0.15f);
            SetHurtbox(hurtbox, flying ? Vector2.zero : new Vector2(0f, hurtSize.y * 0.5f), hurtSize);

            return root;
        }

        // ---------- Jefe lombriz ----------

        private GameObject BuildLombriz(string name, float fleeAt, EnemyHealth minionPrefab, EnemyData minionData,
            string intro, string endMessage)
        {
            EnemyData d = data.Lombriz;
            var root = new GameObject(name);
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 4;

            // La raíz es la cabeza (centro).
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer head = SetupUtils.SpriteChild(visual.transform, "Head", PlaceholderArt.Circle, d.tint,
                Vector3.zero, new Vector2(1.4f, 1.3f), 2);
            SetupUtils.SpriteChild(visual.transform, "Mouth", PlaceholderArt.Circle, new Color(0.35f, 0.08f, 0.12f),
                new Vector3(0.5f, -0.05f, 0f), new Vector2(0.45f, 0.55f), 3);
            SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black,
                new Vector3(0.2f, 0.35f, 0f), new Vector2(0.22f, 0.22f), 4);

            // Montículo de tierra que tiembla en el piso antes de salir (lo coloca el script).
            SpriteRenderer mound = SetupUtils.SpriteChild(root.transform, "Mound", PlaceholderArt.CircleBottom, new Color(0.45f, 0.25f, 0.2f, 0.95f),
                Vector3.zero, new Vector2(1.8f, 0.5f), 30);

            GameObject segmentsRoot = SetupUtils.Child(root.transform, "Segments", Vector3.zero);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", d);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { head });

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(1.4f, 1.3f));

            var boss = root.AddComponent<LombrizBoss>();
            SetupUtils.SetFloat(boss, "fleeAtHealth01", fleeAt);
            SetupUtils.SetString(boss, "introMessage", intro);
            SetupUtils.SetString(boss, fleeAt > 0f ? "fleeMessage" : "defeatMessage", endMessage);
            SetupUtils.SetRef(boss, "visual", visual.transform);
            SetupUtils.SetRef(boss, "segmentsRoot", segmentsRoot.transform);
            SetupUtils.SetRef(boss, "segmentSprite", PlaceholderArt.Circle);
            SetupUtils.SetColor(boss, "segmentColor", d.tint);
            SetupUtils.SetRef(boss, "moundRenderer", mound);
            if (minionPrefab != null)
            {
                SetupUtils.SetRef(boss, "minionPrefab", minionPrefab);
                SetupUtils.SetRef(boss, "minionData", minionData);
            }
            return root;
        }

        // ---------- Nivel ----------

        private GameObject BuildSecret()
        {
            var root = new GameObject("SecretPassage");
            var secret = root.AddComponent<SecretPassage>();
            // Grieta brillante en la pared: tócala para descubrir el pasaje.
            SpriteRenderer glow = SetupUtils.SpriteChild(root.transform, "Glow", PlaceholderArt.Circle, new Color(1f, 0.9f, 0.4f, 0.75f),
                Vector3.zero, new Vector2(0.6f, 1.3f), -5);
            SetupUtils.SetRef(secret, "glow", glow);
            return root;
        }

        // ---------- Peligros ----------

        private GameObject BuildAcid()
        {
            var root = new GameObject("AcidPool");
            var acid = root.AddComponent<AcidPool>();
            SetupUtils.SetVector2(acid, "size", new Vector2(2.8f, 0.5f));

            // Charco sobre el piso (el objeto se coloca a la altura del piso).
            SetupUtils.SpriteChild(root.transform, "Puddle", PlaceholderArt.Circle, new Color(0.85f, 0.95f, 0.25f, 0.9f),
                Vector3.zero, new Vector2(3.2f, 0.35f), 2);
            SetupUtils.SpriteChild(root.transform, "Bubble1", PlaceholderArt.Circle, new Color(1f, 1f, 0.6f, 0.9f),
                new Vector3(-0.6f, 0.12f, 0f), new Vector2(0.25f, 0.25f), 3);
            SetupUtils.SpriteChild(root.transform, "Bubble2", PlaceholderArt.Circle, new Color(1f, 1f, 0.6f, 0.9f),
                new Vector3(0.7f, 0.08f, 0f), new Vector2(0.18f, 0.18f), 3);
            return root;
        }
    }
}
