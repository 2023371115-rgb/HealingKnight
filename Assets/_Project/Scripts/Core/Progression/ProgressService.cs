namespace HealingKnight.Core.Progression
{
    /// <summary>
    /// Reglas de desbloqueo (lineales):
    /// - El primer sistema siempre está disponible.
    /// - Un sistema se desbloquea al completar todos los niveles del anterior.
    /// - Dentro de un sistema, cada nivel se desbloquea al completar el anterior.
    /// - Lo completado se puede rejugar.
    /// No depende de escenas ni de disco, por eso se puede probar con pruebas automáticas.
    /// </summary>
    public class ProgressService
    {
        private readonly GameCatalog catalog;
        private readonly SaveData save;

        public ProgressService(GameCatalog catalog, SaveData save)
        {
            this.catalog = catalog;
            this.save = save ?? new SaveData();
        }

        public SaveData Save => save;

        public bool IsSystemUnlocked(SystemData system)
        {
            int index = catalog.systems.IndexOf(system);
            if (index < 0) return false;
            if (index == 0) return true;
            return IsSystemCompleted(catalog.systems[index - 1]);
        }

        public bool IsSystemCompleted(SystemData system)
        {
            if (system == null || system.levels.Count == 0) return false;
            foreach (LevelData level in system.levels)
            {
                if (level == null || !save.IsLevelCompleted(level.id)) return false;
            }
            return true;
        }

        public bool IsLevelCompleted(LevelData level)
        {
            return level != null && save.IsLevelCompleted(level.id);
        }

        public bool IsLevelUnlocked(LevelData level)
        {
            SystemData system = catalog.FindSystemOf(level);
            if (system == null || !IsSystemUnlocked(system)) return false;

            int index = system.levels.IndexOf(level);
            if (index == 0) return true;
            return save.IsLevelCompleted(system.levels[index - 1].id);
        }

        /// <summary>Desbloqueado y con escena lista para jugarse.</summary>
        public bool IsLevelPlayable(LevelData level)
        {
            return level != null && level.HasScene && IsLevelUnlocked(level);
        }

        /// <summary>Marca el nivel como completado y, si da fórmula, la registra. Devuelve true si es la primera vez.</summary>
        public bool CompleteLevel(LevelData level)
        {
            if (level == null) return false;
            bool firstTime = !save.IsLevelCompleted(level.id);
            save.MarkLevelCompleted(level.id);

            if (level.grantsFormula)
            {
                SystemData system = catalog.FindSystemOf(level);
                if (system != null) save.AddFormula(system.id);
            }

            return firstTime;
        }
    }
}
