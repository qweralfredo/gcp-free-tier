namespace BriefappGuardian.Core.Entities;

/// <summary>
/// Configuração dos limites de quota para cada serviço/métrica monitorado.
/// Permite ao operador ajustar os limites gratuitos e ativar/desativar guardrails.
/// </summary>
public sealed class QuotaConfig
{
    public long Id { get; init; }
    public required string ServiceName { get; init; }
    public required string MetricName { get; init; }
    public required string DisplayName { get; init; }

    /// <summary>Limite gratuito oficial do GCP para esta métrica</summary>
    public double FreeLimit { get; init; }
    public required string Unit { get; init; }

    /// <summary>Threshold de Warning em % (default: 75)</summary>
    public double WarningPercent { get; init; } = 75;

    /// <summary>Threshold de Critical em % (default: 90)</summary>
    public double CriticalPercent { get; init; } = 90;

    /// <summary>Threshold de Emergency/Kill-Switch em % (default: 98)</summary>
    public double EmergencyPercent { get; init; } = 98;

    /// <summary>Guardrail de kill-switch habilitado?</summary>
    public bool KillSwitchEnabled { get; init; } = true;

    /// <summary>Tipo de periodo: "monthly" ou "daily"</summary>
    public string PeriodType { get; init; } = "monthly";

    public bool IsActive { get; init; } = true;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
