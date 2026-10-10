using UnityEngine;

namespace HealingKnight.Gameplay.Movement
{
    /// <summary>
    /// Zona del piso donde se puede caminar. X = izquierda/derecha, Y = profundidad.
    /// El nivel define los límites totales; una arena activa los estrecha en X
    /// (la cámara se "bloquea" y no puedes avanzar hasta derrotar la oleada).
    /// </summary>
    public static class WalkableArea
    {
        private static Rect levelBounds = new Rect(-1000f, -3f, 2000f, 3f);
        private static bool hasLock;
        private static float lockMinX;
        private static float lockMaxX;

        public static Rect LevelBounds => levelBounds;
        public static bool IsLocked => hasLock;

        public static float MinX => hasLock ? Mathf.Max(lockMinX, levelBounds.xMin) : levelBounds.xMin;
        public static float MaxX => hasLock ? Mathf.Min(lockMaxX, levelBounds.xMax) : levelBounds.xMax;
        public static float MinY => levelBounds.yMin;
        public static float MaxY => levelBounds.yMax;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            levelBounds = new Rect(-1000f, -3f, 2000f, 3f);
            hasLock = false;
        }

        public static void SetLevelBounds(Rect bounds)
        {
            levelBounds = bounds;
            hasLock = false;
        }

        public static void Lock(float minX, float maxX)
        {
            hasLock = true;
            lockMinX = minX;
            lockMaxX = maxX;
        }

        public static void Unlock()
        {
            hasLock = false;
        }

        public static Vector2 Clamp(Vector2 groundPosition, float margin = 0.3f)
        {
            groundPosition.x = Mathf.Clamp(groundPosition.x, MinX + margin, MaxX - margin);
            groundPosition.y = Mathf.Clamp(groundPosition.y, MinY, MaxY);
            return groundPosition;
        }

        public static Vector2 RandomPointInDepth(float x)
        {
            return new Vector2(x, Random.Range(MinY + 0.2f, MaxY - 0.2f));
        }
    }
}
