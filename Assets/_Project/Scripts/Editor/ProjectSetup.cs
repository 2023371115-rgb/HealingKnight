using System.Text;
using HealingKnight.Core.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Menú "Healing Knight" del Editor.
    /// "Crear proyecto base" genera (solo lo que falte): sprites placeholder, datos (medicamentos,
    /// tabla de efectividad, enemigos, sistemas y niveles), prefabs y escenas. Se puede volver a ejecutar
    /// sin perder cambios: nunca sobrescribe un asset que ya existe.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Healing Knight/Crear proyecto base", priority = 0)]
        public static void CreateBaseProject()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Healing Knight", "Creando datos...", 0.2f);
                var data = new DataSetup();
                data.Run();
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Healing Knight", "Creando prefabs...", 0.5f);
                var prefabs = new PrefabSetup(data);
                prefabs.Run();
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Healing Knight", "Creando escenas...", 0.8f);
                var scenes = new SceneSetup(data, prefabs);
                scenes.Run();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.OpenScene(SceneSetup.MainMenuPath);

                var summary = new StringBuilder("Proyecto base listo.\n\n");
                summary.AppendLine(scenes.Created.Count > 0
                    ? $"Escenas nuevas: {scenes.Created.Count}"
                    : "Las escenas ya existían (no se tocaron).");
                summary.AppendLine("\nAbre MainMenu y dale Play, o abre Sandbox para probar mecánicas.");
                EditorUtility.DisplayDialog("Healing Knight", summary.ToString(), "Va");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Healing Knight/Abrir escena/Menú principal", priority = 20)]
        private static void OpenMainMenu() => Open(SceneSetup.MainMenuPath);

        [MenuItem("Healing Knight/Abrir escena/Mapa del cuerpo", priority = 21)]
        private static void OpenBodyMap() => Open(SceneSetup.BodyMapPath);

        [MenuItem("Healing Knight/Abrir escena/Digestivo · Boca", priority = 22)]
        private static void OpenBoca() => Open(SceneSetup.BocaPath);

        [MenuItem("Healing Knight/Abrir escena/Digestivo · Estómago", priority = 23)]
        private static void OpenEstomago() => Open(SceneSetup.EstomagoPath);

        [MenuItem("Healing Knight/Abrir escena/Digestivo · Intestino", priority = 24)]
        private static void OpenIntestino() => Open(SceneSetup.IntestinoPath);

        [MenuItem("Healing Knight/Abrir escena/Sandbox", priority = 25)]
        private static void OpenSandbox() => Open(SceneSetup.SandboxPath);

        [MenuItem("Healing Knight/Borrar progreso guardado", priority = 40)]
        private static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Healing Knight", "¿Borrar el progreso guardado?\n" + SaveSystem.FilePath, "Borrar", "Cancelar"))
                return;
            SaveSystem.Delete();
            Debug.Log("[Healing Knight] Progreso borrado.");
        }

        private static void Open(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                EditorUtility.DisplayDialog("Healing Knight", "Esa escena no existe todavía. Ejecuta 'Healing Knight > Crear proyecto base'.", "Va");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
    }
}
