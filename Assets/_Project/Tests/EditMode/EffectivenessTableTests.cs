using HealingKnight.Core.Combat;
using NUnit.Framework;
using UnityEngine;

namespace HealingKnight.Tests
{
    public class EffectivenessTableTests
    {
        private EffectivenessTable table;
        private MedicationData antibiotico, antiviral, antiparasitario;

        [SetUp]
        public void SetUp()
        {
            table = ScriptableObject.CreateInstance<EffectivenessTable>();
            antibiotico = ScriptableObject.CreateInstance<MedicationData>();
            antiviral = ScriptableObject.CreateInstance<MedicationData>();
            antiparasitario = ScriptableObject.CreateInstance<MedicationData>();

            table.Set(ThreatType.Bacteria, antibiotico, Effectiveness.Effective);
            table.Set(ThreatType.Parasito, antiparasitario, Effectiveness.Effective);
            table.Set(ThreatType.Parasito, antibiotico, Effectiveness.Partial);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(antibiotico);
            Object.DestroyImmediate(antiviral);
            Object.DestroyImmediate(antiparasitario);
        }

        [Test]
        public void MedicamentoCorrecto_EsEfectivo()
        {
            Assert.AreEqual(Effectiveness.Effective, table.Evaluate(ThreatType.Bacteria, antibiotico));
            Assert.AreEqual(1f, table.GetMultiplier(ThreatType.Bacteria, antibiotico));
        }

        [Test]
        public void MedicamentoParcial_HaceUnCuartoDelDanio()
        {
            Assert.AreEqual(Effectiveness.Partial, table.Evaluate(ThreatType.Parasito, antibiotico));
            Assert.AreEqual(0.25f, table.GetMultiplier(ThreatType.Parasito, antibiotico), 0.0001f);
        }

        [Test]
        public void CombinacionNoListada_EsIneficaz()
        {
            Assert.AreEqual(Effectiveness.Ineffective, table.Evaluate(ThreatType.Bacteria, antiviral));
            Assert.AreEqual(0f, table.GetMultiplier(ThreatType.Bacteria, antiviral));
        }

        [Test]
        public void SinMedicamento_EsIneficaz()
        {
            Assert.AreEqual(Effectiveness.Ineffective, table.Evaluate(ThreatType.Virus, null));
        }

        [Test]
        public void Set_ReemplazaLaCombinacionExistente()
        {
            table.Set(ThreatType.Bacteria, antibiotico, Effectiveness.Partial);
            Assert.AreEqual(Effectiveness.Partial, table.Evaluate(ThreatType.Bacteria, antibiotico));
            Assert.AreEqual(3, table.entries.Count);
        }
    }
}
