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
                    "¡LA LOMBRIZ! Usa Antiparasitario. Esquívala cambiando de profundidad.",
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

        // ---------- Proyectiles ----------

        private GameObject BuildProjectile(string name, Vector2 size)
        {
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>();
            var projectile = root.AddComponent<Projectile>();

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            var sr = SetupUtils.SpriteChild(visual.transform, "Sprite", PlaceholderArt.Capsule, Color.white, Vector3.zero, size, 0);

            SetupUtils.SpriteChild(root.transform, "Shadow", PlaceholderArt.Circle, new Color(0f, 0f, 0f, 0.25f),
                Vector3.zero, new Vector2(size.x, size.y * 0.5f), -1);

            SetupUtils.SetRef(projectile, "visual", visual.transform);
            SetupUtils.SetRef(projectile, "spriteRenderer", sr);
            return root;
        }

        // ---------- Jugador ----------

        private GameObject BuildPlayer()
        {
            var root = new GameObject("HealingKnight");
            root.AddComponent<SortingGroup>();

            SpriteRenderer shadow = SetupUtils.SpriteChild(root.transform, "Shadow", PlaceholderArt.Circle,
                new Color(0f, 0f, 0f, 0.35f), Vector3.zero, new Vector2(1.3f, 0.4f), -10);

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Transform v = visual.transform;

            // Figura placeholder: reemplázala por las piezas del personaje (PSD importado).
            SetupUtils.SpriteChild(v, "BackLeg", PlaceholderArt.Square, new Color(0.65f, 0.67f, 0.72f), new Vector3(-0.15f, 0f, 0f), new Vector2(0.25f, 0.45f), -2);
            SetupUtils.SpriteChild(v, "FrontLeg", PlaceholderArt.Square, new Color(0.85f, 0.87f, 0.9f), new Vector3(0.12f, 0f, 0f), new Vector2(0.25f, 0.45f), 1);
            SetupUtils.SpriteChild(v, "Torso", PlaceholderArt.Square, new Color(0.92f, 0.93f, 0.96f), new Vector3(0f, 0.4f, 0f), new Vector2(0.75f, 0.6f), 0);
            SetupUtils.SpriteChild(v, "Emblem", PlaceholderArt.SquareCentered, new Color(0.9f, 0.15f, 0.2f), new Vector3(0.05f, 0.72f, 0f), new Vector2(0.18f, 0.18f), 2);
            SetupUtils.SpriteChild(v, "Head", PlaceholderArt.CircleBottom, new Color(0.95f, 0.96f, 0.98f), new Vector3(0f, 0.95f, 0f), new Vector2(1.05f, 1.0f), 3);
            SetupUtils.SpriteChild(v, "Visor", PlaceholderArt.SquareCentered, new Color(0.3f, 0.9f, 1f), new Vector3(0.22f, 1.48f, 0f), new Vector2(0.55f, 0.14f), 4);
            SpriteRenderer arm = SetupUtils.SpriteChild(v, "ArmCannon", PlaceholderArt.Capsule, Color.white, new Vector3(0.45f, 0.82f, 0f), new Vector2(0.8f, 0.32f), 5);

            // Escudo burbuja (diseño del prototipo): rodea todo el cuerpo.
            SpriteRenderer shieldSprite = SetupUtils.SpriteChild(v, "ShieldBubble", PlaceholderArt.Circle, new Color(0.03f, 0.91f, 0.95f, 0.45f),
                new Vector3(0.05f, 0.95f, 0f), new Vector2(2.1f, 2.1f), 8);

            var body = root.AddComponent<DepthBody>();
            SetupUtils.SetRef(body, "visual", v);
            SetupUtils.SetRef(body, "shadow", shadow.transform);

            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<PlayerController>();
            SetupUtils.SetRef(controller, "config", data.PlayerConfig);

            var shooter = root.AddComponent<PlayerShooter>();
            SetupUtils.SetRef(shooter, "capsulePrefab", Capsule);
            SetupUtils.SetRef(shooter, "tintedArm", arm);
            SetupUtils.SetRef(shooter, "fallbackRegionalMedication", data.Antiparasitario);

            root.AddComponent<PlayerMelee>();

            var shield = root.AddComponent<PlayerShield>();
            SetupUtils.SetRef(shield, "shieldVisual", shieldSprite.gameObject);

            root.AddComponent<PlayerHealth>();

            var hurtbox = root.AddComponent<Hurtbox>();
            SetupUtils.SetFloat(hurtbox, "halfWidth", 0.4f);
            SetupUtils.SetFloat(hurtbox, "bodyHeight", 1.9f);

            return root;
        }

        // ---------- Enemigos ----------

        private enum EnemyShape { Long, Capsule, Round, Blob, Pear }

        private GameObject BuildEnemy(string name, EnemyData enemyData, EnemyShape shape)
        {
            float s = enemyData.visualScale;
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>();

            SpriteRenderer shadow = SetupUtils.SpriteChild(root.transform, "Shadow", PlaceholderArt.Circle,
                new Color(0f, 0f, 0f, 0.35f), Vector3.zero, new Vector2(1.2f * s, 0.35f * s), -10);

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer bodySprite;
            float bodyHeight;

            switch (shape)
            {
                case EnemyShape.Long: // parásito: alargado y pegado al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.4f * s, 0.6f * s), 0);
                    bodyHeight = 0.7f * s;
                    break;
                case EnemyShape.Capsule: // bacteria: cápsula/bastón
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Capsule, enemyData.tint,
                        new Vector3(0f, 0.4f * s, 0f), new Vector2(1.1f * s, 0.55f * s), 0);
                    bodyHeight = 0.9f * s;
                    break;
                case EnemyShape.Blob: // ameba / colonia: mancha pegada al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.1f * s, 0.85f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Lobe", PlaceholderArt.CircleBottom, Color.Lerp(enemyData.tint, Color.white, 0.2f),
                        new Vector3(-0.35f * s, 0f, 0f), new Vector2(0.6f * s, 0.55f * s), -1);
                    bodyHeight = 0.85f * s;
                    break;
                case EnemyShape.Pear: // giardia: forma de pera con "ojos"
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        new Vector3(0f, 0.2f * s, 0f), new Vector2(0.9f * s, 1.0f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Tail", PlaceholderArt.Capsule, Color.Lerp(enemyData.tint, Color.black, 0.15f),
                        new Vector3(-0.45f * s, 0.35f * s, 0f), new Vector2(0.6f * s, 0.18f * s), -1);
                    bodyHeight = 1.2f * s;
                    break;
                default: // virus: esfera flotante
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        new Vector3(0f, 1.0f * s, 0f), new Vector2(0.9f * s, 0.9f * s), 0);
                    bodyHeight = 1.5f * s;
                    break;
            }

            // Ojo para saber hacia dónde mira.
            SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black,
                bodySprite.transform.localPosition + new Vector3(0.3f * s, 0.25f * s, 0f), new Vector2(0.14f * s, 0.14f * s), 1);

            var body = root.AddComponent<DepthBody>();
            SetupUtils.SetRef(body, "visual", visual.transform);
            SetupUtils.SetRef(body, "shadow", shadow.transform);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", enemyData);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { bodySprite });

            var brain = root.AddComponent<EnemyBrain>();
            SetupUtils.SetRef(brain, "data", enemyData);
            if (enemyData.attackType == EnemyAttackType.Ranged) SetupUtils.SetRef(brain, "projectilePrefab", EnemyProjectile);

            var hurtbox = root.AddComponent<Hurtbox>();
            SetupUtils.SetFloat(hurtbox, "halfWidth", 0.5f * s);
            SetupUtils.SetFloat(hurtbox, "bodyHeight", bodyHeight + (shape == EnemyShape.Round ? 0.5f : 0.3f));

            return root;
        }

        // ---------- Jefe lombriz ----------

        private GameObject BuildLombriz(string name, float fleeAt, EnemyHealth minionPrefab, EnemyData minionData,
            string intro, string endMessage)
        {
            EnemyData d = data.Lombriz;
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>();

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer head = SetupUtils.SpriteChild(visual.transform, "Head", PlaceholderArt.Circle, d.tint,
                Vector3.zero, new Vector2(1.4f, 1.3f), 2);
            SetupUtils.SpriteChild(visual.transform, "Mouth", PlaceholderArt.Circle, new Color(0.35f, 0.08f, 0.12f),
                new Vector3(0.5f, -0.05f, 0f), new Vector2(0.45f, 0.55f), 3);
            SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black,
                new Vector3(0.2f, 0.35f, 0f), new Vector2(0.22f, 0.22f), 4);

            SpriteRenderer mound = SetupUtils.SpriteChild(root.transform, "Mound", PlaceholderArt.Circle, new Color(0.45f, 0.25f, 0.2f, 0.9f),
                Vector3.zero, new Vector2(1.8f, 0.5f), -20);

            GameObject segmentsRoot = SetupUtils.Child(root.transform, "Segments", Vector3.zero);

            var body = root.AddComponent<DepthBody>();
            SetupUtils.SetRef(body, "visual", visual.transform);
            SetupUtils.SetBool(body, "clampToWalkableArea", false);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", d);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { head });

            var hurtbox = root.AddComponent<Hurtbox>();
            SetupUtils.SetFloat(hurtbox, "halfWidth", 0.65f);
            SetupUtils.SetFloat(hurtbox, "bodyHeight", 1.3f);

            var boss = root.AddComponent<LombrizBoss>();
            SetupUtils.SetFloat(boss, "fleeAtHealth01", fleeAt);
            SetupUtils.SetString(boss, "introMessage", intro);
            SetupUtils.SetString(boss, fleeAt > 0f ? "fleeMessage" : "defeatMessage", endMessage);
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
            SpriteRenderer glow = SetupUtils.SpriteChild(root.transform, "Glow", PlaceholderArt.Circle, new Color(1f, 0.9f, 0.4f, 0.75f),
                Vector3.zero, new Vector2(0.9f, 0.35f), -40);
            SetupUtils.SetRef(secret, "glow", glow);
            return root;
        }

        // ---------- Peligros ----------

        private GameObject BuildAcid()
        {
            var root = new GameObject("AcidPool");
            var acid = root.AddComponent<AcidPool>();
            SetupUtils.SetVector2(acid, "size", new Vector2(3f, 0.9f));

            // Decal del piso: se dibuja debajo de los personajes.
            SetupUtils.SpriteChild(root.transform, "Puddle", PlaceholderArt.Circle, new Color(0.85f, 0.95f, 0.25f, 0.85f),
                Vector3.zero, new Vector2(3.2f, 1.0f), -50);
            SetupUtils.SpriteChild(root.transform, "Bubble1", PlaceholderArt.Circle, new Color(1f, 1f, 0.6f, 0.9f),
                new Vector3(-0.6f, 0.05f, 0f), new Vector2(0.25f, 0.18f), -49);
            SetupUtils.SpriteChild(root.transform, "Bubble2", PlaceholderArt.Circle, new Color(1f, 1f, 0.6f, 0.9f),
                new Vector3(0.7f, -0.1f, 0f), new Vector2(0.18f, 0.13f), -49);
            return root;
        }
    }
}
