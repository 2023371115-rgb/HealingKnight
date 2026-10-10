using HealingKnight.Core.Depth;
using NUnit.Framework;
using UnityEngine;

namespace HealingKnight.Tests
{
    public class DepthMathTests
    {
        [Test]
        public void MismaFranja_DentroDeTolerancia()
        {
            Assert.IsTrue(DepthMath.SameLane(-1.5f, -1.8f, 0.35f));
            Assert.IsFalse(DepthMath.SameLane(-1.5f, -2.0f, 0.35f));
        }

        [Test]
        public void Ataque_TocaSoloEnLaMismaFranjaYAlturaDelCuerpo()
        {
            var target = new Vector2(2f, -1.5f);

            // Misma franja, a la altura del pecho: toca.
            Assert.IsTrue(DepthMath.Overlaps(new Vector2(1.8f, -1.5f), 0.2f, 1f, target, 0.4f, 0f, 1.8f, 0.35f));

            // Otra franja de profundidad: no toca aunque esté a la misma X.
            Assert.IsFalse(DepthMath.Overlaps(new Vector2(2f, -0.8f), 0.2f, 1f, target, 0.4f, 0f, 1.8f, 0.35f));

            // Pasa por encima de la cabeza: no toca.
            Assert.IsFalse(DepthMath.Overlaps(new Vector2(2f, -1.5f), 0.2f, 2.5f, target, 0.4f, 0f, 1.8f, 0.35f));

            // El objetivo está saltando: una cápsula a 2.5 de altura sí lo alcanza.
            Assert.IsTrue(DepthMath.Overlaps(new Vector2(2f, -1.5f), 0.2f, 2.5f, target, 0.4f, 1.2f, 1.8f, 0.35f));
        }

        [Test]
        public void Escudo_SoloBloqueaPorElFrente()
        {
            Assert.IsTrue(DepthMath.IsInFront(0f, 1, 2f));
            Assert.IsFalse(DepthMath.IsInFront(0f, 1, -2f));
            Assert.IsTrue(DepthMath.IsInFront(0f, -1, -2f));
        }

        [Test]
        public void Salto_AlturaYTiempoAlPuntoMasAlto()
        {
            const float height = 1.6f, apex = 0.32f;
            float v0 = JumpMath.InitialVelocity(height, apex);
            float g = JumpMath.Gravity(height, apex);

            // Altura máxima = v0² / 2g
            Assert.AreEqual(height, v0 * v0 / (2f * g), 0.0001f);
            // Tiempo al ápice = v0 / g
            Assert.AreEqual(apex, v0 / g, 0.0001f);
        }
    }
}
