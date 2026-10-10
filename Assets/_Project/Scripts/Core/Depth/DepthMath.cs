using UnityEngine;

namespace HealingKnight.Core.Depth
{
    /// <summary>
    /// Matemática del plano 2.5D estilo beat 'em up.
    /// - X: izquierda/derecha. Y: profundidad en el piso (arriba = más al fondo).
    /// - La altura del salto se lleva aparte (no es parte de la posición en el piso).
    /// Dos cosas solo chocan si están en la misma "franja" de profundidad.
    /// </summary>
    public static class DepthMath
    {
        /// <summary>¿Están en la misma franja de profundidad?</summary>
        public static bool SameLane(float depthA, float depthB, float laneTolerance)
        {
            return Mathf.Abs(depthA - depthB) <= laneTolerance;
        }

        /// <summary>
        /// ¿Un ataque toca a un objetivo?
        /// attackCenter/attackHalfWidth: zona horizontal del ataque en el piso.
        /// attackHeight: altura a la que viaja el ataque (0 = a ras de piso).
        /// targetGround/targetHalfWidth/targetHeight/targetBodyHeight: cuerpo del objetivo.
        /// </summary>
        public static bool Overlaps(
            Vector2 attackCenter, float attackHalfWidth, float attackHeight,
            Vector2 targetGround, float targetHalfWidth, float targetHeight, float targetBodyHeight,
            float laneTolerance)
        {
            if (!SameLane(attackCenter.y, targetGround.y, laneTolerance)) return false;
            if (Mathf.Abs(attackCenter.x - targetGround.x) > attackHalfWidth + targetHalfWidth) return false;

            // El ataque debe pasar a la altura del cuerpo del objetivo (que puede estar saltando).
            float bottom = targetHeight - 0.1f;
            float top = targetHeight + targetBodyHeight;
            return attackHeight >= bottom && attackHeight <= top;
        }

        /// <summary>Dirección horizontal (-1 o 1) de 'from' hacia 'to'. Si están alineados devuelve 'fallback'.</summary>
        public static int FacingTowards(float fromX, float toX, int fallback = 1)
        {
            float dx = toX - fromX;
            if (Mathf.Abs(dx) < 0.0001f) return fallback >= 0 ? 1 : -1;
            return dx > 0f ? 1 : -1;
        }

        /// <summary>¿El punto 'source' está delante de quien mira hacia 'facing'?</summary>
        public static bool IsInFront(float ownerX, int facing, float sourceX, float tolerance = 0.05f)
        {
            float dx = sourceX - ownerX;
            if (Mathf.Abs(dx) <= tolerance) return true;
            return Mathf.Sign(dx) == Mathf.Sign(facing);
        }
    }

    /// <summary>Salto con altura y tiempo al punto más alto fijos (la altura siempre es la misma).</summary>
    public static class JumpMath
    {
        public static float Gravity(float height, float timeToApex)
        {
            return 2f * height / (timeToApex * timeToApex);
        }

        public static float InitialVelocity(float height, float timeToApex)
        {
            return 2f * height / timeToApex;
        }
    }
}
