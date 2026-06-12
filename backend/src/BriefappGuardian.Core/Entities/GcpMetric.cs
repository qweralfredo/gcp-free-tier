namespace BriefappGuardian.Core.Entities;

/// <summary>
/// Representa uma métrica coletada de um serviço GCP em um instante de tempo.
/// Armazenada localmente no DuckDB para análise e projeção de quota.
/// </summary>
public sealed class GcpMetric
{
    public long Id { get; init; }

    /// <summary>Nome do serviço GCP (ex: "compute", "cloudsql", "run", "storage")</summary>
    public required string ServiceName { get; init; }

    /// <summary>Nome da métrica específica (ex: "egress_bytes", "api_requests", "instances")</summary>
    public required string MetricName { get; init; }

    /// <summary>Valor atual coletado</summary>
    public double Value { get; init; }

    /// <summary>Unidade da métrica (ex: "bytes", "count", "hours")</summary>
    public required string Unit { get; init; }

    /// <summary>Limite gratuito para esta métrica (ex: 1_073_741_824 = 1 GB)</summary>
    public double FreeLimit { get; init; }

    /// <summary>Porcentagem de uso do limite gratuito (0-100+)</summary>
    public double UsagePercent => FreeLimit > 0 ? (Value / FreeLimit) * 100.0 : 0;

    /// <summary>Instante em que a métrica foi coletada (UTC)</summary>
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Período de referência (ex: "2025-06" para métricas mensais)</summary>
    public required string Period { get; init; }
}
