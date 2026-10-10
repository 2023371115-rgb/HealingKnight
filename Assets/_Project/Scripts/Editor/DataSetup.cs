using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>Crea los ScriptableObjects base con los valores del GDD. Si ya existen, no los toca.</summary>
    internal class DataSetup
    {
        public const string DataFolder = "Assets/_Project/Data";

        public MedicationData Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico, Antiparasitario, Mucolitico;
        public EffectivenessTable Table;
        public PlayerConfig PlayerConfig;
        public EnemyData HPylori, Giardia, Rotavirus, Entamoeba, CDifficile, Oxiuro, SMutans, Lombriz;
        public GameCatalog Catalog;
        public LevelData DigestivoEstomago, DigestivoDelgado, DigestivoGrueso, DigestivoBoca;

        public void Run()
        {
            CreateMedications();
            CreateEffectivenessTable();
            CreatePlayerConfig();
            CreateEnemies();
            CreateCatalog();
        }

        private MedicationData Med(string file, string name, Color color, bool regional, string description)
        {
            return SetupUtils.LoadOrCreate<MedicationData>($"{DataFolder}/Medications/{file}.asset", m =>
            {
                m.displayName = name;
                m.color = color;
                m.isRegional = regional;
                m.description = description;
            });
        }

        private void CreateMedications()
        {
            Antibiotico = Med("Antibiotico", "Antibiótico", new Color(0.25f, 0.55f, 1f), false, "Efectivo contra bacterias.");
            Antiviral = Med("Antiviral", "Antiviral", new Color(0.85f, 0.95f, 1f), false, "Efectivo contra virus.");
            Antiinflamatorio = Med("Antiinflamatorio", "Antiinflamatorio", new Color(1f, 0.5f, 0.75f), false, "Reduce la inflamación. Parcial contra bacterias y virus.");
            Antihistaminico = Med("Antihistaminico", "Antihistamínico", new Color(0.7f, 0.5f, 1f), false, "Parcial contra parásitos y mucosidad.");
            Antiparasitario = Med("Antiparasitario", "Antiparasitario", new Color(1f, 0.72f, 0.1f), true, "Cápsula regional del Sistema Digestivo. Efectivo contra parásitos.");
            Mucolitico = Med("Mucolitico", "Mucolítico", new Color(0.55f, 1f, 0.6f), true, "Cápsula regional del Sistema Respiratorio. Disuelve la mucosidad.");
        }

        private void CreateEffectivenessTable()
        {
            Table = SetupUtils.LoadOrCreate<EffectivenessTable>($"{DataFolder}/Combat/EffectivenessTable.asset", t =>
            {
                // GDD 5.7 (la columna "Regional" se reparte entre Antiparasitario y Mucolítico).
                t.Set(ThreatType.Bacteria, Antibiotico, Effectiveness.Effective);
                t.Set(ThreatType.Bacteria, Antiinflamatorio, Effectiveness.Partial);
                t.Set(ThreatType.Bacteria, Antiparasitario, Effectiveness.Partial);
                t.Set(ThreatType.Bacteria, Mucolitico, Effectiveness.Partial);

                t.Set(ThreatType.Virus, Antiviral, Effectiveness.Effective);
                t.Set(ThreatType.Virus, Antiinflamatorio, Effectiveness.Partial);
                t.Set(ThreatType.Virus, Antiparasitario, Effectiveness.Partial);
                t.Set(ThreatType.Virus, Mucolitico, Effectiveness.Partial);

                t.Set(ThreatType.Parasito, Antiparasitario, Effectiveness.Effective);
                t.Set(ThreatType.Parasito, Antibiotico, Effectiveness.Partial);
                t.Set(ThreatType.Parasito, Antihistaminico, Effectiveness.Partial);

                t.Set(ThreatType.Inflamacion, Antiinflamatorio, Effectiveness.Effective);

                t.Set(ThreatType.Mucosidad, Mucolitico, Effectiveness.Effective);
                t.Set(ThreatType.Mucosidad, Antihistaminico, Effectiveness.Partial);
            });
        }

        private void CreatePlayerConfig()
        {
            PlayerConfig = SetupUtils.LoadOrCreate<PlayerConfig>($"{DataFolder}/Player/PlayerConfig.asset", c =>
            {
                c.muzzleHeight = 0.85f;
                c.genericMedications = new List<MedicationData> { Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico };
            });
        }

        private EnemyData Enemy(string file, System.Action<EnemyData> init)
        {
            return SetupUtils.LoadOrCreate<EnemyData>($"{DataFolder}/Enemies/{file}.asset", init);
        }

        private void CreateEnemies()
        {
            // Todos son patógenos reales del sistema digestivo (con licencia de "mutados" para el juego).
            HPylori = Enemy("Bacteria_HPylori", e =>
            {
                e.displayName = "H. pylori";
                e.threat = ThreatType.Bacteria;
                e.maxHealth = 20f; e.moveSpeed = 3f;
                e.attackDistance = 1f; e.attackWindup = 0.35f; e.attackDamage = 10f;
                e.tint = new Color(0.5f, 0.85f, 0.4f);
            });

            Giardia = Enemy("Parasito_Giardia", e =>
            {
                e.displayName = "Giardia";
                e.threat = ThreatType.Parasito;
                e.maxHealth = 22f; e.moveSpeed = 3.2f;
                e.attackDistance = 1f; e.attackWindup = 0.35f; e.attackDamage = 8f;
                e.tint = new Color(0.95f, 0.85f, 0.55f);
            });

            Rotavirus = Enemy("Virus_Rotavirus", e =>
            {
                e.displayName = "Rotavirus";
                e.threat = ThreatType.Virus;
                e.maxHealth = 20f; e.moveSpeed = 2f;
                e.attackType = EnemyAttackType.Ranged;
                e.attackDistance = 5.5f; e.attackWindup = 0.4f; e.attackCooldown = 2.5f; e.attackDamage = 10f;
                e.projectileColor = new Color(0.8f, 0.5f, 1f);
                e.tint = new Color(0.75f, 0.45f, 0.95f);
            });

            Entamoeba = Enemy("Parasito_Entamoeba", e =>
            {
                e.displayName = "Entamoeba (ameba)";
                e.threat = ThreatType.Parasito;
                e.maxHealth = 50f; e.moveSpeed = 1.4f;
                e.attackDistance = 1.4f; e.attackWindup = 0.6f; e.attackDamage = 15f; e.attackKnockback = 6f;
                e.knockbackResistance = 0.6f; e.linfocitoCharge = 2;
                e.tint = new Color(0.8f, 0.6f, 0.75f);
                e.visualScale = 1.4f;
            });

            CDifficile = Enemy("Bacteria_CDifficile", e =>
            {
                e.displayName = "C. difficile";
                e.threat = ThreatType.Bacteria;
                e.maxHealth = 28f; e.moveSpeed = 2.6f;
                e.attackDistance = 1.1f; e.attackWindup = 0.4f; e.attackDamage = 12f;
                e.tint = new Color(0.45f, 0.65f, 0.35f);
                e.visualScale = 1.1f;
            });

            Oxiuro = Enemy("Parasito_Oxiuro", e =>
            {
                e.displayName = "Oxiuro";
                e.threat = ThreatType.Parasito;
                e.maxHealth = 14f; e.moveSpeed = 3.5f;
                e.attackDistance = 0.9f; e.attackWindup = 0.3f; e.attackDamage = 6f;
                e.tint = new Color(0.97f, 0.95f, 0.88f);
                e.visualScale = 0.8f;
            });

            SMutans = Enemy("Bacteria_SMutans", e =>
            {
                e.displayName = "S. mutans (caries)";
                e.threat = ThreatType.Bacteria;
                e.maxHealth = 18f; e.moveSpeed = 2.8f;
                e.attackDistance = 1f; e.attackWindup = 0.35f; e.attackDamage = 8f;
                e.tint = new Color(0.85f, 0.85f, 0.55f);
            });

            Lombriz = Enemy("Jefe_Lombriz", e =>
            {
                e.displayName = "Lombriz";
                e.threat = ThreatType.Parasito;
                e.maxHealth = 320f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f;
                e.hurtStunTime = 0f;
                e.linfocitoCharge = 5;
                e.tint = new Color(0.95f, 0.62f, 0.6f);
            });
        }

        private LevelData Level(string system, string file, string id, string name, string scene, bool formula, string objective)
        {
            return SetupUtils.LoadOrCreate<LevelData>($"{DataFolder}/Systems/{system}/{file}.asset", l =>
            {
                l.id = id;
                l.displayName = name;
                l.sceneName = scene;
                l.grantsFormula = formula;
                l.objective = objective;
            });
        }

        private SystemData MakeSystem(string folder, string id, string name, string number, MedicationData regional,
            Color color, Vector2 mapPosition, string description, params LevelData[] levels)
        {
            return SetupUtils.LoadOrCreate<SystemData>($"{DataFolder}/Systems/{folder}/{folder}.asset", s =>
            {
                s.id = id;
                s.displayName = name;
                s.mapNumber = number;
                s.regionalMedication = regional;
                s.mapColor = color;
                s.mapPosition = mapPosition;
                s.description = description;
                s.levels = new List<LevelData>(levels);
            });
        }

        private void CreateCatalog()
        {
            DigestivoEstomago = Level("Digestivo", "01_Estomago", "digestivo_estomago", "Estómago", "Digestivo_Estomago", false,
                "Aprende a pelear y cruza el ácido gástrico eliminando a H. pylori.");
            DigestivoDelgado = Level("Digestivo", "02_IntestinoDelgado", "digestivo_delgado", "Intestino delgado", "Digestivo_IntestinoDelgado", false,
                "Elimina Giardia y rotavirus. La lombriz aparece... y huye.");
            DigestivoGrueso = Level("Digestivo", "03_IntestinoGrueso", "digestivo_grueso", "Intestino grueso", "Digestivo_IntestinoGrueso", true,
                "Amebas, C. difficile y oxiuros. Acaba con la lombriz.");
            DigestivoBoca = Level("Digestivo", "99_Boca_Secreto", "digestivo_boca", "Boca", "Digestivo_Boca", false,
                "Nivel secreto: caries y placa bacteriana.");
            if (!DigestivoBoca.hidden)
            {
                DigestivoBoca.hidden = true;
                DigestivoBoca.secretId = "digestivo_boca";
                UnityEditor.EditorUtility.SetDirty(DigestivoBoca);
            }

            SystemData digestivo = MakeSystem("Digestivo", "digestivo", "Sistema Digestivo", "I", Antiparasitario,
                new Color(0.95f, 0.45f, 0.55f), new Vector2(0.5f, 0.3f),
                "Los patógenos se ingirieron en cápsulas y se instalaron aquí primero.",
                DigestivoEstomago, DigestivoDelgado, DigestivoGrueso, DigestivoBoca);

            SystemData respiratorio = MakeSystem("Respiratorio", "respiratorio", "Sistema Respiratorio", "II", Mucolitico,
                new Color(0.45f, 0.75f, 1f), new Vector2(0.5f, 0.6f),
                "La mucosidad atrapa a los patógenos y los protege del medicamento.",
                Level("Respiratorio", "01_Nariz", "respiratorio_nariz", "Nariz", "", false, "Rinovirus y alérgenos."),
                Level("Respiratorio", "02_Traquea", "respiratorio_traquea", "Tráquea", "", false, "Tos ferina (Bordetella pertussis)."),
                Level("Respiratorio", "03_Pulmones", "respiratorio_pulmones", "Pulmones", "", true, "Neumonía, influenza y el foco de tuberculosis."));

            SystemData nervioso = MakeSystem("Nervioso", "nervioso", "Sistema Nervioso", "III", null,
                new Color(0.7f, 0.5f, 1f), new Vector2(0.5f, 0.9f),
                "Tormentas eléctricas y tejido sano que hay que proteger.",
                Level("Nervioso", "01_Medula", "nervioso_medula", "Médula espinal", "", false, ""),
                Level("Nervioso", "02_Cerebro", "nervioso_cerebro", "Cerebro", "", true, ""));

            SystemData circulatorio = MakeSystem("Circulatorio", "circulatorio", "Sistema Circulatorio", "IV", null,
                new Color(0.9f, 0.2f, 0.25f), new Vector2(0.45f, 0.65f),
                "La Cepa Madre bombea variantes resistentes a todo el cuerpo.",
                Level("Circulatorio", "01_Venas", "circulatorio_venas", "Venas y arterias", "", false, ""),
                Level("Circulatorio", "02_Corazon", "circulatorio_corazon", "Corazón", "", true, ""));

            Catalog = SetupUtils.LoadOrCreate<GameCatalog>($"{DataFolder}/Resources/{GameCatalog.ResourcePath}.asset", c =>
            {
                c.systems = new List<SystemData> { digestivo, respiratorio, nervioso, circulatorio };
            });
        }
    }
}
