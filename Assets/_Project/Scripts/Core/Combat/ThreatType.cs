namespace HealingKnight.Core.Combat
{
    /// <summary>Tipo de amenaza biológica. Define qué medicamento la trata (GDD 5.7).</summary>
    public enum ThreatType
    {
        Bacteria,
        Virus,
        Parasito,
        Inflamacion,
        Mucosidad
    }

    /// <summary>Resultado de aplicar un medicamento a una amenaza.</summary>
    public enum Effectiveness
    {
        Ineffective,
        Partial,
        Effective
    }
}
