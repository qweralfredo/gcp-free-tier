namespace BriefappGuardian.Core.Contracts;

/// <summary>
/// DTO de resposta do endpoint /api/dashboard.
/// Contém o estado completo de consumo de cotas GCP para renderização no frontend.
/// </summary>
public sealed record DashboardResponse(
    DateTime GeneratedAt,
    string ProjectId,
    IReadOnlyList<ServiceQuotaDto> Services,
    IReadOnlyList<AlertSummaryDto> RecentAlerts,
    DashboardSummaryDto Summary
);

public sealed record ServiceQuotaDto(
    string ServiceName,
    string DisplayName,
    string MetricName,
    double CurrentValue,
    double FreeLimit,
    double UsagePercent,
    string Unit,
    string Status,          // "ok" | "warning" | "critical" | "emergency"
    string PeriodType,
    string Period,
    double? ProjectedEndOfPeriod,
    int? EstimatedDaysToLimit
);

public sealed record AlertSummaryDto(
    long Id,
    string ServiceName,
    string MetricName,
    string Level,           // "warning" | "critical" | "emergency"
    double TriggerPercent,
    string? ActionTaken,
    DateTime CreatedAt
);

public sealed record DashboardSummaryDto(
    int TotalServices,
    int ServicesOk,
    int ServicesWarning,
    int ServicesCritical,
    int ServicesEmergency,
    DateTime? LastCollectionAt,
    bool AllServicesHealthy
);
