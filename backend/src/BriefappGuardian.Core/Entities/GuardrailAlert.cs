namespace BriefappGuardian.Core.Entities;

/// <summary>
/// Alerta gerado quando uma métrica cruza um threshold configurado.
/// </summary>
public sealed class GuardrailAlert
{
    public long Id { get; init; }
    public required string ServiceName { get; init; }
    public required string MetricName { get; init; }

    /// <summary>Nível do alerta: Warning (75%), Critical (90%), Emergency (98%)</summary>
    public AlertLevel Level { get; init; }

    /// <summary>Valor que disparou o alerta</summary>
    public double TriggerValue { get; init; }
    public double TriggerPercent { get; init; }

    /// <summary>Ação tomada pelo guardrail (null = apenas notificação)</summary>
    public string? ActionTaken { get; init; }

    public bool NotificationSent { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? Notes { get; init; }
}

public enum AlertLevel
{
    Warning = 1,    // 75% da quota
    Critical = 2,   // 90% da quota
    Emergency = 3   // 98% da quota — executa kill-switch
}
