using BriefappGuardian.Api.Data;
using BriefappGuardian.Core.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BriefappGuardian.Api.Endpoints;

/// <summary>
/// Endpoints REST da API do BriefappGuardian.
/// Segue o padrão Minimal API do ASP.NET Core 8+.
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").WithOpenApi();

        // GET /api/health — health check para Nginx e Docker
        api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .WithName("GetHealth")
           .WithSummary("Health check endpoint");

        // GET /api/dashboard — dados completos para o frontend Vue.js
        api.MapGet("/dashboard", (DuckDbContext db) =>
        {
            var metrics = db.GetLatestMetrics();
            var configs = db.GetActiveConfigs();
            var alerts = db.GetRecentAlerts(20);

            var now = DateTime.UtcNow;
            var period = now.ToString("yyyy-MM");

            var services = configs.Select(cfg =>
            {
                var metric = metrics.FirstOrDefault(m =>
                    m.service == cfg.ServiceName && m.metric == cfg.MetricName);

                var value = metric == default ? 0 : metric.value;
                var percent = cfg.FreeLimit > 0 ? (value / cfg.FreeLimit) * 100.0 : 0;

                var status = percent switch
                {
                    >= 98 => "emergency",
                    >= 90 => "critical",
                    >= 75 => "warning",
                    _ => "ok"
                };

                // Projeção: se temos consumo, estimar fim do limite
                double? projected = null;
                int? daysToLimit = null;

                if (metric != default && percent > 0 && cfg.PeriodType == "monthly")
                {
                    var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                    var dayOfMonth = now.Day;
                    var dailyRate = value / dayOfMonth;
                    projected = dailyRate * daysInMonth;

                    if (dailyRate > 0)
                        daysToLimit = (int)Math.Floor((cfg.FreeLimit - value) / dailyRate);
                }

                return new ServiceQuotaDto(
                    cfg.ServiceName,
                    cfg.DisplayName,
                    cfg.MetricName,
                    value,
                    cfg.FreeLimit,
                    Math.Round(percent, 2),
                    cfg.Unit,
                    status,
                    cfg.PeriodType,
                    metric == default ? period : metric.period,
                    projected.HasValue ? Math.Round(projected.Value, 0) : null,
                    daysToLimit
                );
            }).ToList();

            var recentAlerts = alerts.Select(a => new AlertSummaryDto(
                a.id,
                a.service,
                a.metric,
                a.level switch { 1 => "warning", 2 => "critical", _ => "emergency" },
                Math.Round(a.percent, 2),
                a.action,
                a.createdAt
            )).ToList();

            var summary = new DashboardSummaryDto(
                services.Count,
                services.Count(s => s.Status == "ok"),
                services.Count(s => s.Status == "warning"),
                services.Count(s => s.Status == "critical"),
                services.Count(s => s.Status == "emergency"),
                metrics.Count > 0 ? metrics.Max(m => m.collectedAt) : null,
                services.All(s => s.Status == "ok")
            );

            var response = new DashboardResponse(now, "", services, recentAlerts, summary);
            return Results.Ok(response);
        })
        .WithName("GetDashboard")
        .WithSummary("Retorna estado completo de consumo de cotas GCP");

        // GET /api/alerts — histórico de alertas
        api.MapGet("/alerts", (DuckDbContext db, int limit = 50) =>
        {
            var alerts = db.GetRecentAlerts(limit);
            return Results.Ok(alerts.Select(a => new AlertSummaryDto(
                a.id,
                a.service,
                a.metric,
                a.level switch { 1 => "warning", 2 => "critical", _ => "emergency" },
                a.percent,
                a.action,
                a.createdAt
            )));
        })
        .WithName("GetAlerts")
        .WithSummary("Histórico de alertas guardrail");

        // GET /api/quotas — configurações de quota
        api.MapGet("/quotas", (DuckDbContext db) =>
        {
            var configs = db.GetActiveConfigs();
            return Results.Ok(configs.Select(c => new
            {
                c.ServiceName,
                c.DisplayName,
                c.MetricName,
                c.FreeLimit,
                c.Unit,
                c.WarningPercent,
                c.CriticalPercent,
                c.EmergencyPercent,
                c.KillSwitchEnabled,
                c.PeriodType
            }));
        })
        .WithName("GetQuotas")
        .WithSummary("Configurações de quota e thresholds");
    }
}
